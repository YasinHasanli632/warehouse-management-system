using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anbar.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchAndReturnFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockBatchId",
                table: "StockMovements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentInvoiceId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StockBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ShelfId = table.Column<int>(type: "int", nullable: false),
                    SourceInvoiceId = table.Column<int>(type: "int", nullable: true),
                    SourceInvoiceItemId = table.Column<int>(type: "int", nullable: true),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InitialQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockBatches_InvoiceItems_SourceInvoiceItemId",
                        column: x => x.SourceInvoiceItemId,
                        principalTable: "InvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBatches_Invoices_SourceInvoiceId",
                        column: x => x.SourceInvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBatches_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBatches_Shelves_ShelfId",
                        column: x => x.ShelfId,
                        principalTable: "Shelves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7581));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7584));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7586));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7588));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7589));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7591));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7593));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7594));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7596));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7295));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7322));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7325));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7327));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7525));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7530));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7532));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7534));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7543));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7545));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7547));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7549));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 10, 35, 36, 214, DateTimeKind.Local).AddTicks(7552));

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockBatchId",
                table: "StockMovements",
                column: "StockBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ParentInvoiceId",
                table: "Invoices",
                column: "ParentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_BatchNumber",
                table: "StockBatches",
                column: "BatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_ProductId_ShelfId_EntryDate",
                table: "StockBatches",
                columns: new[] { "ProductId", "ShelfId", "EntryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_ProductId_ShelfId_RemainingQuantity",
                table: "StockBatches",
                columns: new[] { "ProductId", "ShelfId", "RemainingQuantity" });

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_ShelfId",
                table: "StockBatches",
                column: "ShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_SourceInvoiceId",
                table: "StockBatches",
                column: "SourceInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_SourceInvoiceItemId",
                table: "StockBatches",
                column: "SourceInvoiceItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Invoices_ParentInvoiceId",
                table: "Invoices",
                column: "ParentInvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_StockBatches_StockBatchId",
                table: "StockMovements",
                column: "StockBatchId",
                principalTable: "StockBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Invoices_ParentInvoiceId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_StockBatches_StockBatchId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "StockBatches");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_StockBatchId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ParentInvoiceId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "StockBatchId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ParentInvoiceId",
                table: "Invoices");

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(8015));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(8023));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(8025));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(8026));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(8027));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(8029));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(8030));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(8032));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(8034));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7772));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7797));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7800));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7802));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7965));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7969));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7971));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7973));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7975));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7976));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7978));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7980));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7982));
        }
    }
}
