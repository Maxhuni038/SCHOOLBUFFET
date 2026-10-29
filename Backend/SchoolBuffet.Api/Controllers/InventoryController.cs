using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Services;

namespace SchoolBuffet.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Manage)]
[Route("api/admin/inventory")]
public class InventoryController : ControllerBase
{
    private readonly StockService _stock;

    public InventoryController(StockService stock)
    {
        _stock = stock;
    }

    [HttpGet]
    public async Task<ActionResult<StockListDto>> GetBoard(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] string? filter)
    {
        return Ok(await _stock.GetBoardAsync(search, categoryId, filter));
    }

    [HttpGet("movements")]
    public async Task<ActionResult<StockMovementsDto>> GetMovements(
        [FromQuery] int? productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        return Ok(await _stock.GetMovementsAsync(productId, page, pageSize));
    }

    [HttpPatch("{productId:int}/adjust")]
    public async Task<ActionResult<StockItemDto>> Adjust(int productId, [FromBody] StockAdjustRequest request)
    {
        return Ok(await _stock.AdjustAsync(productId, request.Change, request.Reason, request.Note, this.User.UserId()));
    }
}
