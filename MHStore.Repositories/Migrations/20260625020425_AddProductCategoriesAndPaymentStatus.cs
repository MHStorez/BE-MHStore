using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MHStore.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategoriesAndPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Products",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "IsAvailable",
                table: "Products",
                newName: "is_available");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Products",
                newName: "image_url");

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_status",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.Sql("""
                UPDATE "Orders"
                SET payment_status = CASE
                    WHEN status = 'Completed' THEN 'Paid'
                    WHEN status = 'PaymentFailed' THEN 'Failed'
                    ELSE 'Pending'
                END;

                UPDATE "Orders"
                SET status = 'Pending'
                WHERE status = 'PaymentFailed';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Categories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.Sql("""
                INSERT INTO "Categories" ("Id", "Name", "Slug", "Status")
                SELECT (
                    SUBSTRING(MD5(source."Name"), 1, 8) || '-' ||
                    SUBSTRING(MD5(source."Name"), 9, 4) || '-' ||
                    SUBSTRING(MD5(source."Name"), 13, 4) || '-' ||
                    SUBSTRING(MD5(source."Name"), 17, 4) || '-' ||
                    SUBSTRING(MD5(source."Name"), 21, 12)
                )::uuid, source."Name", LOWER(REGEXP_REPLACE(source."Name", '[^[:alnum:]]+', '-', 'g')), 'Active'
                FROM (
                    SELECT DISTINCT COALESCE(NULLIF(TRIM("Category"), ''), 'Khác') AS "Name"
                    FROM "Products"
                ) AS source
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Categories" category
                    WHERE LOWER(category."Name") = LOWER(source."Name")
                );

                UPDATE "Products" product
                SET category_id = category."Id"
                FROM "Categories" category
                WHERE LOWER(category."Name") = LOWER(COALESCE(NULLIF(TRIM(product."Category"), ''), 'Khác'));

                INSERT INTO "Categories" ("Id", "Name", "Slug", "Status")
                SELECT (
                    SUBSTRING(MD5('Khác'), 1, 8) || '-' ||
                    SUBSTRING(MD5('Khác'), 9, 4) || '-' ||
                    SUBSTRING(MD5('Khác'), 13, 4) || '-' ||
                    SUBSTRING(MD5('Khác'), 17, 4) || '-' ||
                    SUBSTRING(MD5('Khác'), 21, 12)
                )::uuid, 'Khác', 'khac', 'Active'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Categories" category WHERE LOWER(category."Name") = LOWER('Khác')
                );

                UPDATE "Products"
                SET category_id = (SELECT "Id" FROM "Categories" WHERE LOWER("Name") = LOWER('Khác') LIMIT 1)
                WHERE category_id IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "category_id",
                table: "Products",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_category_id",
                table: "Products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_category_id",
                table: "Products",
                column: "category_id",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_category_id",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_category_id",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "payment_status",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Products",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "is_available",
                table: "Products",
                newName: "IsAvailable");

            migrationBuilder.RenameColumn(
                name: "image_url",
                table: "Products",
                newName: "ImageUrl");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);
        }
    }
}
