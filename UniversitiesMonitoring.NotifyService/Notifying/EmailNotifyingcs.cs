using System.Net;
using System.Net.Mail;
using UniversityMonitoring.Data;
using UniversityMonitoring.Data.Entities;

namespace UniversitiesMonitoring.NotifyService.Notifying;

internal class EmailNotifier
{
    private readonly SmtpClient _emailClient;
    private readonly MailAddress _mailAddress;

    public EmailNotifier(IConfiguration configuration)
    {
        var address = Environment.GetEnvironmentVariable("EMAIL_ADDRESS") ?? configuration["Email:Username"];
        _mailAddress = new MailAddress(address);
        _emailClient = new SmtpClient()
        {
            Host = Environment.GetEnvironmentVariable("SMTP_HOST")!,
            Port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT")!),
            Credentials = new NetworkCredential(address,
                Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? configuration["Email:Password"]),
            EnableSsl = false
        };
    }

    public async Task NotifyAsync(UserEntity userEntity, UniversityServiceEntity serviceEntity)
    {
        if (userEntity.Email == null) return;
        var message = CreateMailMessage(serviceEntity);
        message.To.Add(userEntity.Email);

        await _emailClient.SendMailAsync(message);
    }

    private MailMessage CreateMailMessage(UniversityServiceEntity service) =>
        new()
        {
            From = _mailAddress,
            Subject = "Изменение состояния сервиса",
            Body =
                $"<b>📢 Сервис <a href=\"{service.GenerateUrl()}\">{service.ServiceName}</a> ВУЗа <a href=\"{service.Url}\">{service.UniversityName}</a> изменил свое состояние на {(service.IsOnline ? "онлайн 🟢" : "офлайн 🔴")}</b><br/>",
            IsBodyHtml = true
        };
}