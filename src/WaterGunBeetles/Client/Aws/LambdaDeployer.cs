using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using WaterGunBeetles.Internal;
using WaterGunBeetles.Server.Aws;
using AddPermissionRequest = Amazon.Lambda.Model.AddPermissionRequest;
using Environment = Amazon.Lambda.Model.Environment;
using InvalidParameterValueException = Amazon.Lambda.Model.InvalidParameterValueException;
using RemovePermissionRequest = Amazon.Lambda.Model.RemovePermissionRequest;

namespace WaterGunBeetles.Client.Aws
{
  [SuppressMessage("ReSharper", "MethodSupportsCancellation")]
  public class LambdaDeployer
  {
     Action<object> Verbose { get; }
    
    readonly string _lambdaHandlerName;
    readonly string _packagePath;
    readonly Type _configurationType;
    readonly string _timestamp;
    Func<Task> _cleanup;

    public LambdaDeployer(string timestamp,
      string packagePath,
      Type configurationType,
      Action<object> verbose)
    {
      _timestamp = timestamp;
      _packagePath = packagePath;
      _configurationType = configurationType;
      Verbose = verbose;
      _lambdaHandlerName =
        $"{configurationType.Assembly.GetName().Name}::{typeof(LambdaFunction).FullName}::{nameof(LambdaFunction.Handle)}";
    }

    public string Topic { get; private set; }

    static async Task<(string topicArn, Func<Task> cleanup)> CreateLambda(LambdaCreationOptions lambdaCreationOptions)
    {
      var cleanup = new List<Func<Task>>();
      var log = lambdaCreationOptions.Verbose;
      string publishTopic;


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


      try
      {
        log("Creating IAM Role");
        var (roleArn, _, roleCleanup) =
          await CreateRole(iamClient, lambdaCreationOptions.Timestamp, lambdaCreationOptions.Name,
            lambdaCreationOptions.CancellationToken);
        cleanup.Add(roleCleanup);

        log("Creating Function");
        var (functionArn, _, functionCleanup) =
          await CreateFunction(roleArn,
            lambdaClient,
            lambdaCreationOptions);
        cleanup.Add(functionCleanup);

        log("Creating SNS Topic");
        var (topicArn, topicCleanup) =
          await CreateTopic(snsClient, lambdaCreationOptions.Timestamp, lambdaCreationOptions.Name,
            lambdaCreationOptions.CancellationToken);
        cleanup.Add(topicCleanup);
        publishTopic = topicArn;

        log("Creating SNS Subscription");
        var subCleaner = await Subscribe(snsClient, functionArn, topicArn, lambdaCreationOptions.CancellationToken);
        cleanup.Add(subCleaner);

        log("Permissioning SNS to execute the function");
        var permCleanup = await AddExecutionFromSnsPermission(lambdaClient, functionArn, topicArn,
          lambdaCreationOptions.CancellationToken);
        
        cleanup.Add(permCleanup);
      }
      catch (Exception)
      {
        log("Cleaning up resources");
        await Cleanup(cleanup);
        throw;
      }

      return (publishTopic, () => Cleanup(cleanup));
    }

    static async Task Cleanup(List<Func<Task>> cleanup)
    {
      cleanup.Reverse();
      foreach (var c in cleanup) await c();
    }

    static async Task<(string roleArn, string roleName, Func<Task> roleCleanup)> CreateRole(
      IAmazonIdentityManagementService iamClient,
      string timestamp,
      string name,
      CancellationToken cancellationToken)
    {
      var assumeRolePolicyDocument = Policies.CreateLambdaAssumeRolePolicy().ToJson();
      var roleName = $"Beetles_{name}_{timestamp}";
      var policyName = $"Beetles_{name}_{timestamp}";

      var role = await iamClient.CreateRoleAsync(new CreateRoleRequest
      {
        RoleName = roleName,
        AssumeRolePolicyDocument = assumeRolePolicyDocument
      }, cancellationToken);
      var roleArn = role.Role.Arn;

      await iamClient.PutRolePolicyAsync(new PutRolePolicyRequest
      {
        PolicyName = policyName,
        RoleName = role.Role.RoleName,
        PolicyDocument = Policies.CreateLambdaCloudWatchLogsPolicy().ToJson()
      }, cancellationToken);

      return (roleArn, role.Role.RoleName, async () =>
      {
        await iamClient.DeleteRolePolicyAsync(new DeleteRolePolicyRequest
        {
          RoleName = role.Role.RoleName,
          PolicyName = policyName
        });
        
        await iamClient.DeleteRoleAsync(new DeleteRoleRequest {RoleName = role.Role.RoleName});
      });
    }

    static async Task<Func<Task>> AddExecutionFromSnsPermission(IAmazonLambda lambda,
      string functionArn,
      string topicArn,
      CancellationToken cancellationToken)
    {
      await lambda.AddPermissionAsync(new AddPermissionRequest
      {
        Action = "lambda:InvokeFunction",
        StatementId = "AllowExecutionFromSNS",
        FunctionName = functionArn,
        Principal = "sns.amazonaws.com",
        SourceArn = topicArn
      }, cancellationToken);
      return async () => await lambda.RemovePermissionAsync(
        new RemovePermissionRequest
        {
          FunctionName = functionArn,
          StatementId = "AllowExecutionFromSNS"
        });
    }

    static async Task<Func<Task>> Subscribe(AmazonSimpleNotificationServiceClient snsClient,
      string functionArn,
      string topicArn,
      CancellationToken cancellationToken)
    {
      var subscription = await snsClient.SubscribeAsync(new SubscribeRequest
      {
        Endpoint = functionArn,
        Protocol = "lambda",
        TopicArn = topicArn
      }, cancellationToken);
      return async () => await snsClient.UnsubscribeAsync(subscription.SubscriptionArn);
    }

    static async Task<(string topicArn, Func<Task> topicCleanup)> CreateTopic(
      AmazonSimpleNotificationServiceClient snsClient,
      string timestamp,
      string name,
      CancellationToken cancellationToken)
    {
      var topic = await snsClient.CreateTopicAsync($"Beetles_{name}_{timestamp}", cancellationToken);
      return (topic.TopicArn, async () => await snsClient.DeleteTopicAsync(topic.TopicArn));
    }

    static async Task<(string functionArn, string functionName, Func<Task> functionCleanup)> CreateFunction(
      string roleArn,
      IAmazonLambda lambda,
      LambdaCreationOptions options)
    {
      var functionName = $"Beetles_{options.Name}_{options.Timestamp}";

      var retryWait = Stopwatch.StartNew();
      CreateFunctionResponse function = null;
      do
      {
        try
        {
          function = await lambda.CreateFunctionAsync(new CreateFunctionRequest
          {
            Code = new FunctionCode {ZipFile = new MemoryStream(File.ReadAllBytes(options.PackagePath))},
            Description = $"Beetles Load Test {options.Name}",
            FunctionName = functionName,
            Handler = options.LambdaHandlerName,
            MemorySize = options.MemorySize,
            Publish = true,
            Role = roleArn,
            Runtime = "dotnetcore2.1",
            Timeout = 120,
            Environment = new Environment
            {
              Variables =
              {
                [Constants.ConfigurationTypeNameKey] = options.SettingsTypeName
              }
            },
            VpcConfig = new VpcConfig()
          }, options.CancellationToken);
//          await lambda.PutFunctionConcurrencyAsync(new PutFunctionConcurrencyRequest()
//          {
//            FunctionName = functionName,
//            ReservedConcurrentExecutions = options.ProvisionedConcurrency
//          }, options.CancellationToken);
        }
        catch (InvalidParameterValueException) when (retryWait.Elapsed < TimeSpan.FromMinutes(1))
        {
        }
      } while (function == null);

      var functionArn = function.FunctionArn;
      return (functionArn, functionName, async () => await lambda.DeleteFunctionAsync(functionName));
    }

    public async Task Deploy(int memorySize,
      string name,
      CancellationToken cancellationToken,
      int provisionedConcurrency = 600)
    {
      var (topic, cleanup) =
        await CreateLambda(new LambdaCreationOptions(
          memorySize,
          _timestamp,
          _packagePath,
          _configurationType.AssemblyQualifiedName,
          _lambdaHandlerName,
          name,
          provisionedConcurrency,
          Verbose,
          cancellationToken));

      Topic = topic;
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