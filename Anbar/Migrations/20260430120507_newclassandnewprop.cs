using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Anbar.Migrations
{
    /// <inheritdoc />
    public partial class newclassandnewprop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraExpenseAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ItemsTotalAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ExpenseTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DefaultDirection = table.Column<int>(type: "int", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    UseForStockIn = table.Column<bool>(type: "bit", nullable: false),
                    UseForStockOut = table.Column<bool>(type: "bit", nullable: false),
                    AffectStockCost = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseTypeFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseTypeId = table.Column<int>(type: "int", nullable: false),
                    FieldKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseTypeFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseTypeFieldDefinitions_ExpenseTypes_ExpenseTypeId",
                        column: x => x.ExpenseTypeId,
                        principalTable: "ExpenseTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    ExpenseTypeId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    AffectStockCost = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceExpenses_ExpenseTypes_ExpenseTypeId",
                        column: x => x.ExpenseTypeId,
                        principalTable: "ExpenseTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceExpenses_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceExpenseFieldValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceExpenseId = table.Column<int>(type: "int", nullable: false),
                    ExpenseTypeFieldDefinitionId = table.Column<int>(type: "int", nullable: true),
                    FieldKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsCustom = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceExpenseFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceExpenseFieldValues_ExpenseTypeFieldDefinitions_ExpenseTypeFieldDefinitionId",
                        column: x => x.ExpenseTypeFieldDefinitionId,
                        principalTable: "ExpenseTypeFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceExpenseFieldValues_InvoiceExpenses_InvoiceExpenseId",
                        column: x => x.InvoiceExpenseId,
                        principalTable: "InvoiceExpenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ExpenseTypes",
                columns: new[] { "Id", "AffectStockCost", "CreatedAt", "DefaultDirection", "IsActive", "IsSystem", "Name", "UpdatedAt", "UseForStockIn", "UseForStockOut" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4641), 1, true, true, "Daşınma", null, true, true },
                    { 2, true, new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4670), 1, true, true, "Fəhlə pulu", null, true, true },
                    { 3, false, new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4674), 2, true, true, "Endirim", null, true, true },
                    { 4, false, new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4677), 1, true, true, "Digər xərc", null, true, true }
                });

            migrationBuilder.InsertData(
                table: "ExpenseTypeFieldDefinitions",
                columns: new[] { "Id", "CreatedAt", "ExpenseTypeId", "FieldKey", "FieldType", "IsActive", "IsRequired", "Label", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4973), 1, "DriverName", "Text", true, false, "Sürücü adı", 1, null },
                    { 2, new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4980), 1, "DriverPhone", "Text", true, false, "Telefon", 2, null },
                    { 3, new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4983), 1, "VehicleName", "Text", true, false, "Maşın", 3, null },
                    { 4, new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4985), 1, "VehicleNumber", "Text", true, false, "Dövlət nömrəsi", 4, null },
                    { 5, new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4987), 1, "Distance", "Text", true, false, "Məsafə", 5, null },
                    { 6, new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4989), 2, "WorkerName", "Text", true, false, "Fəhlə adı", 1, null },
                    { 7, new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4991), 2, "WorkerPhone", "Text", true, false, "Telefon", 2, null },
                    { 8, new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4993), 2, "WorkType", "Text", true, false, "İş növü", 3, null },
                    { 9, new DateTime(2026, 4, 30, 16, 5, 5, 895, DateTimeKind.Local).AddTicks(4995), 2, "WorkHour", "Text", true, false, "İş saatı", 4, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTypeFieldDefinitions_ExpenseTypeId_FieldKey",
                table: "ExpenseTypeFieldDefinitions",
                columns: new[] { "ExpenseTypeId", "FieldKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTypes_Name",
                table: "ExpenseTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceExpenseFieldValues_ExpenseTypeFieldDefinitionId",
                table: "InvoiceExpenseFieldValues",
                column: "ExpenseTypeFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceExpenseFieldValues_InvoiceExpenseId_FieldKey",
                table: "InvoiceExpenseFieldValues",
                columns: new[] { "InvoiceExpenseId", "FieldKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceExpenses_ExpenseTypeId",
                table: "InvoiceExpenses",
                column: "ExpenseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceExpenses_InvoiceId",
                table: "InvoiceExpenses",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceExpenseFieldValues");

            migrationBuilder.DropTable(
                name: "ExpenseTypeFieldDefinitions");

            migrationBuilder.DropTable(
                name: "InvoiceExpenses");

            migrationBuilder.DropTable(
                name: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExtraExpenseAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ItemsTotalAmount",
                table: "Invoices");
        }
    }
}
