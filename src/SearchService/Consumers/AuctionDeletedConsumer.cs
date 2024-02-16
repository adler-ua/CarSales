using AutoMapper;
using MassTransit;
using MongoDB.Entities;

namespace SearchService;

public class AuctionDeletedConsumer : IConsumer<Contracts.AuctionDeleted>
{
    private readonly IMapper _mapper;

    public AuctionDeletedConsumer(IMapper mapper){
        _mapper = mapper;
    }

    public async Task Consume(ConsumeContext<Contracts.AuctionDeleted> context)
    {
        Console.WriteLine("--> Consuming auction deleted: " + context.Message.Id);
        var result = await DB.DeleteAsync<Item>(context.Message.Id);
        if(result.IsAcknowledged)
            Console.WriteLine($"--! Deleted {result.DeletedCount} auction: {context.Message.Id}");
        else
            throw new MessageException(typeof(Contracts.AuctionDeleted), "Problem deleting object");
    }
}
