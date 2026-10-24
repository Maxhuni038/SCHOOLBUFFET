namespace SchoolBuffet.Api.Services;

public static class FrontendLocator
{
    public static string Resolve(IWebHostEnvironment environment, string configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.IsPathRooted(configured)
                ? configured
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
        }

        DirectoryInfo? cursor = new(environment.ContentRootPath);
        while (cursor is not null)
        {
            string candidate = Path.Combine(cursor.FullName, "Frontend");
            if (File.Exists(Path.Combine(candidate, "index.html")))
            {
                return candidate;
            }

            cursor = cursor.Parent;
        }

        return Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "Frontend"));
    }

    public static string Uploads(IWebHostEnvironment environment, string configuredFrontend, string configuredUploads)
    {
        if (!string.IsNullOrWhiteSpace(configuredUploads))
        {
            return Path.IsPathRooted(configuredUploads)
                ? configuredUploads
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredUploads));
        }

        return Path.Combine(Resolve(environment, configuredFrontend), "assets", "img", "uploads");
    }
}
