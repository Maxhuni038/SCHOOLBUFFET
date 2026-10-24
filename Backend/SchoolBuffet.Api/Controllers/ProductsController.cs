using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Services;

namespace SchoolBuffet.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/products")]
public class ProductsController : ControllerBase
{
    private readonly CatalogService _catalog;
    private readonly FileStorageService _storage;

    public ProductsController(CatalogService catalog, FileStorageService storage)
    {
        _catalog = catalog;
        _storage = storage;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] bool includeInactive = true)
    {
        return Ok(await _catalog.GetProductsAsync(search, categoryId, includeInactive));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> Get(int id)
    {
        return Ok(await _catalog.GetProductAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] ProductInput input)
    {
        return Ok(await _catalog.CreateProductAsync(input));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] ProductInput input)
    {
        ProductDto before = await _catalog.GetProductAsync(id);
        ProductDto after = await _catalog.UpdateProductAsync(id, input);

        if (!string.Equals(before.ImagePath, after.ImagePath, StringComparison.Ordinal))
        {
            _storage.DeleteManaged(before.ImagePath);
        }

        return Ok(after);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<MessageDto>> Delete(int id)
    {
        ProductDto product = await _catalog.GetProductAsync(id);
        await _catalog.DeleteProductAsync(id);
        _storage.DeleteManaged(product.ImagePath);

        return Ok(new MessageDto($"Produkti {product.Name} u fshi nga byfeja."));
    }
}
