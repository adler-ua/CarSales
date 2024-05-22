using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests;

public static class ServiceCollectionExtensions
{
    public static void RemoveDbContext(this IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault( d => d.ServiceType == typeof(DbContextOptions<AuctionDbContext>));
            if(descriptor != null)
                services.Remove(descriptor);
    }

    public static void EnsureCreated(this IServiceCollection services)
    {
        var servicesProvider = services.BuildServiceProvider();

            using var scope = servicesProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AuctionDbContext>();

            db.Database.Migrate();
            DbHelper.InitDbForTests(db);
    }
}
