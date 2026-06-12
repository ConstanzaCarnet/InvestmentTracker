using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Common.Authentication;
using Transactions.Application.Interfaces;
using Transactions.Application.DTOs;

namespace Transactions.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionController(ITransactionService service)
    {
        _service = service;
    }

    /// <summary>Records a buy transaction for the authenticated user (userId from the JWT).</summary>
    [HttpPost("buy")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Buy([FromBody] BuyRequest request)
    {
        var result = await _service.BuyAsync(User.GetUserId(), request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Records a sell transaction for the authenticated user (userId from the JWT).</summary>
    [HttpPost("sell")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Sell([FromBody] SellRequest request)
    {
        var result = await _service.SellAsync(User.GetUserId(), request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Returns the authenticated user's full transaction history.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTransactions()
    {
        var history = await _service.GetTransactionHistoryAsync(User.GetUserId());
        return Ok(history);
    }

    /// <summary>Returns the authenticated user's transactions for a specific instrument.</summary>
    [HttpGet("me/instrument/{instrumentId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTransactionsByInstrument(Guid instrumentId)
    {
        var transactions = await _service.GetTransactionHistoryByTickerAsync(User.GetUserId(), instrumentId);
        return Ok(transactions);
    }

    /// <summary>Returns the authenticated user's transactions on a given date.</summary>
    [HttpGet("me/date/{date}")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTransactionsByDate(DateTime date)
    {
        var transactions = await _service.GetTransactionHistoryByDateAsync(User.GetUserId(), date);
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
