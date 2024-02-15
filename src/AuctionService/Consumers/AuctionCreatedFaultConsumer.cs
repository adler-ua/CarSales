using Contracts;
using MassTransit;

namespace AuctionService;

public class AuctionCreatedFaultConsumer : IConsumer<Fault<Contracts.AuctionCreated>>
{
    public async Task Consume(ConsumeContext<Fault<AuctionCreated>> context)
    {
        Console.WriteLine("--> consuming faulty creation...");

        var exception = context.Message.Exceptions.First();

        // here we can analyse the exception and figure out what we can change in the message to republish it
        // Example:
        if(exception.ExceptionType == "System.ArgumentException")
        {
            // ... changing something in the message ...
            // publishing to the original exchange:
            await context.Publish(context.Message.Message);
        }
    }
}
