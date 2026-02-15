using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MyShopBotNET9.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace MyShopBotNET9.Services;

public class TelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceProvider _serviceProvider;

    public TelegramBotService(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    {
        _botClient = botClient;
        _serviceProvider = serviceProvider;
    }

    public async Task StartBotAsync(CancellationToken ct = default)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
            ThrowPendingUpdates = true,
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: ct
        );

        var me = await _botClient.GetMeAsync(ct);
        Console.WriteLine($"🤖 Bot @{me.Username} started successfully!");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        try
        {
            // Создаем новый scope для каждого обновления
            using var scope = _serviceProvider.CreateScope();
            var botHandler = scope.ServiceProvider.GetRequiredService<TelegramBotHandler>();
            await botHandler.HandleUpdateAsync(update, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error handling update: {ex.Message}");
        }
    }

    private async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        Console.WriteLine($"❌ Telegram Bot polling error: {exception.Message}");
        await Task.CompletedTask;
    }
}