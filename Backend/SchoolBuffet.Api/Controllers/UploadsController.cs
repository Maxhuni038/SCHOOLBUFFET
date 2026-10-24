using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolBuffet.Api.Services;

namespace SchoolBuffet.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/uploads")]
public class UploadsController : ControllerBase
{
    private readonly FileStorageService _storage;

    public UploadsController(FileStorageService storage)
    {
        _storage = storage;
    }

    [HttpPost("image")]
    [RequestSizeLimit(5_000_000)]
    public async Task<ActionResult<object>> UploadImage(IFormFile? file)
    {
        string path = await _storage.SaveAsync(file);
        return Ok(new { path });
    }
}
