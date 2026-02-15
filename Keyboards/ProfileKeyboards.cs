using Telegram.Bot.Types.ReplyMarkups;

namespace MyShopBotNET9.Keyboards;

public static class ProfileKeyboards
{
    public static InlineKeyboardMarkup GetProfileKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🏙️ Изменить город", "change_city")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
            }
        });
    }
}