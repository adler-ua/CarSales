using Grpc.Core;

namespace AuctionService;

public class GrpcAuctionService : GrpcAuction.GrpcAuctionBase
{
    private readonly AuctionDbContext _dbContext;

    public GrpcAuctionService(AuctionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override async Task<GrpcAuctionResponse> GetAuction(GetAuctionRequest request, ServerCallContext context)
    {
        Console.WriteLine("==> Received GRPC request GetAuction");

        var auction = await _dbContext.Auctions.FindAsync(Guid.Parse(request.Id)) 
            ?? throw new RpcException(new Status(StatusCode.NotFound,"Not Found"));
        var response = new GrpcAuctionResponse(){
            Auction = new GrpcAuctionModel(){
                Id = auction.Id.ToString(),
                AuctionEnd = auction.AuctionEnd.ToString(),
                ReservePrice = auction.ReservePrice,
                Seller = auction.Seller
            }
        };
        return response;
    }
}
