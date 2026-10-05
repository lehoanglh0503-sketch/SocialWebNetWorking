using System.Net;
using System.Net.Mail;

namespace Social_Website.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var host = _configuration["Email:Host"] ?? "smtp.gmail.com";
            var portString = _configuration["Email:Port"];
            int port = int.TryParse(portString, out int p) ? p : 587;
            var enableSsl = bool.TryParse(_configuration["Email:UseStartTls"], out bool ssl) ? ssl : true;
            var user = _configuration["Email:User"];
            var password = _configuration["Email:Password"];
            var fromAddress = _configuration["Email:FromAddress"] ?? user ?? "noreply@connectify.com";
            var fromName = _configuration["Email:FromName"] ?? "Connectify";

            // If SMTP credentials are not configured (e.g., in Dev mode), log the content safely
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogInformation("==========================================");
                _logger.LogInformation("DEV MODE: Email credentials not set in appsettings. Email would be sent to: {Email}", email);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation("Body: {Body}", htmlMessage);
                _logger.LogInformation("==========================================");
                return;
            }

            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(fromAddress, fromName);
                message.To.Add(new MailAddress(email));
                message.Subject = subject;
                message.Body = htmlMessage;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(user, password),
                    EnableSsl = enableSsl
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                // Do not rethrow to avoid breaking user request flow
            }
        }
    }
}
