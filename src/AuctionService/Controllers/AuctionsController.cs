﻿using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionRepository _repo;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;

    public AuctionsController(  IAuctionRepository repo, 
                                IMapper mapper,
                                IPublishEndpoint publishEndpoint)
    {
        _repo = repo;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
    {
        return await _repo.GetAuctionsAsync(date);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
    {
        var auction = await _repo.GetAuctionByIdAsync(id);
        if(auction == null) return NotFound();
        
        return _mapper.Map<AuctionDto>(auction);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto dto)
    {
        var auction = _mapper.Map<Auction>(dto);
        // add current user as seller:
        auction.Seller = User.Identity.Name;

        _repo.AddAuction(auction);
        var newAuction = _mapper.Map<AuctionDto>(auction);
        await _publishEndpoint.Publish(_mapper.Map<Contracts.AuctionCreated>(newAuction));
        // both things (created auction and outbox message for MassTransit.RabbitMq)
        // are going to be saved as a part of a signle transaction:
        var result = await _repo.SaveChangesAsync();
        
        if(!result) return BadRequest("Could not save changes to the DB");
        
        return CreatedAtAction(nameof(GetAuctionById), new {auction.Id}, _mapper.Map<AuctionDto>(auction));
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto dto)
    {
        var auction = await _repo.GetAuctionEntityByIdAsync(id);

        if(auction == null) return NotFound();

        // check seller == username when we introduce IdentityManagement
        if(auction.Seller!=User.Identity.Name) return Forbid(); // HTTP 403 response

        auction.Item.Make = dto.Make ?? auction.Item.Make; // null-conditional operator: if dto.Make == null, then we keep original value
        auction.Item.Model = dto.Model ?? auction.Item.Model;
        auction.Item.Color = dto.Color ?? auction.Item.Color;
        auction.Item.Mileage = dto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = dto.Year ?? auction.Item.Year;
        auction.UpdatedAt = DateTime.UtcNow;

        await _publishEndpoint.Publish(_mapper.Map<Contracts.AuctionUpdated>(auction));

        var result = await _repo.SaveChangesAsync();
        if(result) return Ok();
        return BadRequest("There was no update happened");
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var auction = await _repo.GetAuctionEntityByIdAsync(id);
        if(auction == null) return NotFound();

        // check seller name:
        if(auction.Seller != User.Identity.Name) return Forbid();

        _repo.RemoveAuction(auction);
        await _publishEndpoint.Publish<AuctionDeleted>(new { Id = auction.Id.ToString() });

        var result = await _repo.SaveChangesAsync();

        if(!result) return BadRequest("Could not remove auction record from DB");

        return Ok();
    }
}
