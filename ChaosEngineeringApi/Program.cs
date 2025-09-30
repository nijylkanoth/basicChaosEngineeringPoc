using ChaosEngineeringApi.Interfaces;
using ChaosEngineeringApi.Managers;
using ChaosEngineeringApi.Services;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Simmy;
using Polly.Simmy.Fault;
using Polly.Simmy.Latency;
using Polly.Simmy.Outcomes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IChaosManager, ChaosManager>();
builder.Services.AddHttpContextAccessor();

var chaosBuilder = builder.Services.AddHttpClient<TodoClientService>( options=> options.BaseAddress=new Uri("https://jsonplaceholder.typicode.com") );

chaosBuilder.AddStandardResilienceHandler().Configure(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);

    options.CircuitBreaker.ShouldHandle = args => args.Outcome switch
    {
        { } outcome when HttpClientResiliencePredicates.IsTransient(outcome) => PredicateResult.True(),
        { Exception: InvalidOperationException } => PredicateResult.True(),
        _ => PredicateResult.False()
    };

    options.Retry.ShouldHandle = args => args.Outcome switch
    {
        { } outcome when HttpClientResiliencePredicates.IsTransient(outcome) => PredicateResult.True(),
        { Exception: InvalidOperationException } => PredicateResult.True(),
        _ => PredicateResult.False(),
    };

});




// Injecting Chaos engineering logic

chaosBuilder.AddResilienceHandler("chaosPipe", (builder,context) =>
{
    // STATIC Chaos 

    //double injectionRate = 0.3;

    //builder
    //.AddChaosLatency(injectionRate,TimeSpan.FromSeconds(5))
    //.AddChaosFault(injectionRate, ()=> new InvalidOperationException("Chaos Injection") )
    //.AddChaosOutcome(injectionRate, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError))
    //;


    // DYNAMIC chaos

    var manager = context.ServiceProvider.GetRequiredService<IChaosManager>();

    builder
        .AddChaosLatency(new ChaosLatencyStrategyOptions()
        {
            InjectionRateGenerator = args => manager.GetInjectionRateAsync(args.Context),
            EnabledGenerator = args => manager.IsChaosEnabledAsync(args.Context),
            Latency = TimeSpan.FromSeconds(5),
        })
        .AddChaosFault(new ChaosFaultStrategyOptions()
        {
            InjectionRateGenerator = args => manager.GetInjectionRateAsync(args.Context),
            EnabledGenerator = args => manager.IsChaosEnabledAsync(args.Context),
            FaultGenerator = new FaultGenerator().AddException( () => new InvalidOperationException("Chaos Injection") )
        })
        .AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>()
        {
            InjectionRateGenerator = args => manager.GetInjectionRateAsync(args.Context),
            EnabledGenerator = args => manager.IsChaosEnabledAsync(args.Context),
            OutcomeGenerator = new OutcomeGenerator<HttpResponseMessage>().AddResult(() => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError))
        })
        ;

});


builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/todos", (TodoClientService client, CancellationToken cancellationToken) =>
        client.GetTodosAsync(cancellationToken)//.GetAwaiter().GetResult()
    );

app.Run();
