using AuctionService;
using AuctionService.Data;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<AuctionDbContext>(opt => {
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// register AutoMapper as a service:
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddMassTransit(x => {
    x.AddEntityFrameworkOutbox<AuctionDbContext>(opt => {
        // every 10sec MassTransit is going to check Outbox table to see
        // if there are any outgoing messages left while the RabbitMq was unabailable
        // and attempt to delivery those messages when it is up again:
        opt.QueryDelay = TimeSpan.FromSeconds(10);
        opt.UsePostgres();
        opt.UseBusOutbox();
    });

    x.AddConsumersFromNamespaceContaining<AuctionCreatedFaultConsumer>();

    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false));

    x.UsingRabbitMq((context,cfg) => {
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
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => 
    {
        options.Authority = builder.Configuration["IdentityServiceUrl"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters.ValidateAudience = false;
        options.TokenValidationParameters.NameClaimType = "username";
    });

builder.Services.AddGrpc();
builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<GrpcAuctionService>();

var retryPolicy = Policy.Handle<NpgsqlException>()
                        .WaitAndRetry(5, retryAttempts => TimeSpan.FromSeconds(10));

// try to execute:
retryPolicy.ExecuteAndCapture(() => DbInitializer.InitDb(app));

app.Run();

public partial class Program {}