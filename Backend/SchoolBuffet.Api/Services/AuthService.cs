using Microsoft.EntityFrameworkCore;
using SchoolBuffet.Api.Data;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Models;

namespace SchoolBuffet.Api.Services;

public sealed class AuthService
{
    private const string GenericFailure = "Perdoruesi ose fjalakalimi nuk jane te sakte.";

    private readonly SchoolBuffetDbContext _db;

    public AuthService(SchoolBuffetDbContext db)
    {
        _db = db;
    }

    public async Task<User> AuthenticateAsync(string? username, string? password)
    {
        string normalized = (username ?? string.Empty).Trim().ToLowerInvariant();

        User? user = await _db.Users.FirstOrDefaultAsync(x => x.Username.ToLower() == normalized);

        if (user is null || !user.IsActive || !PasswordHasher.Verify(password ?? string.Empty, user.PasswordHash))
        {
            AppThrow.Unauthorized(GenericFailure);
        }

        if (PanelFor(user!.Role) is null)
        {
            AppThrow.Forbidden("Kjo llogari nuk ka qasje ne panelet e byfese.");
        }

        user.LastLoginUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return user;
    }

    public static string? PanelFor(string? role)
    {
        return role switch
        {
            AppRoles.Admin => "admin",
            AppRoles.Staff => "admin",
            AppRoles.Kitchen => "kitchen",
            _ => null
        };
    }

    public async Task ChangePasswordAsync(int userId, string? currentPassword, string? newPassword)
    {
        User user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId)
            ?? throw new AppException(System.Net.HttpStatusCode.NotFound, "Perdoruesi nuk u gjet.");

        ValidationBag bag = new();
        bag.Ensure(!string.IsNullOrWhiteSpace(currentPassword), nameof(ChangePasswordRequest.CurrentPassword), "Fjalakalimi aktual eshte i detyueshem");
        bag.Ensure(!string.IsNullOrWhiteSpace(newPassword), nameof(ChangePasswordRequest.NewPassword), "Fjalakalimi i ri eshte i detyueshem");

        string next = (newPassword ?? string.Empty).Trim();
        bag.Ensure(next.Length is >= 10 and <= 128, nameof(ChangePasswordRequest.NewPassword),
            "Fjalakalimi i ri duhet te kete te pakten 10 karaktere");
        bag.Ensure(next.Any(char.IsLetter) && next.Any(char.IsDigit),
            nameof(ChangePasswordRequest.NewPassword), "Fjalakalimi duhet te permbaje nje shkronje dhe nje numer");

        bag.Ensure(PasswordHasher.Verify(currentPassword ?? string.Empty, user.PasswordHash),
            nameof(ChangePasswordRequest.CurrentPassword), "Fjalakalimi aktual nuk eshte i sakte");

        bag.Ensure(!PasswordHasher.Verify(next, user.PasswordHash),
            nameof(ChangePasswordRequest.NewPassword), "Fjalakalimi i ri duhet te ndryshoje nga ai aktual");

        bag.ThrowIfInvalid("Fjalakalimi nuk u perditesua");

        user.PasswordHash = PasswordHasher.Hash(next);
        await _db.SaveChangesAsync();
    }

    public async Task<AdminUserDto> GetAsync(int userId)
    {
        User user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId)
            ?? throw new AppException(System.Net.HttpStatusCode.NotFound, "Perdoruesi nuk u gjet.");

        return ToDto(user);
    }

    public static AdminUserDto ToDto(User user)
    {
        return new AdminUserDto(
            user.Id,
            user.FullName,
            user.Username,
            user.Role,
            AppRoles.Label(user.Role),
            PanelFor(user.Role) ?? "admin",
            user.LastLoginUtc);
    }
}
