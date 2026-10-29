using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Services;

namespace SchoolBuffet.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Manage)]
[Route("api/admin/reports")]
public class ReportsController : ControllerBase
{
    private readonly SalesService _sales;

    public ReportsController(SalesService sales)
    {
        _sales = sales;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<SalesSummaryDto>> Summary([FromQuery] int days = 7)
    {
        return Ok(await _sales.GetSummaryAsync(days));
    }

    [HttpGet("history")]
    public async Task<ActionResult<SalesHistoryDto>> History([FromQuery] string? from, [FromQuery] string? to)
    {
        return Ok(await _sales.GetHistoryAsync(from, to));
    }

    [HttpGet("export.csv")]
    public async Task<IActionResult> Export([FromQuery] string? from, [FromQuery] string? to)
    {
        string csv = await _sales.BuildCsvAsync(from, to);
        byte[] payload = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        string fileName = $"shitjet-{DateTime.Now:yyyyMMdd-HHmm}.csv";
        return File(payload, "text/csv; charset=utf-8", fileName);
    }
}
