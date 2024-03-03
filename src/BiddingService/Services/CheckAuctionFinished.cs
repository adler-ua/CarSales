

using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace BiddingService;

public class CheckAuctionFinished : BackgroundService
{
    private readonly ILogger<CheckAuctionFinished> _logger;
    private readonly IServiceProvider _services;

    public CheckAuctionFinished(ILogger<CheckAuctionFinished> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting check for finished auctions");

        stoppingToken.Register(()=>_logger.LogInformation("==> Auction check is stopping"));

        while(!stoppingToken.IsCancellationRequested)
        {
            await CheckAuctions(stoppingToken);

            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task CheckAuctions(CancellationToken stoppingToken)
    {
        var finishedAuctions = await DB.Find<Auction>()
                                        .Match(a => a.AuctionEnd <= DateTime.UtcNow)
                                        .Match(a => !a.AuctionIsFinished)
                                        .ExecuteAsync(stoppingToken);
        if(finishedAuctions.Count == 0) return;
        
        _logger.LogInformation($"==> Found {finishedAuctions.Count} auctions that have completed");

        using var scope = _services.CreateScope();
        var endpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        foreach(var auction in finishedAuctions)
        {
            auction.AuctionIsFinished = true;
            await auction.SaveAsync(null, stoppingToken);

            var winningBid = await DB.Find<Bid>()
                                .Match(b => b.AuctionId == auction.ID)
                                .Match(b => b.BidStatus == BidStatus.Accepted)
                                .Sort(x => x.Descending(b => b.Amount))
                                .ExecuteFirstAsync(stoppingToken);
            await endpoint.Publish<AuctionFinished>(new AuctionFinished(){
                ItemSold = true,
                AuctionId = auction.ID,
                Amount = winningBid?.Amount,
                Winner = winningBid?.Bidder, // winningBid is optional if there is no winning bid
                Seller = auction.Seller
            }, stoppingToken);
        }
    }
}
