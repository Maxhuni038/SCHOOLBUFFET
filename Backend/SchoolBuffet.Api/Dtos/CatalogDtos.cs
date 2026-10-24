namespace SchoolBuffet.Api.Dtos;

public record CategoryDto(
    int Id,
    string Name,
    string Icon,
    string AccentColor,
    string Description,
    int SortOrder,
    bool IsActive,
    int ProductCount,
    DateTime CreatedAtUtc);

public record CategoryInput(
    string? Name,
    string? Icon,
    string? AccentColor,
    string? Description,
    int? SortOrder,
    bool? IsActive);

public record ProductDto(
    int Id,
    int CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryAccentColor,
    string Name,
    string Description,
    decimal Price,
    string ImagePath,
    string Badge,
    bool IsRecommended,
    int SortOrder,
    bool IsActive,
    int TimesOrdered,
    DateTime CreatedAtUtc);

public record ProductInput(
    int? CategoryId,
    string? Name,
    string? Description,
    decimal? Price,
    string? ImagePath,
    string? Badge,
    bool? IsRecommended,
    int? SortOrder,
    bool? IsActive);

public record MenuItemDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    string ImagePath,
    string Badge,
    bool IsRecommended);

public record MenuCategoryDto(
    int Id,
    string Name,
    string Icon,
    string AccentColor,
    string Description,
    List<MenuItemDto> Products);

public record MenuDto(List<MenuCategoryDto> Categories, DateTime GeneratedAtUtc, string Currency);
