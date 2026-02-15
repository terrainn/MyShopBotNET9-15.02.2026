using System.Collections.Generic;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyShopBotNET9.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public string? PricesJson { get; set; }

    [NotMapped] // ← ВАЖНО: EF Core будет игнорировать это свойство
    public Dictionary<decimal, decimal> GramPrices
    {
        get
        {
            if (string.IsNullOrEmpty(PricesJson))
                return new Dictionary<decimal, decimal> { { 1.0m, Price } };

            try
            {
                return JsonSerializer.Deserialize<Dictionary<decimal, decimal>>(PricesJson)
                       ?? new Dictionary<decimal, decimal> { { 1.0m, Price } };
            }
            catch
            {
                return new Dictionary<decimal, decimal> { { 1.0m, Price } };
            }
        }
        set
        {
            PricesJson = JsonSerializer.Serialize(value);
            if (value.ContainsKey(1.0m))
                Price = value[1.0m];
        }
    }

    public string? Description { get; set; }
    public int StockQuantity { get; set; }
    public string? Category { get; set; }
    public string? City { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public List<CartItem> CartItems { get; set; } = new();
}