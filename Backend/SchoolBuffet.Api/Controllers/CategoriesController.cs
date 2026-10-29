using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Services;

namespace SchoolBuffet.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Manage)]
[Route("api/admin/categories")]
public class CategoriesController : ControllerBase
{
    private readonly CatalogService _catalog;

    public CategoriesController(CatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories([FromQuery] bool includeInactive = true)
    {
        return Ok(await _catalog.GetCategoriesAsync(includeInactive));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CategoryInput input)
    {
        return Ok(await _catalog.CreateCategoryAsync(input));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] CategoryInput input)
    {
        return Ok(await _catalog.UpdateCategoryAsync(id, input));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<MessageDto>> Delete(int id)
    {
        await _catalog.DeleteCategoryAsync(id);
        return Ok(new MessageDto("Kategoria u fshi me sukses."));
    }
}
