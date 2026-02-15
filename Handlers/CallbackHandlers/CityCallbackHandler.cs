using Telegram.Bot;
using Telegram.Bot.Types;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Keyboards;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.CallbackHandlers;

public class CityCallbackHandler : ICallbackHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserService _userService;

    public CityCallbackHandler(ITelegramBotClient botClient, UserService userService)
    {
        _botClient = botClient;
        _userService = userService;
    }

    public bool CanHandle(string callbackData) =>
        callbackData?.StartsWith("city_") == true || callbackData == "change_city";

    public async Task HandleAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        if (callback.Message == null) return;

        var data = callback.Data!;

        try
        {
            if (data == "change_city")
            {
                // Показываем меню выбора города
                await _botClient.EditMessageTextAsync(
                    chatId: callback.Message.Chat.Id,
                    messageId: callback.Message.MessageId,
                    text: "🏙️ **Выберите ваш город:**",
                    replyMarkup: CityKeyboards.GetCitiesKeyboard(),
                    cancellationToken: ct);
                return;
            }

            // Обработка выбора конкретного города
            if (data.StartsWith("city_"))
            {
                var cityName = data.Replace("city_", "");

                // Обновляем город пользователя
                await _userService.UpdateUserCityAsync(user.Id, cityName);

                await _botClient.EditMessageTextAsync(
                    chatId: callback.Message.Chat.Id,
                    messageId: callback.Message.MessageId,
                    text: $"✅ Город установлен: {cityName}\n\nТеперь вы можете пользоваться магазином!",
                    replyMarkup: Keyboards.InlineKeyboards.GetMainMenuKeyboard(),
                    cancellationToken: ct);

                await _botClient.AnswerCallbackQueryAsync(callback.Id, $"Город: {cityName}", cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error handling city callback: {ex.Message}");
            await _botClient.AnswerCallbackQueryAsync(callback.Id, "❌ Ошибка выбора города", cancellationToken: ct);
        }
    }
}