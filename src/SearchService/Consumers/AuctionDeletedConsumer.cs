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
        if(result.DeletedCount > 0)
            Console.WriteLine($"--! Deleted {result.DeletedCount} auction: {context.Message.Id}");
        else
            Console.WriteLine("--! nothing was deleted");
    }
}
