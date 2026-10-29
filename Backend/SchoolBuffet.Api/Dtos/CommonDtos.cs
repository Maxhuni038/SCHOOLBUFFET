namespace SchoolBuffet.Api.Dtos;

public record ErrorResponse(string Message, Dictionary<string, List<string>>? Errors = null);

public record MessageDto(string Message);

public record LoginRequest(string? Username, string? Password);

public record AdminUserDto(
    int Id,
    string FullName,
    string Username,
    string Role,
    string RoleLabel,
    string Panel,
    DateTime? LastLoginUtc);

public record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
