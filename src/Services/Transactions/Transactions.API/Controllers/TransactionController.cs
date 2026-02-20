using Microsoft.AspNetCore.Mvc;
using Transactions.Domain.Entities;
using Transactions.Application.Interfaces;
using Transactions.Application.DTOs;
using MassTransit;
using EventBus.Messages.Events;

namespace Transactions.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionController(ITransactionService service)
    {
        _transactionService = service;
    }

    [HttpPost("buy")]
    public async Task<IActionResult> Buy(BuyRequest request)
    {
        var result = await _transactionService.BuyAsync(request);
        return Ok(result);
    }

    [HttpPost("sell")]
    public async Task<IActionResult> Sell(SellRequest request)
    {
        var result = await _transactionService.SellAsync(request);
        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserTransactions(Guid userId)
    {
        var history = await _transactionService.GetTransactionHistoryAsync(userId);
        return Ok(history);
    }

    //busqueda por ticker segun usuario
    [HttpGet("user/{userId}/ticker/{ticker}")]
    public async Task<IActionResult> GetUserTransactionsByTicker(Guid userId, string ticker)
    {
        var transactions = await _transactionService.GetTransactionHistoryByTickerAsync(userId, ticker);
        return Ok(transactions);
    }

    //busqueda por fecha segun usuario
    [HttpGet("user/{userId}/date/{date}")]
    public async Task<IActionResult> GetUserTransactionsByDate(Guid userId, DateTime date)
    {
        var transactions = await _transactionService.GetTransactionHistoryByDateAsync(userId, date);
        return Ok(transactions);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, TransactionRequest request)
    {
        var result = await _transactionService.UpdateTransactionAsync(id, request);
        return result.Success ? Ok(result.Message) : NotFound(result.Message);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _transactionService.DeleteTransactionAsync(id);
        return result.Success ? Ok(result.Message) : NotFound(result.Message);
    }
}