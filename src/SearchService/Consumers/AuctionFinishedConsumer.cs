using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace SearchService;

public class AuctionFinishedConsumer : IConsumer<AuctionFinished>
{
    public async Task Consume(ConsumeContext<AuctionFinished> context)
    {
        Console.WriteLine("--> Consiming auction finished with auctionId: " + context.Message.AuctionId);
        
        var auction = await DB.Find<Item>().OneAsync(context.Message.AuctionId);

        if(context.Message.ItemSold)
        {
            auction.Winner = context.Message.Winner;
            auction.Seller = context.Message.Seller;
        }

        auction.Status = "Finished";
        await auction.SaveAsync();

        Console.WriteLine("!-- updated auction finished status for auctionId: " + auction.ID);
    }
}
