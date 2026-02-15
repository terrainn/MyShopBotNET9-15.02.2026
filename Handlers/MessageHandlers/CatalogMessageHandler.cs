using Telegram.Bot;
using Telegram.Bot.Types;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Keyboards;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.MessageHandlers;

public class CatalogMessageHandler : IMessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ICatalogService _catalogService;
    private readonly UserService _userService;

    public CatalogMessageHandler(ITelegramBotClient botClient,
                               ICatalogService catalogService,
                               UserService userService)
    {
        _botClient = botClient;
        _catalogService = catalogService;
        _userService = userService;
    }

    public bool CanHandle(string message, BotState userState)
    {
        // ИСПРАВЛЕНО: Добавлены реальные команды для каталога
        return message == "📋 Каталог" ||
               message == "/catalog" ||
               message == "🛍️ Каталог товаров" ||
               message == "show_catalog";
    }

    public async Task HandleAsync(Message message, MyUser user, CancellationToken ct)
    {
        // Обновляем состояние пользователя
        await _userService.UpdateUserStateAsync(user.Id, BotState.Catalog);

        // Получаем категории с учетом города пользователя
        var categories = await _catalogService.GetCategoriesAsync(user.City);

        if (categories.Any())
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"🏪 **Категории товаров**\n\nВыберите категорию:",
                replyMarkup: CatalogKeyboards.GetCategoriesKeyboard(categories),
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: ct);
        }
        else
        {
            // Если категорий нет, предлагаем выбрать город
            if (string.IsNullOrEmpty(user.City))
            {
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "📍 **Сначала выберите город**\n\nДля просмотра каталога нужно указать ваш город.",
                    replyMarkup: CityKeyboards.GetCitiesKeyboard(),
                    cancellationToken: ct);
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"📭 В вашем городе ({user.City}) пока нет товаров",
                    cancellationToken: ct);
            }
        }
    }
}