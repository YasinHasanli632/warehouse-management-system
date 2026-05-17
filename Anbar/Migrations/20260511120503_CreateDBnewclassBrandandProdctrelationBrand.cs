using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anbar.Migrations
{
    /// <inheritdoc />
    public partial class CreateDBnewclassBrandandProdctrelationBrand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BrandId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "CostSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(2131));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1909));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1913));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1915));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1917));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1920));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1922));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1924));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1927));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1935));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1359));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1396));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1400));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1404));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(2300));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(2305));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(2308));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(2310));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(2312));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(2314));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(2316));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(2318));

            migrationBuilder.UpdateData(
                table: "ImportSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(2248));

            migrationBuilder.UpdateData(
                table: "InvoiceSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(2038));

            migrationBuilder.UpdateData(
                table: "StockSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(2085));

            migrationBuilder.UpdateData(
                table: "TaxSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(2188));

            migrationBuilder.UpdateData(
                table: "Taxes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1837));

            migrationBuilder.UpdateData(
                table: "Taxes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1858));

            migrationBuilder.UpdateData(
                table: "Taxes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1863));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1752));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1759));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1762));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1765));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1768));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1771));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1773));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1776));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1779));

            migrationBuilder.UpdateData(
                table: "WarehouseSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 11, 16, 5, 0, 284, DateTimeKind.Local).AddTicks(1994));

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId",
                table: "Products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                table: "Brands",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Brands_BrandId",
                table: "Products",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Brands_BrandId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Products_BrandId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "CostSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7125));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7015));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7018));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7020));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7021));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7022));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7023));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7024));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7025));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7027));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6710));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6733));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6736));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6737));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7186));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7191));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7198));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7199));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7200));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7202));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7203));

            migrationBuilder.UpdateData(
                table: "ImportFieldSettings",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7204));

            migrationBuilder.UpdateData(
                table: "ImportSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7167));

            migrationBuilder.UpdateData(
                table: "InvoiceSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7070));

            migrationBuilder.UpdateData(
                table: "StockSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7094));

            migrationBuilder.UpdateData(
                table: "TaxSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7145));

            migrationBuilder.UpdateData(
                table: "Taxes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6976));

            migrationBuilder.UpdateData(
                table: "Taxes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6988));

            migrationBuilder.UpdateData(
                table: "Taxes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6990));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6919));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6923));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6925));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6926));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6928));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6929));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6932));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6933));

            migrationBuilder.UpdateData(
                table: "Units",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6934));

            migrationBuilder.UpdateData(
                table: "WarehouseSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7051));
        }
    }
}
