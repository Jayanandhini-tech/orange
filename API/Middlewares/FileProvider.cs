using Microsoft.Extensions.FileProviders;

namespace CMS.API.Middlewares;

public static class FileProvider
{
    public static WebApplication RegisterFileProviders(this WebApplication app)
    {
        string imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
        if (!Directory.Exists(imagesDir))
            Directory.CreateDirectory(imagesDir);

        string productPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Products");
        if (!Directory.Exists(productPath))
            Directory.CreateDirectory(productPath);

        string categoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Categories");
        if (!Directory.Exists(categoryPath))
            Directory.CreateDirectory(categoryPath);

        app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(imagesDir), RequestPath = "/images" });

        return app;
    }
}
