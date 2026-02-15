using System.ComponentModel.DataAnnotations;

namespace MyShopBotNET9.Models;

public class User
{
    [Key]
    public long Id { get; set; }  // Это и есть ChatId в Telegram
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public BotState CurrentState { get; set; } = BotState.MainMenu;
    public string? City { get; set; }
    public bool IsAdmin { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    public List<CartItem> CartItems { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
}