using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MHStore.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderWorkflowAndDeliveryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TransactionId",
                table: "PaymentLogs",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PaymentLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "address_note",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "address_reference_id",
                table: "Orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "delivery_address",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                table: "Orders",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                table: "Orders",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "order_channel",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Website");

            migrationBuilder.AddColumn<string>(
                name: "order_code",
                table: "Orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "order_status",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "PendingConfirmation");

            migrationBuilder.AddColumn<string>(
                name: "payment_method",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Online");

            migrationBuilder.AddColumn<string>(
                name: "receiver_name",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "receiver_phone",
                table: "Orders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Orders"
                SET
                    order_code = UPPER(SUBSTRING('MH' || TO_CHAR(created_at, 'YYMMDD') || REPLACE(id::text, '-', ''), 1, 16)),
                    order_status = CASE
                        WHEN status = 'Completed' THEN 'Completed'
                        WHEN status = 'Processing' THEN 'Preparing'
                        WHEN status = 'PaymentFailed' THEN 'PendingConfirmation'
                        ELSE 'PendingConfirmation'
                    END,
                    payment_status = CASE
                        WHEN payment_status = 'Paid' THEN 'Paid'
                        WHEN payment_status = 'Failed' OR status = 'PaymentFailed' THEN 'Failed'
                        WHEN payment_status = 'Unpaid' THEN 'Unpaid'
                        ELSE 'Pending'
                    END,
                    order_channel = 'Website',
                    payment_method = 'Online',
                    receiver_name = COALESCE(NULLIF(customer_info ->> 'name', ''), NULLIF(customer_info ->> 'Name', ''), ''),
                    receiver_phone = COALESCE(NULLIF(customer_info ->> 'phone', ''), NULLIF(customer_info ->> 'Phone', ''), ''),
                    delivery_address = COALESCE(NULLIF(customer_info ->> 'address', ''), NULLIF(customer_info ->> 'Address', ''), ''),
                    address_note = COALESCE(NULLIF(customer_info ->> 'note', ''), NULLIF(customer_info ->> 'Note', ''), ''),
                    address_reference_id = COALESCE(NULLIF(customer_info ->> 'addressReferenceId', ''), NULLIF(customer_info ->> 'AddressReferenceId', ''), '');
                """);

            migrationBuilder.DropColumn(
                name: "status",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLogs_TransactionId",
                table: "PaymentLogs",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_created_at",
                table: "Orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_order_channel",
                table: "Orders",
                column: "order_channel");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_order_code",
                table: "Orders",
                column: "order_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_order_status",
                table: "Orders",
                column: "order_status");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_payment_method",
                table: "Orders",
                column: "payment_method");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_payment_status",
                table: "Orders",
                column: "payment_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.Sql("""
                UPDATE "Orders"
                SET status = CASE
                    WHEN order_status = 'Completed' THEN 'Completed'
                    WHEN order_status IN ('Confirmed', 'Preparing', 'Delivering') THEN 'Processing'
                    ELSE 'Pending'
                END;
                """);

            migrationBuilder.DropIndex(
                name: "IX_PaymentLogs_TransactionId",
                table: "PaymentLogs");

            migrationBuilder.DropIndex(
                name: "IX_Orders_created_at",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_order_channel",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_order_code",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_order_status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_payment_method",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_payment_status",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "address_note",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "address_reference_id",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "delivery_address",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "order_channel",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "order_code",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "order_status",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "receiver_name",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "receiver_phone",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionId",
                table: "PaymentLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PaymentLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }
    }
}
