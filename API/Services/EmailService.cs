using MailKit.Net.Smtp;
using MimeKit;

namespace CMS.API.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> logger;

    public EmailService(ILogger<EmailService> logger)
    {
        this.logger = logger;
    }

    public async Task<bool> SendEmail(List<string> ToAddress, string Subject, string Body, string FilePath)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("BVC", "reports@bvc24.com"));
            email.To.AddRange(ToAddress.Select(a => new MailboxAddress(a, a)).ToList());
            email.Bcc.Add(new MailboxAddress("Siva", "siva@bvc24.com"));
            email.Subject = Subject;

            var builder = new BodyBuilder();
            builder.TextBody = Body;
            await builder.Attachments.AddAsync(FilePath);

            email.Body = builder.ToMessageBody();

            using (var smtp = new SmtpClient())
            {
                smtp.Connect("mail.bvc24.com", 587, false);
                smtp.Authenticate("reports@bvc24.com", "Bvc$5Tcv!12");
                var response = smtp.Send(email);
                smtp.Disconnect(true);
                logger.LogInformation(response);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }
}
