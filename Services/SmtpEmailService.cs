using MailKit.Net.Smtp;
using MimeKit;

namespace PingCRM.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var smtpConfig = _configuration.GetSection("Smtp");
        var host = smtpConfig["Host"] ?? "localhost";
        var port = smtpConfig.GetValue<int>("Port", 587);
        var username = smtpConfig["Username"];
        var password = smtpConfig["Password"];
        var fromAddress = smtpConfig["FromAddress"] ?? "noreply@pingcrm.com";
        var fromName = smtpConfig["FromName"] ?? "PingCRM";
        var useSsl = smtpConfig.GetValue<bool>("UseSsl", true);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(host, port, useSsl);

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password);
            }

            await client.SendAsync(message);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}
