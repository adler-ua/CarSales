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
        Console.WriteLine("--> Consuming auction created: "+ context.Message.Id);
        var item = _mapper.Map<Item>(context.Message);
        await DB.Update<Item>()
            .MatchID(context.Message.Id)
            .ModifyOnly(x => new { x.Make, x.Model, x.Color, x.Mileage, x.Year }, item)
            .ExecuteAsync();
    }
}
