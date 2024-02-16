using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly AuctionDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;

    public AuctionsController(  AuctionDbContext context, 
                                IMapper mapper,
                                IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
    {
        var query = _context.Auctions.OrderBy(x=>x.Item.Make).AsQueryable();
        if(!string.IsNullOrEmpty(date))
        {
            query = query.Where(x=>x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) >0 );
        }
        return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
    {
        var auction = await _context.Auctions.Include(x => x.Item).FirstOrDefaultAsync(x=>x.Id == id);
        if(auction == null) return NotFound();
        
        return _mapper.Map<AuctionDto>(auction);
    }

    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto dto)
    {
        var auction = _mapper.Map<Auction>(dto);
        // TODO: add current user as seller
        auction.Seller = "test";

        _context.Auctions.Add(auction);
        var newAuction = _mapper.Map<AuctionDto>(auction);
        await _publishEndpoint.Publish(_mapper.Map<Contracts.AuctionCreated>(newAuction));
        // both things (created auction and outbox message for MassTransit.RabbitMq)
        // are going to be saved as a part of a signle transaction:
        var result = await _context.SaveChangesAsync() > 0;
        
        if(!result) return BadRequest("Could not save changes to the DB");
        
        return CreatedAtAction(nameof(GetAuctionById), new {auction.Id}, _mapper.Map<AuctionDto>(auction));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto dto)
    {
        var auction = await _context.Auctions.Include(x=>x.Item).FirstOrDefaultAsync(x => x.Id == id);

        if(auction == null) return NotFound();

        // TODO: check seller == username when we introduce IdentityManagement
        auction.Item.Make = dto.Make ?? auction.Item.Make; // null-conditional operator: if dto.Make == null, then we keep original value
        auction.Item.Model = dto.Model ?? auction.Item.Model;
        auction.Item.Color = dto.Color ?? auction.Item.Color;
        auction.Item.Mileage = dto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = dto.Year ?? auction.Item.Year;
        auction.UpdatedAt = DateTime.UtcNow;

        var updatedAuction = _mapper.Map<AuctionDto>(auction);
        await _publishEndpoint.Publish(_mapper.Map<Contracts.AuctionUpdated>(updatedAuction));

        var result = await _context.SaveChangesAsync() > 0;
        if(result) return Ok();
        return BadRequest("There was no update happened");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var auction = await _context.Auctions.FindAsync(id);
        if(auction == null) return NotFound();

        // TODO: check seller name

        _context.Auctions.Remove(auction);
        var result = await _context.SaveChangesAsync() > 0;

        if(!result) return BadRequest("Could not remove auction record from DB");

        return Ok();
    }
}
