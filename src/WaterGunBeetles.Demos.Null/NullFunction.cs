using System.Threading.Tasks;

namespace WaterGunBeetles.Lambda
{
  public class NullFunction : AwsLambdaFunction<NullJourney, NullJourneyResult>{
    public NullFunction() : base(NullJourneyTaker)
    {
    }

    static Task<NullJourneyResult> NullJourneyTaker(NullJourney journey)
    {
      return Task.FromResult(new NullJourneyResult());
    }
  }
}