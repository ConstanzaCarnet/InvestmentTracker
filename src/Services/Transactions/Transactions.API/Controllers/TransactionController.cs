using Microsoft.AspNetCore.Mvc;
using Transactions.Application.Interfaces;
using Transactions.Application.DTOs;

namespace Transactions.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionController(ITransactionService service)
    {
        _service = service;
    }

    /// <summary>Records a buy transaction for the given instrument.</summary>
    [HttpPost("buy")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Buy([FromBody] BuyRequest request)
    {
        var result = await _service.BuyAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Records a sell transaction for the given instrument.</summary>
    [HttpPost("sell")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Sell([FromBody] SellRequest request)
    {
        var result = await _service.SellAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Returns the full transaction history for a user.</summary>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserTransactions(Guid userId)
    {
        var history = await _service.GetTransactionHistoryAsync(userId);
        return Ok(history);
    }

    /// <summary>Returns transactions for a specific user and instrument.</summary>
    [HttpGet("user/{userId:guid}/instrument/{instrumentId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserTransactionsByInstrument(Guid userId, Guid instrumentId)
    {
        var transactions = await _service.GetTransactionHistoryByTickerAsync(userId, instrumentId);
        return Ok(transactions);
    }

    /// <summary>Returns transactions for a specific user on a given date.</summary>
    [HttpGet("user/{userId:guid}/date/{date}")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserTransactionsByDate(Guid userId, DateTime date)
    {
        var transactions = await _service.GetTransactionHistoryByDateAsync(userId, date);
        return Ok(transactions);
    }

    /// <summary>Updates an existing transaction. Ticker must be a valid, active instrument.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TransactionRequest request)
    {
        var (success, message) = await _service.UpdateTransactionAsync(id, request);
        return success ? Ok(message) : NotFound(message);
    }

    /// <summary>Deletes a transaction and publishes a reversal event to Holdings.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (success, message) = await _service.DeleteTransactionAsync(id);
        return success ? Ok(message) : NotFound(message);
    }
}
