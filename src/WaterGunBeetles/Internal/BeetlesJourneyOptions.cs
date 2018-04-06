namespace WaterGunBeetles.Internal
{
  public class BeetlesJourneyOptions<T> : IBeetlesJourney<T>
  {
    readonly IBeetlesOptions _options;

    public BeetlesJourneyOptions(IBeetlesOptions options)
    {
      _options = options;
    }

    public BeetlesMetaModel MetaModel => _options.MetaModel;
  }
}