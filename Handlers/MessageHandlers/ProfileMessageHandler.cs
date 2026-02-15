using Telegram.Bot;
using Telegram.Bot.Types;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Keyboards;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.MessageHandlers;

public class ProfileMessageHandler : IMessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserService _userService;

    public ProfileMessageHandler(ITelegramBotClient botClient, UserService userService)
    {
        _botClient = botClient;
        _userService = userService;
    }

    public bool CanHandle(string message, BotState userState)
    {
        Console.WriteLine($"🔍 ProfileHandler проверяет: '{message}', state: {userState}");

        if (string.IsNullOrEmpty(message)) return false;

        // Проверяем разные варианты текста для профиля
        bool isProfileCommand = message.Contains("профиль", StringComparison.OrdinalIgnoreCase) ||
                               message.Contains("profile", StringComparison.OrdinalIgnoreCase) ||
                               message == "/profile" ||
                               message == "👤 Профиль" ||
                               message == "show_profile";

        Console.WriteLine($"📊 ProfileHandler результат: {isProfileCommand}");
        return isProfileCommand;
    }

    public async Task HandleAsync(Message message, MyUser user, CancellationToken ct)
    {
        Console.WriteLine($"🔄 Profile handler called for user {user.Id}");

        // Обновляем активность пользователя
        await _userService.UpdateUserStateAsync(user.Id, BotState.Profile);

        var cityText = string.IsNullOrEmpty(user.City)
            ? "🌍 Город не выбран"
            : $"🏙️ Город: {user.City}";

        var usernameText = string.IsNullOrEmpty(user.Username)
            ? "👤 Username: не установлен"
            : $"👤 @{user.Username}";

        var profileText = $"👤 **Ваш профиль**\n\n" +
                         $"🆔 ID: {user.Id}\n" +
                         $"{usernameText}\n" +
                         $"👤 Имя: {user.FirstName ?? "Не указано"}\n" +
                         $"{cityText}\n" +
                         (user.IsAdmin ? "👑 **АДМИНИСТРАТОР**\n" : "") +
                         $"\n📅 Регистрация: {user.CreatedAt:dd.MM.yyyy}";

        Console.WriteLine($"📤 Sending profile to user {user.Id}");

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: profileText,
            replyMarkup: ProfileKeyboards.GetProfileKeyboard(),
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: ct);

        Console.WriteLine($"✅ Profile sent to user {user.Id}");
    }
}