using Amazon.Auth.AccessControlPolicy;

namespace WaterGunBeetles.Client.Aws
{
  static class Policies
  {
    public static Policy CreateLambdaAssumeRolePolicy()
    {
      return new Policy
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
      };
    }

    public static Policy CreateLambdaCloudWatchLogsPolicy()
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
              new ActionIdentifier("logs:FilterLogEvents")
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
  }
}