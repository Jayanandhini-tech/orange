using SixLabors.ImageSharp;

namespace CMS.API.Services.Interfaces
{
    public interface IFileStorageService
    {
        void StoreFile(IFormFile file, string filePath, Size size);
        string StoreImage(IFormFile imageFile, string? imageSubPath = null);
        string StoreImage(IFormFile imageFile, Size size, string? imageSubPath = null);
    }
}