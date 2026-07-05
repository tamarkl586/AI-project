using DrawReportService.BLL.Interfaces;
using MailKit.Net.Smtp;
using MimeKit;

namespace DrawReportService.BLL.Implementations;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var host = _config["EmailSettings:Host"] ?? throw new InvalidOperationException("EmailSettings:Host is missing in configuration");
        var portStr = _config["EmailSettings:Port"];
        var port = int.TryParse(portStr, out var parsedPort) ? parsedPort : 587;
        var username = _config["EmailSettings:Username"] ?? throw new InvalidOperationException("EmailSettings:Username is missing in configuration");
        var password = _config["EmailSettings:Password"]?.Replace(" ", string.Empty) ?? throw new InvalidOperationException("EmailSettings:Password is missing in configuration");
        var from = _config["EmailSettings:From"] ?? username;

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(from));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;
        email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

        using var smtp = new SmtpClient();
        try
        {
            _logger.LogInformation("Attempting to send email to {Email} via {Host}:{Port}", toEmail, host, port);

            await smtp.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username, password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (MailKit.Security.AuthenticationException ex)
        {
            _logger.LogError(ex, "SMTP authentication failed for {Username}.", username);
            throw new InvalidOperationException("שגיאה באימות מול שרת המייל – יש לעדכן את סיסמת האפליקציה (App Password) בהגדרות.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw new InvalidOperationException("שגיאה בשליחת המייל, אך הזוכה נשמר במערכת.", ex);
        }
    }
}
