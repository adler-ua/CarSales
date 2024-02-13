using MongoDB.Entities;

namespace SearchService;

public class AuctionServiceHttpClient
{
    private readonly HttpClient _client;
    private readonly IConfiguration _config;

    public AuctionServiceHttpClient(HttpClient client, IConfiguration config)
    {
        _client = client;
        _config = config;
    }

    public async Task<List<Item>> GetItemsForSearchDb()
    {
        var lastUpdated = await DB.Find<Item, string>()
                                    .Sort(x=>x.Descending(x=>x.UpdatedAt))
                                    .Project(x=>x.UpdatedAt.ToString())
                                    .ExecuteFirstAsync();
        Console.WriteLine("LastUpdated: "+lastUpdated);
        return await _client.GetFromJsonAsync<List<Item>>(_config["AuctionServiceUrl"]+"/api/auctions?date="+lastUpdated);
    }
}
