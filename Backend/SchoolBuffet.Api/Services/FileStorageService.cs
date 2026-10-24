using Microsoft.AspNetCore.Http;

namespace SchoolBuffet.Api.Services;

public sealed class FileStorageService
{
    public const string PublicFolder = "/assets/img/uploads/";

    private static readonly Dictionary<string, string[]> Signatures = new()
    {
        [".jpg"] = new[] { "image/jpeg", "image/jpg", "image/pjpeg" },
        [".jpeg"] = new[] { "image/jpeg", "image/jpg", "image/pjpeg" },
        [".png"] = new[] { "image/png" },
        [".webp"] = new[] { "image/webp", "image/vnd.webp" }
    };

    private readonly AppSettings _settings;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _uploadFolder;

    public FileStorageService(AppSettings settings, IWebHostEnvironment environment, ILogger<FileStorageService> logger)
    {
        _settings = settings;
        _logger = logger;
        _uploadFolder = FrontendLocator.Uploads(environment, settings.FrontendPath, settings.UploadPath);
    }

    public async Task<string> SaveAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            AppThrow.BadRequest("Zgjidh nje foto per te ngarkuar.");
        }

        long maxBytes = (long)_settings.MaxUploadKilobytes * 1024L;
        if (file!.Length > maxBytes)
        {
            AppThrow.BadRequest($"Fotoja nuk mund te tejkaloje {_settings.MaxUploadKilobytes} KB.");
        }

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Signatures.TryGetValue(extension, out string[]? contentTypes))
        {
            AppThrow.BadRequest("Formatet e lejuara te fotos jane: jpg, jpeg, png, webp.");
        }

        string mediaType = (file.ContentType ?? string.Empty).ToLowerInvariant();
        if (!contentTypes!.Contains(mediaType))
        {
            AppThrow.BadRequest("Lloji i file-it nuk perputhet me fotos e lejuara.");
        }

        using MemoryStream buffer = new();
        await file.CopyToAsync(buffer);
        byte[] bytes = buffer.ToArray();

        if (!LooksLikeImage(extension, bytes))
        {
            AppThrow.BadRequest("File-i i ngarkuar nuk eshte nje foto e vlefshme.");
        }

        string folder = _uploadFolder;
        Directory.CreateDirectory(folder);

        string fileName = $"foto-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..12]}{extension}";
        string fullPath = Path.Combine(folder, fileName);

        await File.WriteAllBytesAsync(fullPath, bytes);
        _logger.LogInformation("Foto e re e ruajtur: {FileName}", fileName);

        return PublicFolder + fileName;
    }

    public void DeleteManaged(string? publicPath)
    {
        if (string.IsNullOrWhiteSpace(publicPath) || !publicPath.StartsWith(PublicFolder, StringComparison.Ordinal))
        {
            return;
        }

        string fileName = Path.GetFileName(publicPath);
        if (!fileName.StartsWith("foto-", StringComparison.Ordinal) || fileName.Length > 80)
        {
            return;
        }

        string fullPath = Path.Combine(_uploadFolder, fileName);
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Fotoja nuk u fshie: {FileName}", fileName);
        }
    }

    private static bool LooksLikeImage(string extension, byte[] bytes)
    {
        if (bytes.Length < 12)
        {
            return false;
        }

        return extension switch
        {
            ".jpg" or ".jpeg" => bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            ".png" => bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47
                && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A,
            ".webp" => bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
                && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P',
            _ => false
        };
    }
}
