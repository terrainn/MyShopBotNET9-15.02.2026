using MyShopBotNET9.Models;
using System.Collections.Generic;

namespace MyShopBotNET9.Models;

public class AdminAddProductState
{
    public long AdminId { get; set; }
    public string? ProductName { get; set; }
    public decimal? Price { get; set; }
    public string? Description { get; set; }
    public int? StockQuantity { get; set; }
    public string? Category { get; set; }
    public string? City { get; set; }
    public string? ImageUrl { get; set; }
    public int CurrentStep { get; set; } = 0;
    public int? EditingProductId { get; set; }

    // Добавляем словарь для цен по граммовкам
    public Dictionary<decimal, decimal>? GramPrices { get; set; }
}

public enum AdminProductStep
{
    WaitingForName = 1,
    WaitingForPrice,
    WaitingForDescription,
    WaitingForStock,
    WaitingForCategory,
    WaitingForCity,
    WaitingForImage,
    WaitingForGramPrices, // ← Добавили новый шаг
    Completed
}