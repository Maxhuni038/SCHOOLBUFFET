using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Services;

namespace SchoolBuffet.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Manage)]
[Route("api/admin/payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _payments;

    public PaymentsController(PaymentService payments)
    {
        _payments = payments;
    }

    [HttpGet]
    public async Task<ActionResult<PaymentListDto>> List(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        return Ok(await _payments.ListAsync(from, to, status, page, pageSize));
    }

    [HttpPatch("{id:int}/paid")]
    public async Task<ActionResult<PaymentDto>> MarkPaid(int id, [FromBody] PaymentActionRequest request)
    {
        return Ok(await _payments.MarkPaidAsync(id, request.Method, request.Note, this.User.UserId()));
    }

    [HttpPatch("{id:int}/pending")]
    public async Task<ActionResult<PaymentDto>> MarkPending(int id, [FromBody] PaymentActionRequest request)
    {
        return Ok(await _payments.MarkPendingAsync(id, request.Note, this.User.UserId()));
    }

    [HttpPatch("{id:int}/void")]
    public async Task<ActionResult<PaymentDto>> Void(int id, [FromBody] PaymentActionRequest request)
    {
        return Ok(await _payments.VoidAsync(id, request.Note, this.User.UserId()));
    }

    [HttpPatch("{id:int}/method")]
    public async Task<ActionResult<PaymentDto>> SetMethod(int id, [FromBody] PaymentActionRequest request)
    {
        return Ok(await _payments.SetMethodAsync(id, request.Method));
    }
}
