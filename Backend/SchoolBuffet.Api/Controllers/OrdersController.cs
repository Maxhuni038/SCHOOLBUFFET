using Microsoft.AspNetCore.Mvc;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Services;

namespace SchoolBuffet.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orders;

    public OrdersController(OrderService orders)
    {
        _orders = orders;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest request)
    {
        OrderDto created = await _orders.CreateAsync(request);
        return Ok(created);
    }

    [HttpGet("track/{code}")]
    public async Task<ActionResult<OrderDto>> Track(string code)
    {
        return Ok(await _orders.TrackAsync(code));
    }
}
