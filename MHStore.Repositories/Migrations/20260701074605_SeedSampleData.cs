using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MHStore.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class SeedSampleData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "Slug", "Status" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222221"), "Món chính", "mon-chinh", "Active" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Ăn vặt", "an-vat", "Active" }
                });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "id", "address_note", "address_reference_id", "created_at", "customer_info", "delivery_address", "latitude", "longitude", "order_channel", "order_code", "order_status", "payment_method", "payment_status", "receiver_name", "receiver_phone", "stock_released", "total_price" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444441"), "Giao sau 18h", "", new DateTime(2026, 6, 30, 10, 0, 0, 0, DateTimeKind.Utc), "{\"name\":\"Anh Huy\",\"phone\":\"0334140131\",\"address\":\"Quận 1, TP.HCM\",\"latitude\":null,\"longitude\":null,\"note\":\"Giao sau 18h\",\"addressReferenceId\":\"\"}", "Quận 1, TP.HCM", null, null, "Website", "MH26063044444444", "PendingConfirmation", "Online", "Pending", "Anh Huy", "0334140131", false, 210000m },
                    { new Guid("44444444-4444-4444-4444-444444444442"), "Đã thanh toán SePay", "", new DateTime(2026, 6, 29, 13, 0, 0, 0, DateTimeKind.Utc), "{\"name\":\"Chị Minh\",\"phone\":\"0334140131\",\"address\":\"Quận Bình Thạnh, TP.HCM\",\"latitude\":null,\"longitude\":null,\"note\":\"Đã thanh toán SePay\",\"addressReferenceId\":\"\"}", "Quận Bình Thạnh, TP.HCM", null, null, "Website", "MH26062944444444", "Completed", "Online", "Paid", "Chị Minh", "0334140131", false, 250000m }
                });

            migrationBuilder.InsertData(
                table: "OrderItems",
                columns: new[] { "Id", "OrderId", "ProductId", "ProductName", "Quantity", "UnitPrice" },
                values: new object[,]
                {
                    { new Guid("77777777-7777-7777-7777-777777777771"), new Guid("44444444-4444-4444-4444-444444444441"), new Guid("33333333-3333-3333-3333-333333333331"), "Chả ram tôm đất", 1, 120000m },
                    { new Guid("77777777-7777-7777-7777-777777777772"), new Guid("44444444-4444-4444-4444-444444444441"), new Guid("33333333-3333-3333-3333-333333333333"), "Cá viên chiên", 2, 45000m },
                    { new Guid("77777777-7777-7777-7777-777777777773"), new Guid("44444444-4444-4444-4444-444444444442"), new Guid("33333333-3333-3333-3333-333333333332"), "Nem chua rán", 2, 65000m },
                    { new Guid("77777777-7777-7777-7777-777777777774"), new Guid("44444444-4444-4444-4444-444444444442"), new Guid("33333333-3333-3333-3333-333333333331"), "Chả ram tôm đất", 1, 120000m }
                });

            migrationBuilder.InsertData(
                table: "PaymentLogs",
                columns: new[] { "Id", "Amount", "CreatedAt", "OrderId", "RawData", "Status", "TransactionId" },
                values: new object[] { new Guid("55555555-5555-5555-5555-555555555551"), 250000m, new DateTime(2026, 6, 29, 13, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444442"), "Seed payment log", "Paid", "SEED-SEPAY-0001" });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "id", "category_id", "description", "image_url", "is_available", "name", "price", "stock" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333331"), new Guid("22222222-2222-2222-2222-222222222221"), "Gói đông lạnh, chiên nhanh là giòn.", "https://images.unsplash.com/photo-1604908177522-0403f218842b?auto=format&fit=crop&w=900&q=80", true, "Chả ram tôm đất", 120000m, 30 },
                    { new Guid("33333333-3333-3333-3333-333333333332"), new Guid("22222222-2222-2222-2222-222222222222"), "Hộp tiện lợi cho bữa ăn vặt tại nhà.", "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=900&q=80", true, "Nem chua rán", 65000m, 40 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new Guid("22222222-2222-2222-2222-222222222222"), "Đóng gói sẵn, phù hợp chiên hoặc thả lẩu.", "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=900&q=80", true, "Cá viên chiên", 45000m, 50 }
                });

            migrationBuilder.InsertData(
                table: "ProductImages",
                columns: new[] { "id", "image_url", "product_id", "sort_order" },
                values: new object[,]
                {
                    { new Guid("66666666-6666-6666-6666-666666666661"), "https://images.unsplash.com/photo-1604908177522-0403f218842b?auto=format&fit=crop&w=900&q=80", new Guid("33333333-3333-3333-3333-333333333331"), 0 },
                    { new Guid("66666666-6666-6666-6666-666666666662"), "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=900&q=80", new Guid("33333333-3333-3333-3333-333333333332"), 0 },
                    { new Guid("66666666-6666-6666-6666-666666666663"), "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=900&q=80", new Guid("33333333-3333-3333-3333-333333333333"), 0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777771"));

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777772"));

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777773"));

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777774"));

            migrationBuilder.DeleteData(
                table: "PaymentLogs",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555551"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666661"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666662"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666663"));

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444441"));

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444442"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333331"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333332"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222221"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));
        }
    }
}
