using MassTransit;
using MongoDB.Driver;
using MongoDB.Entities;
using Polly;
using Polly.Extensions.Http;
using SearchService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<AuctionServiceHttpClient>().AddPolicyHandler(GetPolicy());
builder.Services.AddMassTransit(x => 
{
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();

    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));
    x.UsingRabbitMq((context, cfg) => {

        cfg.UseMessageRetry(r => 
        {
            r.Handle<RabbitMqConnectionException>();
            r.Interval(5, TimeSpan.FromSeconds(10));
        });
        
        cfg.Host(builder.Configuration["RabbitMq:Host"], "/", host=>
        {
            host.Username(builder.Configuration.GetValue("RabbitMq:Username", "guest" /*default user name*/));
            host.Password(builder.Configuration.GetValue("RabbitMq:Password", "guest" /*default password*/));
        });
        
        cfg.ReceiveEndpoint("search-auction-created", endpointFormatter=>{
            // try 5 times with 5 sec between the attempts:
            endpointFormatter.UseMessageRetry(r => r.Interval(5, 5));
            endpointFormatter.ConfigureConsumer<AuctionCreatedConsumer>(context);
        });

        cfg.ReceiveEndpoint("search-auction-updated", endpointNameFormatter => {
            endpointNameFormatter.UseMessageRetry(r => r.Interval(5, 5));
            endpointNameFormatter.ConfigureConsumer<AuctionUpdatedConsumer>(context);
        });

        cfg.ReceiveEndpoint("search-auction-deleted", endpointNameFormatter => {
            endpointNameFormatter.UseMessageRetry(r => r.Interval(5, 5));
            endpointNameFormatter.ConfigureConsumer<AuctionDeletedConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(async () => {
    
    await Policy.Handle<TimeoutException>()
        .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(10))
        .ExecuteAndCaptureAsync(async () => await DbInitializer.InitDb(app));
});

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetPolicy()
    => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));