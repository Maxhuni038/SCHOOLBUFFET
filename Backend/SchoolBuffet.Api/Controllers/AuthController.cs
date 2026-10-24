using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Services;
using System.Security.Claims;

namespace SchoolBuffet.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AdminUserDto>> Login([FromBody] LoginRequest request)
    {
        Models.User account = await _auth.AuthenticateAsync(request.Username, request.Password);

        ClaimsIdentity identity = new(
            new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new(ClaimTypes.Name, account.Username),
                new(ClaimTypes.GivenName, account.FullName),
                new(ClaimTypes.Role, account.Role)
            },
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return Ok(AuthService.ToDto(account));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new MessageDto("Dalu nga paneli i administratorit me sukses."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AdminUserDto>> Me()
    {
        return Ok(await _auth.GetAsync(this.User.AdminId()));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        await _auth.ChangePasswordAsync(this.User.AdminId(), request.CurrentPassword, request.NewPassword);
        return Ok(new MessageDto("Fjalakalimi u ndryshua me sukses."));
    }
}
