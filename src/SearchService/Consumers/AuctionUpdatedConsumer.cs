using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace SearchService;

public class AuctionUpdatedConsumer : IConsumer<AuctionUpdated>
{
    private readonly IMapper _mapper;

    public AuctionUpdatedConsumer(IMapper mapper){
        _mapper = mapper;
    }

    public async Task Consume(ConsumeContext<AuctionUpdated> context)
    {
        Console.WriteLine("--> Consuming auction updated: " + context.Message.Id);
        var item = _mapper.Map<Item>(context.Message);
        var result = await DB.Update<Item>()
            .MatchID(context.Message.Id)
            .ModifyOnly(x => new { x.Make, x.Model, x.Color, x.Mileage, x.Year, x.UpdatedAt }, item)
            .ExecuteAsync();
        if(result.IsAcknowledged)
            Console.WriteLine("--! Updated auction: " + context.Message.Id);
        else
            throw new MessageException(typeof(Contracts.AuctionUpdated), "Problem updating object");
    }
}
