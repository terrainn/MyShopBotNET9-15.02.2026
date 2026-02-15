using Microsoft.EntityFrameworkCore;
using MyShopBotNET9.Data;
using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public class SupportService
{
    private readonly AppDbContext _context;
    private readonly NotificationService _notificationService;

    public SupportService(AppDbContext context, NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    /// <summary>Получить историю переписки по заказу</summary>
    public async Task<List<SupportMessage>> GetOrderChatHistoryAsync(int orderId)
    {
        return await _context.Set<SupportMessage>()
            .Where(m => m.OrderId == orderId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    /// <summary>Сохранить сообщение от клиента</summary>
    public async Task<SupportMessage> SaveClientMessageAsync(int orderId, long clientId, string? text, string? photoFileId = null)
    {
        var message = new SupportMessage
        {
            OrderId = orderId,
            SenderId = clientId,
            SenderType = SenderType.Client,
            MessageText = text,
            PhotoFileId = photoFileId,
            SentAt = DateTime.UtcNow,
            IsReadByAdmin = false,
            IsReadByClient = true
        };

        _context.Set<SupportMessage>().Add(message);
        await _context.SaveChangesAsync();

        return message;
    }

    /// <summary>Сохранить ответ от админа</summary>
    public async Task<SupportMessage> SaveAdminMessageAsync(int orderId, long adminId, string? text, string? photoFileId = null)
    {
        var message = new SupportMessage
        {
            OrderId = orderId,
            SenderId = adminId,
            SenderType = SenderType.Admin,
            MessageText = text,
            PhotoFileId = photoFileId,
            SentAt = DateTime.UtcNow,
            IsReadByAdmin = true,
            IsReadByClient = false
        };

        _context.Set<SupportMessage>().Add(message);
        await _context.SaveChangesAsync();

        return message;
    }

    /// <summary>Отметить сообщения как прочитанные админом</summary>
    public async Task MarkMessagesAsReadByAdminAsync(int orderId)
    {
        var messages = await _context.Set<SupportMessage>()
            .Where(m => m.OrderId == orderId && !m.IsReadByAdmin && m.SenderType == SenderType.Client)
            .ToListAsync();

        foreach (var msg in messages)
        {
            msg.IsReadByAdmin = true;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>Отметить сообщения как прочитанные клиентом</summary>
    public async Task MarkMessagesAsReadByClientAsync(int orderId, long clientId)
    {
        var messages = await _context.Set<SupportMessage>()
            .Where(m => m.OrderId == orderId && !m.IsReadByClient && m.SenderType == SenderType.Admin)
            .ToListAsync();

        foreach (var msg in messages)
        {
            msg.IsReadByClient = true;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>Проверить, есть ли непрочитанные сообщения у клиента</summary>
    public async Task<bool> HasUnreadMessagesForClientAsync(long clientId)
    {
        return await _context.Set<SupportMessage>()
            .AnyAsync(m => m.SenderType == SenderType.Admin &&
                          !m.IsReadByClient &&
                          m.Order != null &&
                          m.Order.UserId == clientId);
    }

    /// <summary>Получить все заказы с непрочитанными сообщениями для админа</summary>
    public async Task<List<int>> GetOrdersWithUnreadMessagesAsync()
    {
        return await _context.Set<SupportMessage>()
            .Where(m => !m.IsReadByAdmin && m.SenderType == SenderType.Client)
            .Select(m => m.OrderId)
            .Distinct()
            .ToListAsync();
    }
}