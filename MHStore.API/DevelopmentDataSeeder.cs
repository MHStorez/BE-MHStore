using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MHStore.Repositories.Data;
using MHStore.Repositories.Entities;
using MHStore.Services.AccountService;
using OrderCustomerInfo = MHStore.Services.OrderService.CustomerInfoResponse;

namespace MHStore.API;

public static class DevelopmentDataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MainCategoryId = Guid.Parse("22222222-2222-2222-2222-222222222221");
    private static readonly Guid SnackCategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProductChaRamId = Guid.Parse("33333333-3333-3333-3333-333333333331");
    private static readonly Guid ProductNemChuaId = Guid.Parse("33333333-3333-3333-3333-333333333332");
    private static readonly Guid ProductCaVienId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PendingOrderId = Guid.Parse("44444444-4444-4444-4444-444444444441");
    private static readonly Guid CompletedOrderId = Guid.Parse("44444444-4444-4444-4444-444444444442");

    public static async Task SeedAsync(AppDbContext context, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var enabled = environment.IsDevelopment() || configuration.GetValue("SampleDataSeed:Enabled", false);

        if (!enabled)
        {
            return;
        }

        await SeedAdminAsync(context, configuration);
        await SeedCategoriesAsync(context);
        await SeedProductsAsync(context);
        await SeedOrdersAsync(context);
        await context.SaveChangesAsync();
    }

    private static async Task SeedAdminAsync(AppDbContext context, IConfiguration configuration)
    {
        var enabled = configuration.GetValue("AdminSeed:Enabled", true);

        if (!enabled)
        {
            return;
        }

        var username = configuration.GetValue("AdminSeed:Username", "admin")?.Trim();
        var password = configuration.GetValue("AdminSeed:Password", "Admin@123");
        var fullName = configuration.GetValue("AdminSeed:FullName", "MHStore Admin")?.Trim();
        var resetPassword = configuration.GetValue("AdminSeed:ResetPassword", true);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var admin = await context.Users.FirstOrDefaultAsync(user => user.Username == username);

        if (admin == null)
        {
            context.Users.Add(new User
            {
                Id = AdminId,
                Username = username,
                PasswordHash = PasswordHasher.Hash(password),
                FullName = string.IsNullOrWhiteSpace(fullName) ? "MHStore Admin" : fullName,
                Role = "Admin",
                CreatedAt = new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc)
            });

            return;
        }

        admin.Role = "Admin";
        admin.FullName = string.IsNullOrWhiteSpace(fullName) ? admin.FullName : fullName;

        if (resetPassword)
        {
            admin.PasswordHash = PasswordHasher.Hash(password);
        }
    }

    private static async Task SeedCategoriesAsync(AppDbContext context)
    {
        var categories = new[]
        {
            new Category { Id = MainCategoryId, Name = "Món chính", Slug = "mon-chinh", Status = "Active" },
            new Category { Id = SnackCategoryId, Name = "Ăn vặt", Slug = "an-vat", Status = "Active" }
        };

        foreach (var category in categories)
        {
            var existingCategory = await context.Categories.FirstOrDefaultAsync(item => item.Id == category.Id);

            if (existingCategory == null)
            {
                context.Categories.Add(category);
                continue;
            }

            existingCategory.Name = category.Name;
            existingCategory.Slug = category.Slug;
            existingCategory.Status = category.Status;
        }
    }

    private static async Task SeedProductsAsync(AppDbContext context)
    {
        var products = new[]
        {
            new Product
            {
                Id = ProductChaRamId,
                Name = "Chả ram tôm đất",
                Description = "Gói đông lạnh, chiên nhanh là giòn.",
                Price = 120000,
                ImageUrl = "https://images.unsplash.com/photo-1604908177522-0403f218842b?auto=format&fit=crop&w=900&q=80",
                CategoryId = MainCategoryId,
                IsAvailable = true
            },
            new Product
            {
                Id = ProductNemChuaId,
                Name = "Nem chua rán",
                Description = "Hộp tiện lợi cho bữa ăn vặt tại nhà.",
                Price = 65000,
                ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=900&q=80",
                CategoryId = SnackCategoryId,
                IsAvailable = true
            },
            new Product
            {
                Id = ProductCaVienId,
                Name = "Cá viên chiên",
                Description = "Đóng gói sẵn, phù hợp chiên hoặc thả lẩu.",
                Price = 45000,
                ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=900&q=80",
                CategoryId = SnackCategoryId,
                IsAvailable = true
            }
        };

        foreach (var product in products)
        {
            var existingProduct = await context.Products.FirstOrDefaultAsync(item => item.Id == product.Id);

            if (existingProduct == null)
            {
                context.Products.Add(product);
                continue;
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.IsAvailable = product.IsAvailable;
        }
    }

    private static async Task SeedOrdersAsync(AppDbContext context)
    {
        await SeedOrderAsync(
            context,
            PendingOrderId,
            "Pending",
            "Pending",
            DateTime.UtcNow.AddHours(-2),
            new OrderCustomerInfo
            {
                Name = "Anh Huy",
                Phone = "0334140131",
                Address = "Quận 1, TP.HCM",
                Note = "Giao sau 18h"
            },
            [
                new SeedOrderItem(ProductChaRamId, "Chả ram tôm đất", 1, 120000),
                new SeedOrderItem(ProductCaVienId, "Cá viên chiên", 2, 45000)
            ]);

        await SeedOrderAsync(
            context,
            CompletedOrderId,
            "Completed",
            "Paid",
            DateTime.UtcNow.AddDays(-1).AddHours(3),
            new OrderCustomerInfo
            {
                Name = "Chị Minh",
                Phone = "0334140131",
                Address = "Quận Bình Thạnh, TP.HCM",
                Note = "Đã thanh toán SePay"
            },
            [
                new SeedOrderItem(ProductNemChuaId, "Nem chua rán", 2, 65000),
                new SeedOrderItem(ProductChaRamId, "Chả ram tôm đất", 1, 120000)
            ],
            new PaymentLog
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555551"),
                OrderId = CompletedOrderId,
                TransactionId = "SEED-SEPAY-0001",
                Amount = 250000,
                Status = "Paid",
                RawData = "Seed payment log",
                CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(3)
            });
    }

    private static async Task SeedOrderAsync(
        AppDbContext context,
        Guid orderId,
        string status,
        string paymentStatus,
        DateTime createdAt,
        OrderCustomerInfo customer,
        IReadOnlyCollection<SeedOrderItem> items,
        PaymentLog? paymentLog = null)
    {
        if (await context.Orders.AnyAsync(order => order.Id == orderId))
        {
            return;
        }

        var order = new Order
        {
            Id = orderId,
            CustomerInfo = JsonSerializer.Serialize(customer, JsonOptions),
            Status = status,
            PaymentStatus = paymentStatus,
            CreatedAt = createdAt,
            TotalPrice = items.Sum(item => item.UnitPrice * item.Quantity),
            Items = items.Select(item => new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

        context.Orders.Add(order);

        if (paymentLog != null)
        {
            context.PaymentLogs.Add(paymentLog);
        }
    }

    private sealed record SeedOrderItem(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
}
