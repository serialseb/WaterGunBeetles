namespace WaterGunBeetles.Internal
{
  public class BeetlesOptions : IBeetlesOptions
  {
    public BeetlesMetaModel MetaModel { get; } = new BeetlesMetaModel();
  }
}