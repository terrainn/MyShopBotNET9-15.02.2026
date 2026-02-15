using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public class AdminStateServiceNew : IAdminStateService
{
    private readonly Dictionary<long, AdminAddProductState> _productStates = new();

    public void StartProductCreation(long adminId)
    {
        _productStates[adminId] = new AdminAddProductState { AdminId = adminId };
        Console.WriteLine($"🔄 Started product creation for admin {adminId}");
    }

    public void SaveProductName(long adminId, string name)
    {
        if (_productStates.ContainsKey(adminId))
        {
            _productStates[adminId].ProductName = name;
            Console.WriteLine($"💾 Saved product name: {name}");
        }
    }

    public void SaveProductPrice(long adminId, decimal price)
    {
        if (_productStates.ContainsKey(adminId))
        {
            _productStates[adminId].Price = price;
            Console.WriteLine($"💾 Saved product price: {price}");
        }
    }

    public void SaveProductDescription(long adminId, string description)
    {
        if (_productStates.ContainsKey(adminId))
        {
            _productStates[adminId].Description = description;
            Console.WriteLine($"💾 Saved product description");
        }
    }

    public void SaveProductStock(long adminId, int stock)
    {
        if (_productStates.ContainsKey(adminId))
        {
            _productStates[adminId].StockQuantity = stock;
            Console.WriteLine($"💾 Saved product stock: {stock}");
        }
    }

    public void SaveProductCategory(long adminId, string category)
    {
        if (_productStates.ContainsKey(adminId))
        {
            _productStates[adminId].Category = category;
            Console.WriteLine($"💾 Saved product category: {category}");
        }
    }

    public void SaveProductCity(long adminId, string city)
    {
        if (_productStates.ContainsKey(adminId))
        {
            _productStates[adminId].City = city;
            Console.WriteLine($"💾 Saved product city: {city}");
        }
    }

    public void SaveProductImage(long adminId, string imageUrl)
    {
        if (_productStates.ContainsKey(adminId))
        {
            _productStates[adminId].ImageUrl = imageUrl;
            Console.WriteLine($"💾 Saved product image URL");
        }
    }

    public AdminAddProductState? GetProductState(long adminId)
    {
        var exists = _productStates.ContainsKey(adminId);
        Console.WriteLine($"📋 Get product state for admin {adminId}: {(exists ? "exists" : "not found")}");
        return exists ? _productStates[adminId] : null;
    }

    public void ClearProductState(long adminId)
    {
        if (_productStates.ContainsKey(adminId))
        {
            _productStates.Remove(adminId);
            Console.WriteLine($"🗑️ Cleared product state for admin {adminId}");
        }
    }
    public void SetEditingProductId(long adminId, int productId)
    {
        if (!_productStates.ContainsKey(adminId))
        {
            _productStates[adminId] = new AdminAddProductState { AdminId = adminId };
        }
        _productStates[adminId].EditingProductId = productId;
        Console.WriteLine($"💾 Set editing product ID: {productId} for admin {adminId}");
    }

    public int? GetEditingProductId(long adminId)
    {
        return _productStates.ContainsKey(adminId) ? _productStates[adminId].EditingProductId : null;
    }

    public void ClearEditingState(long adminId)
    {
        if (_productStates.ContainsKey(adminId))
        {
            _productStates[adminId].EditingProductId = null;
            Console.WriteLine($"🗑️ Cleared editing state for admin {adminId}");
        }
    }

    public Product CreateProductFromState(long adminId)
    {
        var state = GetProductState(adminId);
        if (state == null) throw new InvalidOperationException("Product state not found");

        var product = new Product
        {
            Name = state.ProductName ?? throw new InvalidOperationException("Product name is required"),
            Price = state.Price ?? throw new InvalidOperationException("Product price is required"),
            Description = state.Description ?? throw new InvalidOperationException("Product description is required"),
            StockQuantity = state.StockQuantity ?? throw new InvalidOperationException("Product stock is required"),
            Category = state.Category ?? throw new InvalidOperationException("Product category is required"),
            City = state.City ?? throw new InvalidOperationException("Product city is required"),
            ImageUrl = state.ImageUrl ?? "https://via.placeholder.com/300",
            IsActive = true
        };

        Console.WriteLine($"✅ Created product: {product.Name} for city {product.City}");
        return product;
    }
}