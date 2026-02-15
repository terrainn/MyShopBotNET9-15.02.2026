using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyShopBotNET9.Models;

public class SupportMessage
{
    [Key]
    public int Id { get; set; }

    /// <summary>ID заказа, к которому относится сообщение</summary>
    public int OrderId { get; set; }

    /// <summary>ID отправителя (клиента или админа)</summary>
    public long SenderId { get; set; }

    /// <summary>Тип отправителя</summary>
    public SenderType SenderType { get; set; }

    /// <summary>Текст сообщения (может быть null, если только фото)</summary>
    public string? MessageText { get; set; }

    /// <summary>ID фото в Telegram (если есть)</summary>
    public string? PhotoFileId { get; set; }

    /// <summary>Время отправки</summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>Прочитано ли сообщение админом</summary>
    public bool IsReadByAdmin { get; set; }

    /// <summary>Прочитано ли сообщение клиентом</summary>
    public bool IsReadByClient { get; set; }

    // Навигационные свойства
    [ForeignKey("OrderId")]
    public Order? Order { get; set; }
}

public enum SenderType
{
    Client,     // Клиент магазина
    Admin       // Администратор
}