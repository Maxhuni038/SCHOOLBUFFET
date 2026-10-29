using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Services;

namespace SchoolBuffet.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Board)]
[Route("api/kitchen")]
public class KitchenController : ControllerBase
{
    private readonly KitchenService _kitchen;
    private readonly OrderService _orders;

    public KitchenController(KitchenService kitchen, OrderService orders)
    {
        _kitchen = kitchen;
        _orders = orders;
    }

    [HttpGet("board")]
    public async Task<ActionResult<KitchenBoardDto>> Board()
    {
        return Ok(await _kitchen.GetBoardAsync());
    }

    [HttpPost("{id:int}/start")]
    public async Task<ActionResult<OrderDto>> Start(int id)
    {
        return Ok(await _orders.AdvanceAsync(id, "start", this.User.UserId(), CanClose));
    }

    [HttpPost("{id:int}/ready")]
    public async Task<ActionResult<OrderDto>> Ready(int id)
    {
        return Ok(await _orders.AdvanceAsync(id, "ready", this.User.UserId(), CanClose));
    }

    [HttpPost("{id:int}/back")]
    public async Task<ActionResult<OrderDto>> Back(int id)
    {
        return Ok(await _orders.AdvanceAsync(id, "back", this.User.UserId(), CanClose));
    }

    [HttpPost("{id:int}/done")]
    public async Task<ActionResult<OrderDto>> Done(int id)
    {
        return Ok(await _orders.AdvanceAsync(id, "done", this.User.UserId(), CanClose));
    }

    private bool CanClose => AppRoles.CanManageOrders(User);
}
