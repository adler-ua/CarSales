using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Entities;

namespace SearchService;

public class DbInitializer
{
    public static async Task InitDb(WebApplication app)
    {
        await DB.InitAsync("SearchDb", MongoClientSettings.FromConnectionString(app.Configuration.GetConnectionString("MongoDbConnection")));

        await DB.Index<SearchService.Item>()
        .Key(x=>x.Make, KeyType.Text)
        .Key(x=>x.Model, KeyType.Text)
        .Key(x=>x.Color, KeyType.Text)
        .CreateAsync();

        var count = await DB.CountAsync<Item>();
        
        // we are in static method, so have to create scope to use DI services
        using var scope = app.Services.CreateScope();
        var client = scope.ServiceProvider.GetService<AuctionServiceHttpClient>();
        var items = await client.GetItemsForSearchDb();
        Console.WriteLine(items.Count + " returned from Auction service:");
        items.ForEach(item => Console.WriteLine($"{item.ID} updated at {item.UpdatedAt}"));
        if(items.Count>0) await DB.SaveAsync(items);
    }
}
