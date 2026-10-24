using Microsoft.AspNetCore.Mvc;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Services;

namespace SchoolBuffet.Api.Controllers;

[ApiController]
[Route("api")]
public class MenuController : ControllerBase
{
    private readonly CatalogService _catalog;
    private readonly AppSettings _settings;

    public MenuController(CatalogService catalog, AppSettings settings)
    {
        _catalog = catalog;
        _settings = settings;
    }

    [HttpGet("menu")]
    public async Task<ActionResult<MenuDto>> GetMenu()
    {
        return Ok(await _catalog.GetMenuAsync(_settings.CurrencyLabel));
    }
}
