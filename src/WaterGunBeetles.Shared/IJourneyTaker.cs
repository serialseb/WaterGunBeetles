using System.Threading.Tasks;

namespace WaterGunBeetles
{
  public interface IJourneyTaker<T>
  {
    Task TakeJourney<TJourney>(TJourney journey);
  }
}