namespace MyShopBotNET9.Models;

public class AdminUser
{
    public int Id { get; set; }
    public long TelegramUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}