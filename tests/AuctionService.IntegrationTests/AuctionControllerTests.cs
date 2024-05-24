
using System.Net;
using System.Net.Http.Json;
using AuctionService.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests;

// all tests here share the same instance of CustomWebAppFactory
// hence the same instance of test Postgres, RabbitMq
// IAsyncLifetime will allow to clean data after each tests
public class AuctionControllerTests : IClassFixture<CustomWebAppFactory>, IAsyncLifetime
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _httpClient;
    private const string GT_ID = "afbee524-5972-4075-8800-7d1f9d7b0a0c";

    public AuctionControllerTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _httpClient = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAuctions_ShouldReturn3Auctions()
    {
        // arrange (for this test all done in CustomWebAppFactory)

        // act
        var response = await _httpClient.GetFromJsonAsync<List<AuctionDto>>("api/auctions");

        // assert
        Assert.Equal(3, response.Count);
    }

    [Fact]
    public async Task GetAuctionById_WithValidId_ShouldReturnAuction()
    {
        // arrange (for this test all done in CustomWebAppFactory)

        // act
        var response = await _httpClient.GetFromJsonAsync<AuctionDto>($"api/auctions/{GT_ID}");

        // assert
        Assert.Equal("GT", response.Model);
    }

    [Fact]
    public async Task GetAuctionById_WithInvalidId_ShouldReturn404()
    {
        // arrange (for this test all done in CustomWebAppFactory)

        // act
        var response = await _httpClient.GetAsync($"api/auctions/{Guid.NewGuid()}");

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAuctionById_WithInvalidGuid_ShouldReturn400BadRequest()
    {
        // arrange (for this test all done in CustomWebAppFactory)

        // act
        var response = await _httpClient.GetAsync($"api/auctions/not-a-guid");

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuction_WithNoAuth_ShouldReturn401()
    {
        // arrange (for this test all done in CustomWebAppFactory)
        var auction = new CreateAuctionDto(){ Make = "test" };

        // act
        var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuction_WithAuth_ShouldReturn201()
    {
        // arrange (for this test all done in CustomWebAppFactory)
        var auction = GetAuctionForCreate();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);

        // assert
        response.EnsureSuccessStatusCode(); // for not a failure responses
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdAuction = await response.Content.ReadFromJsonAsync<AuctionDto>();
        Assert.Equal("bob", createdAuction.Seller);
    }

    [Fact]
    public async Task CreateAuction_WithInvalidDto_ShouldReturn400()
    {
        // arrange (for this test all done in CustomWebAppFactory)
        var auction = GetAuctionForCreate();
        auction.Make = null;
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_WithValidUpdateDtoAndUser_ShouldReturn200()
    {
        // arrange (for this test all done in CustomWebAppFactory)
        var updateAuctionDto = new UpdateAuctionDto { Make = "make-updated", Mileage = 60000 };
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PutAsJsonAsync($"api/auctions/{GT_ID}", updateAuctionDto);

        // assert
        response.EnsureSuccessStatusCode(); // for not a failure responses
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // verify updated entity properties:
        var updatedAuction = await _httpClient.GetFromJsonAsync<AuctionDto>($"api/auctions/{GT_ID}");
        Assert.Equal("make-updated", updatedAuction.Make); // updated
        Assert.Equal("GT", updatedAuction.Model); // not changed
        Assert.Equal("White", updatedAuction.Color); // not changed
        Assert.Equal(60000, updatedAuction.Mileage); // updated
        Assert.Equal(2020, updatedAuction.Year); // not changed
    }

    [Fact]
    public async Task UpdateAuction_WithValidUpdateDtoAndInvalidUserUser_ShouldReturn403()
    {
        // arrange (for this test all done in CustomWebAppFactory)
        var updateAuctionDto = new UpdateAuctionDto() { Make = "updated" };
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("alice-not-bob"));

        // act
        var response = await _httpClient.PutAsJsonAsync($"api/auctions/{GT_ID}", updateAuctionDto);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        // get access to the scope where the test is running in, to get hold of database:
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        DbHelper.ReinitDbForTests(db);
        return Task.CompletedTask;
    }

    private CreateAuctionDto GetAuctionForCreate()
    {
        return new CreateAuctionDto() {
            Make = "test",
            Model = "testModel",
            ImageUrl = "test",
            Color = "test",
            Mileage = 10,
            Year = 10,
            ReservePrice = 10
        };
    }
}
