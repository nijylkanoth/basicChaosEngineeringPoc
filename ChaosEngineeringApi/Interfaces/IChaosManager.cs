using Polly;

namespace ChaosEngineeringApi.Interfaces;

public interface IChaosManager
{
    public ValueTask<double> GetInjectionRateAsync(ResilienceContext resilienceContext);

    public ValueTask<bool> IsChaosEnabledAsync(ResilienceContext resilienceContext);
}
