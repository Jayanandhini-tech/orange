
namespace CMS.API.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmail(List<string> ToAddress, string Subject, string Body, string FilePath);
    }
}