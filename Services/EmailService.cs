using System.Net;
using System.Net.Mail;

namespace CollegeIssueManagement.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var section = _configuration.GetSection("SmtpSettings");
            var server = section["Server"] ?? "smtp.gmail.com";
            var portStr = section["Port"] ?? "587";
            var username = section["Username"] ?? string.Empty;
            var password = section["Password"] ?? string.Empty;
            var fromEmail = section["FromEmail"] ?? username;
            var fromName = section["FromName"] ?? "Texas College";

            int port = int.TryParse(portStr, out int p) ? p : 587;

            using var client = new SmtpClient(server, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 20000
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(to);
            await client.SendMailAsync(mail);
        }
    }
}