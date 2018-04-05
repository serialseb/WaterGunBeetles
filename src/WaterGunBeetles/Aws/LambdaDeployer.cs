using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Statement = Amazon.Auth.AccessControlPolicy.Statement;

namespace WaterGunBeetles
{
  public class LambdaDeployer
  {
    readonly string _timestamp;
    readonly string _packagePath;
    IEnumerable<string> _topics;
    Func<Task> _cleanup;

    public LambdaDeployer(
      string timestamp,
      string packagePath)
    {
      _timestamp = timestamp;
      _packagePath = packagePath;
    }

    static async Task<(IEnumerable<string> topicsArn, Func<Task> cleanup)> CreateLambdas(int count, int memorySize,
      string timestamp, string packagePath)
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


      var (roleArn, roleCleanup) = await CreateRole(iamClient, timestamp);
      cleanup.Add(roleCleanup);

      try
      {
        for (var lambdaIndex = 0; lambdaIndex < count; lambdaIndex++)
        {
          var (functionArn, functionName, functionCleanup) =
            await CreateFunction(memorySize, roleArn, timestamp, lambdaIndex, lambdaClient, packagePath);
          cleanup.Add(functionCleanup);

          var (topicArn, topicCleanup) = await CreateTopic(snsClient, timestamp, lambdaIndex);
          cleanup.Add(topicCleanup);
          publishTopics.Add(topicArn);

          var subCleaner = await Subscribe(snsClient, functionArn, topicArn);
          cleanup.Add(subCleaner);

          var permCleanup = await AddPermission(lambdaClient, functionArn, topicArn);
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

    static async Task<(string roleArn, Func<Task> roleCleanup)> CreateRole(
      AmazonIdentityManagementServiceClient iamClient, string timestamp)
    {
      var role = await iamClient.CreateRoleAsync(new CreateRoleRequest()
      {
        RoleName = $"Bettles_Role_{timestamp}",
        AssumeRolePolicyDocument = new Policy
        {
          Version = "2012-10-17",
          Statements =
          {
            new Statement(Statement.StatementEffect.Allow)
            {
              Actions = {new ActionIdentifier("sts:AssumeRole")},
              Principals =
              {
                new Principal("lambda.amazonaws.com")
                {
                  Provider = "Service"
                }
              }
            }
          }
        }.ToJson()
      });
      var roleArn = role.Role.Arn;
      return (roleArn, async () =>
        await iamClient.DeleteRoleAsync(new DeleteRoleRequest() {RoleName = role.Role.RoleName}));
    }

    static async Task<Func<Task>> AddPermission(AmazonLambdaClient lambda, string functionArn, string topicArn)
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
      var topic = await snsClient.CreateTopicAsync($"BeetleSays_{timestamp}_{lambdaIndex}");
      return (topic.TopicArn, async () => await snsClient.DeleteTopicAsync(topic.TopicArn));
    }

    static async Task<(string functionArn, string functionName, Func<Task> functionCleanup)> CreateFunction(
      int memorySize,
      string roleArn,
      string timestamp, int lambdaIndex, AmazonLambdaClient lambda, string packagePath)
    {
      var functionName = $"Beetle_{timestamp}_{lambdaIndex}";

      var function = await lambda.CreateFunctionAsync(new CreateFunctionRequest
      {
        Code = new FunctionCode {ZipFile = new MemoryStream(File.ReadAllBytes(packagePath))},
        Description = "Beetle Tester Function",
        FunctionName = functionName,
        Handler = "WaterGunBeetles.Lambda::WaterGunBeetles.Lambda.NullFunction::Handler",
        MemorySize = memorySize,
        Publish = true,
        Role = roleArn, // replace with the actual arn of the execution role you created
        Runtime = "dotnetcore2.0",
        Timeout = 120,
        VpcConfig = new VpcConfig { }
      });
      var functionArn = function.FunctionArn;
      return (functionArn, functionName, async () => await lambda.DeleteFunctionAsync(functionName));
    }

    public async Task Deploy(int count, int memorySize)
    {
      var (topics, cleanup) = await CreateLambdas(count, memorySize, _timestamp, _packagePath);

      _topics = topics;
      _cleanup = cleanup;
    }

    public async Task Shutdown()
    {
      await _cleanup();
    }
  }
}