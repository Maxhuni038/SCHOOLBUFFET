using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Services;

namespace SchoolBuffet.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/orders")]
public class AdminOrdersController : ControllerBase
{
    private readonly OrderService _orders;

    public AdminOrdersController(OrderService orders)
    {
        _orders = orders;
    }

    [HttpGet]
    public async Task<ActionResult<OrderListDto>> GetOrders(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] string? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return Ok(await _orders.ListAsync(status, search, date, page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> Get(int id)
    {
        return Ok(await _orders.GetAsync(id));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        return Ok(await _orders.UpdateStatusAsync(id, request.Status, this.User.AdminId()));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<MessageDto>> Delete(int id)
    {
        await _orders.DeleteAsync(id);
        return Ok(new MessageDto("Porosia u fshi nga regjistri."));
    }
}
