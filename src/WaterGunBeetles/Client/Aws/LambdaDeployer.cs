using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using WaterGunBeetles.Internal;
using WaterGunBeetles.Server.Aws;
using InvalidParameterValueException = Amazon.Lambda.Model.InvalidParameterValueException;
using Statement = Amazon.Auth.AccessControlPolicy.Statement;

namespace WaterGunBeetles.Client.Aws
{
  public class LambdaDeployer
  {
    readonly string _timestamp;
    readonly string _packagePath;
    readonly Type _settingsType;
    public IEnumerable<string> Topics { get; private set; }
    Func<Task> _cleanup;


    readonly string _lambdaHandlerName;

    public LambdaDeployer(string timestamp,
      string packagePath, Type settingsType)
    {
      _timestamp = timestamp;
      _packagePath = packagePath;
      _settingsType = settingsType;
      _lambdaHandlerName = $"{settingsType.Assembly.GetName().Name}::{typeof(LambdaFunction).FullName}::{nameof(LambdaFunction.Handle)}";

    }

    static async Task<(IEnumerable<string> topicsArn, Func<Task> cleanup)> CreateLambdas(
      int count,
      int memorySize,
      string timestamp,
      string packagePath,
      string settingsTypeName,
      string lambdaHandlerName)
    {
      var cleanup = new List<Func<Task>>();
      var publishTopics = new List<string>(count);


      var lambdaClient = new AmazonLambdaClient();
      var snsClient = new AmazonSimpleNotificationServiceClient();
      var iamClient = new AmazonIdentityManagementServiceClient();

      cleanup.Add(() =>
      {
        lambdaClient.Dispose();
        snsClient.Dispose();
        iamClient.Dispose();
        return Task.CompletedTask;
      });


      var (roleArn, _, roleCleanup) = await CreateRole(iamClient, timestamp);
      cleanup.Add(roleCleanup);
      try
      {
        for (var lambdaIndex = 0; lambdaIndex < count; lambdaIndex++)
        {
          var (functionArn, _, functionCleanup) =
            await CreateFunction(memorySize, roleArn, timestamp, lambdaIndex, lambdaClient, packagePath,
              settingsTypeName, lambdaHandlerName);
          cleanup.Add(functionCleanup);

          var (topicArn, topicCleanup) = await CreateTopic(snsClient, timestamp, lambdaIndex);
          cleanup.Add(topicCleanup);
          publishTopics.Add(topicArn);

          var subCleaner = await Subscribe(snsClient, functionArn, topicArn);
          cleanup.Add(subCleaner);

          var permCleanup = await AddExecutionFromSnsPermission(lambdaClient, functionArn, topicArn);
          cleanup.Add(permCleanup);
        }
      }
      catch (Exception)
      {
        await Cleanup(cleanup);
        throw;
      }

      return (publishTopics, () => Cleanup(cleanup));
    }

    static async Task Cleanup(List<Func<Task>> cleanup)
    {
      cleanup.Reverse();
      foreach (var c in cleanup) await c();
    }

    static async Task<(string roleArn, string roleName, Func<Task> roleCleanup)> CreateRole(
      AmazonIdentityManagementServiceClient iamClient, string timestamp)
    {
      var assumeRolePolicyDocument = CreateLambdaAssumeRolePolicy().ToJson();
      var roleName = $"Beetles_Role_{timestamp}";
      var policyName = $"Beetles_CloudWatch_Access_{timestamp}";

      var role = await iamClient.CreateRoleAsync(new CreateRoleRequest()
      {
        RoleName = roleName,
        AssumeRolePolicyDocument = assumeRolePolicyDocument
      });
      var roleArn = role.Role.Arn;

      await iamClient.PutRolePolicyAsync(new PutRolePolicyRequest()
      {
        PolicyName = policyName,
        RoleName = role.Role.RoleName,
        PolicyDocument = CreateLambdaCloudWatchLogsPolicy().ToJson()
      });

      return (roleArn, role.Role.RoleName, async () =>
      {
        await iamClient.DeleteRolePolicyAsync(new DeleteRolePolicyRequest()
        {
          RoleName = role.Role.RoleName,
          PolicyName = policyName
        });
        await iamClient.DeleteRoleAsync(new DeleteRoleRequest() {RoleName = role.Role.RoleName});
      });
    }

    static Policy CreateLambdaAssumeRolePolicy()
    {
      return new Policy
      {
        Version = "2012-10-17",
        Statements =
        {
          new Statement(Statement.StatementEffect.Allow)
          {
            Actions = {new ActionIdentifier("sts:AssumeRole")},
            Id = "",
            Principals =
            {
              new Principal("lambda.amazonaws.com")
              {
                Provider = "Service"
              }
            }
          }
        }
      };
    }

    static Policy CreateLambdaCloudWatchLogsPolicy()
    {
      return new Policy
      {
        Version = "2012-10-17",
        Statements =
        {
          new Statement(Statement.StatementEffect.Allow)
          {
            Actions =
            {
              new ActionIdentifier("logs:CreateLogGroup"),
              new ActionIdentifier("logs:CreateLogStream"),
              new ActionIdentifier("logs:DescribeLogGroups"),
              new ActionIdentifier("logs:DescribeLogStreams"),
              new ActionIdentifier("logs:GetLogEvents"),
              new ActionIdentifier("logs:PutLogEvents"),
              new ActionIdentifier("logs:FilterLogEvents"),
            },
            Id = "",
            Resources =
            {
              new Resource("arn:aws:logs:*:*:*")
            }
          }
        }
      };
    }

    static async Task<Func<Task>> AddExecutionFromSnsPermission(AmazonLambdaClient lambda, string functionArn,
      string topicArn)
    {
      await lambda.AddPermissionAsync(new Amazon.Lambda.Model.AddPermissionRequest
      {
        Action = "lambda:InvokeFunction",
        StatementId = "AllowExecutionFromSNS",
        FunctionName = functionArn,
        Principal = "sns.amazonaws.com",
        SourceArn = topicArn
      });
      return async () => await lambda.RemovePermissionAsync(
        new Amazon.Lambda.Model.RemovePermissionRequest
        {
          FunctionName = functionArn,
          StatementId = "AllowExecutionFromSNS"
        });
    }

    static async Task<Func<Task>> Subscribe(AmazonSimpleNotificationServiceClient snsClient, string functionArn,
      string topicArn)
    {
      var subscription = await snsClient.SubscribeAsync(new SubscribeRequest
      {
        Endpoint = functionArn,
        Protocol = "lambda",
        TopicArn = topicArn
      });
      return async () => await snsClient.UnsubscribeAsync(subscription.SubscriptionArn);
    }

    static async Task<(string topicArn, Func<Task> topicCleanup)> CreateTopic(
      AmazonSimpleNotificationServiceClient snsClient, string timestamp, int lambdaIndex)
    {
      var topic = await snsClient.CreateTopicAsync($"Beetles_{timestamp}_{lambdaIndex}");
      return (topic.TopicArn, async () => await snsClient.DeleteTopicAsync(topic.TopicArn));
    }

    static async Task<(string functionArn, string functionName, Func<Task> functionCleanup)> CreateFunction(
      int memorySize,
      string roleArn,
      string timestamp, int lambdaIndex, AmazonLambdaClient lambda, string packagePath, string configurationTypeName,
      string lambdaHandlerName)
    {
      var functionName = $"Beetles_{timestamp}_{lambdaIndex}";

      var retryWait = Stopwatch.StartNew();
      CreateFunctionResponse function = null;
      do
      {
        try
        {
          function = await lambda.CreateFunctionAsync(new CreateFunctionRequest
          {
            Code = new FunctionCode {ZipFile = new MemoryStream(File.ReadAllBytes(packagePath))},
            Description = "Beetle Tester Function",
            FunctionName = functionName,
            Handler = lambdaHandlerName,
            MemorySize = memorySize,
            Publish = true,
            Role = roleArn,
            Runtime = "dotnetcore2.0",
            Timeout = 120,
            Environment = new Amazon.Lambda.Model.Environment
            {
              Variables =
              {
                [Constants.ConfigurationTypeNameKey] = configurationTypeName
              }
            },
            VpcConfig = new VpcConfig()
          });
        }
        catch (InvalidParameterValueException) when (retryWait.Elapsed < TimeSpan.FromMinutes(1))
        {
        }
      } while (function == null);

      var functionArn = function.FunctionArn;
      return (functionArn, functionName, async () => await lambda.DeleteFunctionAsync(functionName));
    }

    public async Task Deploy(int count, int memorySize)
    {
      var (topics, cleanup) =
        await CreateLambdas(count, memorySize, _timestamp, _packagePath, _settingsType.AssemblyQualifiedName, _lambdaHandlerName);

      Topics = topics;
      _cleanup = cleanup;
    }

    public async Task Shutdown()
    {
      if (_cleanup != null)
        await _cleanup.Invoke();
      _cleanup = null;
    }
  }
}