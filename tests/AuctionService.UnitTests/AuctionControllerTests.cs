using AuctionService.Controllers;
using AuctionService.DTOs;
using AuctionService.Entities;
using AuctionService.RequestHelpers;
using AutoFixture;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuctionService.UnitTests;

public class AuctionControllerTests
{
    private readonly Mock<IAuctionRepository> _auctionRepo;
    private readonly Mock<IPublishEndpoint> _publishEndpoint;
    private readonly Fixture _fixture;
    private readonly AuctionsController _controller;
    private readonly IMapper _mapper;

    public AuctionControllerTests()
    {
        _fixture = new Fixture();
        _auctionRepo = new Mock<IAuctionRepository>();
        _publishEndpoint = new Mock<IPublishEndpoint>();

        var mockMapper = new MapperConfiguration(mc => {
            mc.AddMaps(typeof(MappingProfiles).Assembly);
        }).CreateMapper().ConfigurationProvider;
        
        _mapper = new Mapper(mockMapper);
        _controller = new AuctionsController(_auctionRepo.Object, _mapper, _publishEndpoint.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = Helpers.GetClaimsPrincipal() }
            }
        };
    }
    
    [Fact]
    public async Task GetAuctions_WithNoParams_Returns10Auctions()
    {
        // arrange
        var auctions = _fixture.CreateMany<AuctionDto>(10).ToList();
        _auctionRepo.Setup(repo => repo.GetAuctionsAsync(null)).ReturnsAsync(auctions);

        // act
        var result = await _controller.GetAllAuctions(null);

        // assert
        Assert.Equal(10, result.Value.Count);
        Assert.IsType<ActionResult<List<AuctionDto>>>(result);
    }
    
    [Fact]
    public async Task GetAuctionById_WithValidGuid_ReturnsAuction()
    {
        // arrange
        var auction = _fixture.Create<AuctionDto>();
        _auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(auction);

        // act
        var result = await _controller.GetAuctionById(auction.Id);

        // assert
        Assert.Equal(auction.Make, result.Value.Make);
        Assert.IsType<ActionResult<AuctionDto>>(result);
    }
    
    [Fact]
    public async Task GetAuctionById_WithIValidGuid_ReturnsNotFound()
    {
        // arrange
        _auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(value: null);

        // act
        var result = await _controller.GetAuctionById(Guid.NewGuid());

        // assert
        Assert.IsType<NotFoundResult>(result.Result);
    }
    
    [Fact]
    public async Task CreateAuction_WithValidCreateAuctionDto_ReturnsCreatedAtActionResult()
    {
        // arrange
        var auction = _fixture.Create<CreateAuctionDto>();
        _auctionRepo.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

        // act
        var result = await _controller.CreateAuction(auction);
        var createdResult = result.Result as CreatedAtActionResult;

        // assert
        Assert.NotNull(result.Result);
        Assert.Equal("GetAuctionById", createdResult.ActionName);
        Assert.IsType<AuctionDto>(createdResult.Value);
    }
    
    [Fact]
    public async Task UpdateAuction_WithValidIdAndCreateAuctionDto_ReturnsOkResult()
    {
        // arrange
        var updateAuctionDto = _fixture.Create<UpdateAuctionDto>();
        var auction = _fixture.Build<Auction>().Without(x => x.Item).Create();
        auction.Item = _fixture.Build<Item>().Without(x => x.Auction).Create();
        
        auction.Seller = _controller.User.Identity.Name;

        _auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>())).ReturnsAsync(auction);
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

        // act
        var result = await _controller.UpdateAuction(auction.Id, updateAuctionDto);
        
        // assert
        Assert.NotNull(result);
        Assert.IsType<OkResult>(result);
    }
    
    [Fact]
    public async Task UpdateAuction_WithInvalidId_ReturnsNotFound()
    {
        // arrange
        var updateAuctionDto = _fixture.Create<UpdateAuctionDto>();
        _auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Auction)null);

        // act
        var result = await _controller.UpdateAuction(Guid.NewGuid(), updateAuctionDto);
        
        // assert
        Assert.NotNull(result);
        Assert.IsType<NotFoundResult>(result);
    }
    
    [Fact]
    public async Task UpdateAuction_NoUpdateHappened_ReturnsBadRequest()
    {
        // arrange
        var updateAuctionDto = _fixture.Create<UpdateAuctionDto>();
        var auctionEntity = _fixture.Build<Auction>().Without(x => x.Item).Create();
        auctionEntity.Item = _fixture.Build<Item>().Without(x => x.Auction).Create();
        auctionEntity.Seller = _controller.User.Identity.Name;

        _auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>())).ReturnsAsync(auctionEntity);
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(false);

        // act
        var result = await _controller.UpdateAuction(auctionEntity.Id, updateAuctionDto);

        // assert
        Assert.NotNull(result);
        Assert.IsType<BadRequestObjectResult>(result);
    }
    
    [Fact]
    public async Task UpdateAuction_WithInvalidUser_ReturnsForbid()
    {
        // arrange
        var auctionEntity = _fixture.Build<Auction>().Without(x => x.Item).Create();
        auctionEntity.Seller = "wrong name";
        var updateAuctionDto = _fixture.Create<UpdateAuctionDto>();
        _auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>())).ReturnsAsync(auctionEntity);
        
        // act
        var result = await _controller.UpdateAuction(Guid.NewGuid(), updateAuctionDto);
        
        // assert
        Assert.NotNull(result);
        Assert.IsType<ForbidResult>(result);
    }
}