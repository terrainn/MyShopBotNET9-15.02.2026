using Telegram.Bot.Types.ReplyMarkups;

namespace MyShopBotNET9.Keyboards;

public static class CityKeyboards
{
    public static InlineKeyboardMarkup GetCitiesKeyboard()
    {
        var cities = new[]
        {
            "Москва", "Санкт-Петербург", "Новосибирск", "Екатеринбург", "Казань",
            "Нижний Новгород", "Челябинск", "Самара", "Омск", "Ростов-на-Дону"
        };

        var buttons = new List<InlineKeyboardButton[]>();

        // Создаем кнопки по 2 в ряд
        for (int i = 0; i < cities.Length; i += 2)
        {
            var row = new List<InlineKeyboardButton>();
            row.Add(InlineKeyboardButton.WithCallbackData(cities[i], $"city_{cities[i]}"));

            if (i + 1 < cities.Length)
            {
                row.Add(InlineKeyboardButton.WithCallbackData(cities[i + 1], $"city_{cities[i + 1]}"));
            }

            buttons.Add(row.ToArray());
        }

        // Добавляем кнопку возврата
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔙 Назад к профилю", "back_to_profile")
        });

        return new InlineKeyboardMarkup(buttons);
    }
}