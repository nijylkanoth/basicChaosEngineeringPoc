using ChaosEngineeringApi.Interfaces;
using Polly;

namespace ChaosEngineeringApi.Managers;

public class ChaosManager(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor) : IChaosManager
{
    private const string TestUserParamKey = "user";
    private const string TestUser = "testuser";

    public ValueTask<bool> IsChaosEnabledAsync(ResilienceContext resilienceContext)
    {
        if (webHostEnvironment.IsDevelopment())
        {
            return ValueTask.FromResult(true);
        }

        if (httpContextAccessor.HttpContext is { } httpContext && httpContext.Request.Query.TryGetValue(TestUserParamKey, out var values) && values == TestUser)
        {
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    public ValueTask<double> GetInjectionRateAsync(ResilienceContext resilienceContext)
    {
        if (!webHostEnvironment.IsProduction())
        {
            return ValueTask.FromResult(0.03);
        }

        return ValueTask.FromResult(0.0);
    }
}
