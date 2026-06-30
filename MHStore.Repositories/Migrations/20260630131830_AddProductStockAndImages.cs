using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MHStore.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddProductStockAndImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_category_id",
                table: "Products");

            migrationBuilder.AddColumn<int>(
                name: "stock",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "stock_released",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE "Products"
                SET stock = CASE WHEN is_available THEN 100 ELSE 0 END;

                UPDATE "Orders"
                SET stock_released = TRUE;
                """);

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_product_id",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_product_id",
                table: "ProductImages",
                column: "product_id");

            migrationBuilder.Sql("""
                INSERT INTO "ProductImages" (id, product_id, image_url, sort_order)
                SELECT (
                    SUBSTRING(MD5(id::text || image_url), 1, 8) || '-' ||
                    SUBSTRING(MD5(id::text || image_url), 9, 4) || '-' ||
                    SUBSTRING(MD5(id::text || image_url), 13, 4) || '-' ||
                    SUBSTRING(MD5(id::text || image_url), 17, 4) || '-' ||
                    SUBSTRING(MD5(id::text || image_url), 21, 12)
                )::uuid, id, image_url, 0
                FROM "Products"
                WHERE image_url IS NOT NULL AND TRIM(image_url) <> '';
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_category_id",
                table: "Products",
                column: "category_id",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_category_id",
                table: "Products");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropColumn(
                name: "stock",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "stock_released",
                table: "Orders");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_category_id",
                table: "Products",
                column: "category_id",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
