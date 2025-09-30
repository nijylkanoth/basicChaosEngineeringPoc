# basicChaosEngineeringPoc


* #### Create a new ASP.NET Core Web API project ( .NET 8.0 )

  - Create a Model, TodoModel.cs, and replace the contents with the following: <br>

          public record TodoModel 

          {

          [JsonPropertyName("id")] 
          public int Id { get; set; }

          [JsonPropertyName("title")]
          public string Title { get; set; }

          [JsonPropertyName("completed")]
          public bool Completed { get; set; }

          }


- Create a Service, TodoClientService.cs, and replace the contents with the following:

      public class TodoClientService(HttpClient httpClient)
      {
      public async Task<IEnumerable<TodoModel>> GetTodosAsync(CancellationToken cancellationToken)
      {
        return await httpClient.GetFromJsonAsync<IEnumerable<TodoModel>>("/todos",cancellationToken) ?? [];
      }
      }


<br> 

* #### Installing Required Packages:

    In your .NET Core project, install the following 2 packages: <br>
  
    **Microsoft.Extensions.Http.Polly Version="9.0.9"** <br>
    **Microsoft.Extensions.Http.Resilience" Version="9.9.0"**

  <br>


* #### Static injection of Chaos into the HTTP Client:

       In the "ChaosEngineeringApi", HTTP client setup in your code to use the AddResilienceHandler for integrating chaos strategies

       var chaosBuilder = builder.Services.AddHttpClient<TodoClientService>( options=> options.BaseAddress=new Uri("https://jsonplaceholder.typicode.com") );

       chaosBuilder.AddResilienceHandler("chaosPipe", (builder,context) =>
       {
       double injectionRate = 0.3;

       builder
       .AddChaosLatency(injectionRate,TimeSpan.FromSeconds(5)) 
       .AddChaosFault(injectionRate, ()=> new InvalidOperationException("Chaos Injection") ) 
       .AddChaosOutcome(injectionRate, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)) 
       ;
       });

  <br> 

* #### Dynamic injection of Chaos:


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



  <br> 

* #### Using Resilience Strategies to handle Chaos:


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



   

