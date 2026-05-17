using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Anbar.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitEntityToCategoryAndProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultUnitId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                });

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

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "CreatedAt", "IsActive", "IsDefault", "Key", "Name", "SortOrder", "Symbol", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7965), true, true, "eded", "Ədəd", 1, "əd", null },
                    { 2, new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7969), true, false, "kg", "Kiloqram", 2, "kq", null },
                    { 3, new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7971), true, false, "qram", "Qram", 3, "qr", null },
                    { 4, new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7973), true, false, "litr", "Litr", 4, "l", null },
                    { 5, new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7975), true, false, "metr", "Metr", 5, "m", null },
                    { 6, new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7976), true, false, "m2", "Kvadrat metr", 6, "m²", null },
                    { 7, new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7978), true, false, "m3", "Kub metr", 7, "m³", null },
                    { 8, new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7980), true, false, "qutu", "Qutu", 8, "qutu", null },
                    { 9, new DateTime(2026, 5, 1, 12, 6, 54, 972, DateTimeKind.Local).AddTicks(7982), true, false, "paket", "Paket", 9, "pkt", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_UnitId",
                table: "Products",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_DefaultUnitId",
                table: "Categories",
                column: "DefaultUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_Key",
                table: "Units",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_Name",
                table: "Units",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Units_DefaultUnitId",
                table: "Categories",
                column: "DefaultUnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Units_UnitId",
                table: "Products",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Units_DefaultUnitId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Units_UnitId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Products_UnitId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_DefaultUnitId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DefaultUnitId",
                table: "Categories");

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4973));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4980));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4983));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4985));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4987));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4989));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4991));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4993));

            migrationBuilder.UpdateData(
                table: "ExpenseTypeFieldDefinitions",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4995));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4641));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4670));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4674));

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4677));
        }
    }
}
