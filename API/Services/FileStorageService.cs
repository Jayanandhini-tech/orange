using CMS.API.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace CMS.API.Services;

public class FileStorageService : IFileStorageService
{
    public string StoreImage(IFormFile imageFile, string? imageSubPath = null)
    {
        Size size = new Size(200, 200);
        return StoreImage(imageFile, size, imageSubPath);
    }

    public string StoreImage(IFormFile imageFile, Size size, string? imageSubPath = null)
    {
        FileInfo imageInfo = new FileInfo(imageFile.FileName);
        string filename = $"{imageFile.FileName.Replace(imageInfo.Extension, "")}_{(new Random()).Next(1000, 9999)}{imageInfo.Extension}";
        string imagePath = string.IsNullOrEmpty(imageSubPath) ? Path.Combine("Images", filename) : Path.Combine("Images", imageSubPath, filename);
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
        StoreFile(imageFile, filePath, size);
        return imagePath.Replace("\\", "/");
    }

    public void StoreFile(IFormFile file, string filePath, Size size)
    {
        using var image = Image.Load(file.OpenReadStream());
        image.Mutate(i => i.Resize(new ResizeOptions
        {
            Size = size
        }));
        image.Save(filePath);
    }
}
