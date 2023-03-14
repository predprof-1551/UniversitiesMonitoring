using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniversityMonitoring.Data;
using UniversityMonitoring.Data.Entities;

namespace UniversitiesMonitoring.NotifyService.Notifying;

internal class GlobalTelegramNotifying
{   
    private readonly TelegramBotClient _botClient;
    private readonly ChatId _globalChatId;

    public GlobalTelegramNotifying(IConfiguration configuration)
    {
        _botClient = new TelegramBotClient(
            Environment.GetEnvironmentVariable("TELEGRAM_TOKEN") ??
            configuration["TelegramToken"]);
        _globalChatId = Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID") ??
                        configuration["TelegramChatId"];
    }

    public async Task NotifyAsync(IEnumerable<UniversityServiceEntity> changedServices)
    {
        var services = changedServices.ToArray();
        var reportBuilder = new StringBuilder("⚡️📢 *Появились сервисы, которые изменили свое состояние*\n");

        foreach (var service in services)
        {
            reportBuilder.AppendLine(GenerateStatus(service));
        }

        var report = reportBuilder.ToString();

        await _botClient.SendTextMessageAsync(_globalChatId, report, ParseMode.Markdown);
    }

    private string GenerateStatus(UniversityServiceEntity serviceEntity) =>
        $"- Сервис [\"{serviceEntity.ServiceName}\"]({serviceEntity.GenerateUrl()}) ВУЗа [\"{serviceEntity.UniversityName}\"]({serviceEntity.Url}) сменил свое состояние на {(serviceEntity.IsOnline ? "онлайн 🟢" : "оффлайн 🔴")}";
}