using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Anbar.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseWarehouseCostTaxDiscountUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Suppliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "Suppliers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DebtAmountLocal",
                table: "Suppliers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DebtAmountOriginal",
                table: "Suppliers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DebtAmountOriginalCurrency",
                table: "Suppliers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DefaultCurrency",
                table: "Suppliers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OriginType",
                table: "Suppliers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "SupplierPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "SupplierPayments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LocalAmount",
                table: "SupplierPayments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "SupplierPayments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "SupplierPayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "StockMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "StockMovements",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "StockMovements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "StockMovements",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "StockBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountUnitShare",
                table: "StockBatches",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "StockBatches",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpenseUnitShare",
                table: "StockBatches",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "StockBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FifoDate",
                table: "StockBatches",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "FinalTotalCost",
                table: "StockBatches",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalUnitCost",
                table: "StockBatches",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "StockBatches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsImportBatch",
                table: "StockBatches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LocalUnitPrice",
                table: "StockBatches",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "StockBatches",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProductionDate",
                table: "StockBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseUnitPrice",
                table: "StockBatches",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxUnitShare",
                table: "StockBatches",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinStockQuantity",
                table: "Products",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageCostPrice",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsExciseApplicable",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsImportTaxExempt",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPurchasePriceVatIncluded",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVatApplicable",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVatRecoverable",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LastCostPrice",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "Products",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Volume",
                table: "Products",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "Products",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Invoices",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostExcludedTaxAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CostIncludedExpenseAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CostIncludedTaxAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CostStatus",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "Invoices",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalCostAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossItemsAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsImport",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LocalItemsTotalAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LocalTotalAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetItemsAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalDebtAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalItemsTotalAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPaidAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalTotalAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "RecoverableTaxAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "InvoiceItems",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "InvoiceItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "InvoiceItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountUnitShare",
                table: "InvoiceItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "InvoiceItems",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpenseUnitShare",
                table: "InvoiceItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalTotalCost",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalUnitCost",
                table: "InvoiceItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossAmount",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsVatApplicable",
                table: "InvoiceItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVatIncludedInCost",
                table: "InvoiceItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVatIncludedInPrice",
                table: "InvoiceItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LocalTotalAmount",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LocalUnitPrice",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetAmount",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalTotalAmount",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProductBarcode",
                table: "InvoiceItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductCodeSnapshot",
                table: "InvoiceItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductNameSnapshot",
                table: "InvoiceItems",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxUnitShare",
                table: "InvoiceItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "AllocationMethod",
                table: "InvoiceExpenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "InvoiceExpenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "InvoiceExpenses",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeZeroAmountInCost",
                table: "InvoiceExpenses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsImportExpense",
                table: "InvoiceExpenses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTaxRelated",
                table: "InvoiceExpenses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LocalAmount",
                table: "InvoiceExpenses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "InvoiceExpenses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ShouldAllocateToItems",
                table: "InvoiceExpenses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "ExpenseTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DefaultAllocationMethod",
                table: "ExpenseTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeInProductCost",
                table: "ExpenseTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeZeroAmountInCost",
                table: "ExpenseTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustomsExpense",
                table: "ExpenseTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsImportExpense",
                table: "ExpenseTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecoverableTax",
                table: "ExpenseTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "ExpenseTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTaxRelated",
                table: "ExpenseTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowByDefault",
                table: "ExpenseTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ExpenseTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UseForImport",
                table: "ExpenseTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DebtAmountLocal",
                table: "Customers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DebtAmountOriginal",
                table: "Customers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DebtAmountOriginalCurrency",
                table: "Customers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CostSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncludeExpensesInStockCost = table.Column<bool>(type: "bit", nullable: false),
                    DefaultAllocationMethod = table.Column<int>(type: "int", nullable: false),
                    SuggestSalePrice = table.Column<bool>(type: "bit", nullable: false),
                    MinimumMarginPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AutoCalculateCostOnConfirm = table.Column<bool>(type: "bit", nullable: false),
                    RecalculateCostWhenExpenseChanges = table.Column<bool>(type: "bit", nullable: false),
                    ExcludeZeroAmountExpenses = table.Column<bool>(type: "bit", nullable: false),
                    LockCostAfterConfirm = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromCurrency = table.Column<int>(type: "int", nullable: false),
                    ToCurrency = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LocalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    DebtBeforePayment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DebtAfterPayment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPayments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportFieldSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FieldKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ShowOnInvoice = table.Column<bool>(type: "bit", nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportFieldSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnableImportInvoice = table.Column<bool>(type: "bit", nullable: false),
                    AutoOpenImportFieldsForForeignSupplier = table.Column<bool>(type: "bit", nullable: false),
                    RequireDeclarationNumber = table.Column<bool>(type: "bit", nullable: false),
                    RequireExchangeRate = table.Column<bool>(type: "bit", nullable: false),
                    UseInvoiceDateExchangeRate = table.Column<bool>(type: "bit", nullable: false),
                    IncludeCustomsDutyInCost = table.Column<bool>(type: "bit", nullable: false),
                    IncludeBrokerFeeInCost = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInsuranceInCost = table.Column<bool>(type: "bit", nullable: false),
                    IncludeTransportInCost = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceCostSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    BaseItemsAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetItemsAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossItemsAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostIncludedExpenseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostExcludedExpenseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostIncludedTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RecoverableTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostExcludedTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalCostAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceCostSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceCostSummaries_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceExpenseAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceExpenseId = table.Column<int>(type: "int", nullable: false),
                    InvoiceItemId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AllocationMethod = table.Column<int>(type: "int", nullable: false),
                    AllocationBaseValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitAllocatedAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceExpenseAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceExpenseAllocations_InvoiceExpenses_InvoiceExpenseId",
                        column: x => x.InvoiceExpenseId,
                        principalTable: "InvoiceExpenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceExpenseAllocations_InvoiceItems_InvoiceItemId",
                        column: x => x.InvoiceItemId,
                        principalTable: "InvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceExpenseAllocations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceImportInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    DeclarationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeclarationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImportDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OriginCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImportCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomsPoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ForeignSupplierName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ForeignSupplierTaxNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ForeignInvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ForeignInvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransportDocumentNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContainerNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HsCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Incoterm = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CurrencyRateSource = table.Column<int>(type: "int", nullable: false),
                    CustomsDeclaredAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceImportInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceImportInfos_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoicePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LocalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoicePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoicePayments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoicePrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LockConfirmedInvoice = table.Column<bool>(type: "bit", nullable: false),
                    RequireReturnReason = table.Column<bool>(type: "bit", nullable: false),
                    RequireShelfSelection = table.Column<bool>(type: "bit", nullable: false),
                    RequireBatchSelectionForReturn = table.Column<bool>(type: "bit", nullable: false),
                    CopyProductBarcodeToInvoiceItem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceTaxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    TaxType = table.Column<int>(type: "int", nullable: false),
                    TaxName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CalculationSource = table.Column<int>(type: "int", nullable: false),
                    CostTreatment = table.Column<int>(type: "int", nullable: false),
                    RatePercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxBaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsIncludedInCost = table.Column<bool>(type: "bit", nullable: false),
                    IsRecoverable = table.Column<bool>(type: "bit", nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    LocalTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShouldAllocateToItems = table.Column<bool>(type: "bit", nullable: false),
                    AllocationMethod = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceTaxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceTaxes_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalPurchaseSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalPurchaseSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnableFIFO = table.Column<bool>(type: "bit", nullable: false),
                    PreventNegativeStock = table.Column<bool>(type: "bit", nullable: false),
                    CheckShelfCapacity = table.Column<bool>(type: "bit", nullable: false),
                    BlockPassiveProductInInvoice = table.Column<bool>(type: "bit", nullable: false),
                    AutoCreateBatchOnStockIn = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierBalanceTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
                    SupplierPaymentId = table.Column<int>(type: "int", nullable: true),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LocalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DebtBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DebtAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierBalanceTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierBalanceTransactions_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupplierBalanceTransactions_SupplierPayments_SupplierPaymentId",
                        column: x => x.SupplierPaymentId,
                        principalTable: "SupplierPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupplierBalanceTransactions_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Taxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaxType = table.Column<int>(type: "int", nullable: false),
                    DefaultRatePercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsRecoverableByDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsIncludedInCostByDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsIncludedInPriceByDefault = table.Column<bool>(type: "bit", nullable: false),
                    DefaultCalculationSource = table.Column<int>(type: "int", nullable: false),
                    DefaultCostTreatment = table.Column<int>(type: "int", nullable: false),
                    UseForLocalPurchase = table.Column<bool>(type: "bit", nullable: false),
                    UseForImportPurchase = table.Column<bool>(type: "bit", nullable: false),
                    UseForSale = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Taxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxRegime = table.Column<int>(type: "int", nullable: false),
                    EnableVAT = table.Column<bool>(type: "bit", nullable: false),
                    VATPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PurchasePricesIncludeVATByDefault = table.Column<bool>(type: "bit", nullable: false),
                    VATRecoverableByDefault = table.Column<bool>(type: "bit", nullable: false),
                    EnableProfitTax = table.Column<bool>(type: "bit", nullable: false),
                    ProfitTaxPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EnableSimplifiedTax = table.Column<bool>(type: "bit", nullable: false),
                    SimplifiedTaxPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IncludeImportVATInCost = table.Column<bool>(type: "bit", nullable: false),
                    IncludeCustomsDutyInCost = table.Column<bool>(type: "bit", nullable: false),
                    IncludeExciseInCost = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanyVoen = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompanyPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompanyAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DefaultCurrency = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerBalanceTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LocalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DebtBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DebtAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerPaymentId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerBalanceTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerBalanceTransactions_CustomerPayments_CustomerPaymentId",
                        column: x => x.CustomerPaymentId,
                        principalTable: "CustomerPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerBalanceTransactions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerBalanceTransactions_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceDynamicFieldValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    FieldKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InvoiceImportInfoId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceDynamicFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceDynamicFieldValues_InvoiceImportInfos_InvoiceImportInfoId",
                        column: x => x.InvoiceImportInfoId,
                        principalTable: "InvoiceImportInfos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvoiceDynamicFieldValues_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalPurchaseSettingValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocalPurchaseSettingId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ValueType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalPurchaseSettingValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalPurchaseSettingValues_LocalPurchaseSettings_LocalPurchaseSettingId",
                        column: x => x.LocalPurchaseSettingId,
                        principalTable: "LocalPurchaseSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItemTaxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceItemId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    TaxId = table.Column<int>(type: "int", nullable: true),
                    TaxType = table.Column<int>(type: "int", nullable: false),
                    TaxName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CalculationSource = table.Column<int>(type: "int", nullable: false),
                    CostTreatment = table.Column<int>(type: "int", nullable: false),
                    RatePercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxBaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    LocalTaxBaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LocalTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsRecoverable = table.Column<bool>(type: "bit", nullable: false),
                    IsIncludedInCost = table.Column<bool>(type: "bit", nullable: false),
                    IsIncludedInPrice = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItemTaxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItemTaxes_InvoiceItems_InvoiceItemId",
                        column: x => x.InvoiceItemId,
                        principalTable: "InvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceItemTaxes_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceItemTaxes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceItemTaxes_Taxes_TaxId",
                        column: x => x.TaxId,
                        principalTable: "Taxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductTaxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    TaxId = table.Column<int>(type: "int", nullable: false),
                    TaxType = table.Column<int>(type: "int", nullable: false),
                    RatePercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsApplicable = table.Column<bool>(type: "bit", nullable: false),
                    IsRecoverable = table.Column<bool>(type: "bit", nullable: false),
                    IsIncludedInCost = table.Column<bool>(type: "bit", nullable: false),
                    IsIncludedInPrice = table.Column<bool>(type: "bit", nullable: false),
                    CostTreatment = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTaxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductTaxes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductTaxes_Taxes_TaxId",
                        column: x => x.TaxId,
                        principalTable: "Taxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceTaxAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceTaxId = table.Column<int>(type: "int", nullable: false),
                    InvoiceItemId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitAllocatedAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InvoiceItemTaxId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceTaxAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceTaxAllocations_InvoiceItemTaxes_InvoiceItemTaxId",
                        column: x => x.InvoiceItemTaxId,
                        principalTable: "InvoiceItemTaxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceTaxAllocations_InvoiceItems_InvoiceItemId",
                        column: x => x.InvoiceItemId,
                        principalTable: "InvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceTaxAllocations_InvoiceTaxes_InvoiceTaxId",
                        column: x => x.InvoiceTaxId,
                        principalTable: "InvoiceTaxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceTaxAllocations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CostSettings",
                columns: new[] { "Id", "AutoCalculateCostOnConfirm", "CreatedAt", "DefaultAllocationMethod", "ExcludeZeroAmountExpenses", "IncludeExpensesInStockCost", "IsActive", "LockCostAfterConfirm", "MinimumMarginPercent", "RecalculateCostWhenExpenseChanges", "SuggestSalePrice", "UpdatedAt" },
                values: new object[] { 1, true, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7125), 2, true, true, true, true, 0m, true, true, null });

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
                columns: new[] { "Code", "CreatedAt", "DefaultAllocationMethod", "IncludeInProductCost", "IncludeZeroAmountInCost", "IsCustomsExpense", "IsImportExpense", "IsRecoverableTax", "IsRequired", "IsTaxRelated", "ShowByDefault", "SortOrder", "UseForImport" },
                values: new object[] { "DASIMA", new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6710), 2, false, false, false, false, false, false, false, true, 0, false });

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Code", "CreatedAt", "DefaultAllocationMethod", "IncludeInProductCost", "IncludeZeroAmountInCost", "IsCustomsExpense", "IsImportExpense", "IsRecoverableTax", "IsRequired", "IsTaxRelated", "ShowByDefault", "SortOrder", "UseForImport" },
                values: new object[] { "FEHLE", new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6733), 2, false, false, false, false, false, false, false, true, 0, false });

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Code", "CreatedAt", "DefaultAllocationMethod", "IncludeInProductCost", "IncludeZeroAmountInCost", "IsCustomsExpense", "IsImportExpense", "IsRecoverableTax", "IsRequired", "IsTaxRelated", "ShowByDefault", "SortOrder", "UseForImport" },
                values: new object[] { "ENDIRIM", new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6736), 2, false, false, false, false, false, false, false, true, 0, false });

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Code", "CreatedAt", "DefaultAllocationMethod", "IncludeInProductCost", "IncludeZeroAmountInCost", "IsCustomsExpense", "IsImportExpense", "IsRecoverableTax", "IsRequired", "IsTaxRelated", "ShowByDefault", "SortOrder", "UseForImport" },
                values: new object[] { "DIGER", new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6737), 2, false, false, false, false, false, false, false, true, 0, false });

            migrationBuilder.InsertData(
                table: "ImportFieldSettings",
                columns: new[] { "Id", "CreatedAt", "DefaultValue", "DisplayName", "FieldKey", "FieldType", "IsActive", "IsRequired", "IsVisible", "OptionsJson", "Placeholder", "ShowOnInvoice", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7186), null, "Gömrük bəyannamə №", "DeclarationNumber", 1, true, false, true, null, null, true, 1, null },
                    { 2, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7191), null, "İdxal tarixi", "ImportDate", 1, true, false, true, null, null, true, 2, null },
                    { 3, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7198), null, "Mənşə ölkəsi", "OriginCountry", 1, true, false, true, null, null, true, 3, null },
                    { 4, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7199), null, "Gömrük postu", "CustomsPoint", 1, true, false, true, null, null, true, 4, null },
                    { 5, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7200), null, "Valyuta", "Currency", 1, true, true, true, null, null, true, 5, null },
                    { 6, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7202), null, "Məzənnə", "ExchangeRate", 1, true, true, true, null, null, true, 6, null },
                    { 7, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7203), null, "Xarici qaimə №", "ForeignInvoiceNumber", 1, true, false, true, null, null, true, 7, null },
                    { 8, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7204), null, "Daşıma sənədi №", "TransportDocumentNumber", 1, true, false, true, null, null, true, 8, null }
                });

            migrationBuilder.InsertData(
                table: "ImportSettings",
                columns: new[] { "Id", "AutoOpenImportFieldsForForeignSupplier", "CreatedAt", "EnableImportInvoice", "IncludeBrokerFeeInCost", "IncludeCustomsDutyInCost", "IncludeInsuranceInCost", "IncludeTransportInCost", "IsActive", "RequireDeclarationNumber", "RequireExchangeRate", "UpdatedAt", "UseInvoiceDateExchangeRate" },
                values: new object[] { 1, true, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7167), true, true, true, true, true, true, false, true, null, false });

            migrationBuilder.InsertData(
                table: "InvoiceSettings",
                columns: new[] { "Id", "CopyProductBarcodeToInvoiceItem", "CreatedAt", "InvoicePrefix", "IsActive", "LockConfirmedInvoice", "RequireBatchSelectionForReturn", "RequireReturnReason", "RequireShelfSelection", "UpdatedAt" },
                values: new object[] { 1, true, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7070), "QAI", true, true, true, true, true, null });

            migrationBuilder.InsertData(
                table: "StockSettings",
                columns: new[] { "Id", "AutoCreateBatchOnStockIn", "BlockPassiveProductInInvoice", "CheckShelfCapacity", "CreatedAt", "EnableFIFO", "IsActive", "PreventNegativeStock", "UpdatedAt" },
                values: new object[] { 1, true, true, true, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7094), true, true, true, null });

            migrationBuilder.InsertData(
                table: "TaxSettings",
                columns: new[] { "Id", "CreatedAt", "EnableProfitTax", "EnableSimplifiedTax", "EnableVAT", "IncludeCustomsDutyInCost", "IncludeExciseInCost", "IncludeImportVATInCost", "IsActive", "ProfitTaxPercent", "PurchasePricesIncludeVATByDefault", "SimplifiedTaxPercent", "TaxRegime", "UpdatedAt", "VATPercent", "VATRecoverableByDefault" },
                values: new object[] { 1, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7145), false, false, false, true, true, false, true, 20m, true, 2m, 1, null, 18m, true });

            migrationBuilder.InsertData(
                table: "Taxes",
                columns: new[] { "Id", "Code", "CreatedAt", "DefaultCalculationSource", "DefaultCostTreatment", "DefaultRatePercent", "IsActive", "IsIncludedInCostByDefault", "IsIncludedInPriceByDefault", "IsRecoverableByDefault", "Name", "Note", "TaxType", "UpdatedAt", "UseForImportPurchase", "UseForLocalPurchase", "UseForSale" },
                values: new object[,]
                {
                    { 1, "VAT", new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6976), 1, 3, 18m, true, false, true, true, "ƏDV", null, 1, null, true, true, true },
                    { 2, "CUSTOMS_DUTY", new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6988), 3, 2, 0m, true, true, false, false, "Gömrük rüsumu", null, 3, null, true, false, false },
                    { 3, "EXCISE", new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(6990), 3, 2, 0m, true, true, false, false, "Aksiz", null, 4, null, true, true, false }
                });

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

            migrationBuilder.InsertData(
                table: "WarehouseSettings",
                columns: new[] { "Id", "AppName", "CompanyAddress", "CompanyName", "CompanyPhone", "CompanyVoen", "CreatedAt", "DefaultCurrency", "IsActive", "UpdatedAt" },
                values: new object[] { 1, "Mebel Anbar Sistemi", null, null, null, null, new DateTime(2026, 5, 7, 10, 46, 8, 184, DateTimeKind.Local).AddTicks(7051), 1, true, null });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTypes_Code",
                table: "ExpenseTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyRates_FromCurrency_ToCurrency_RateDate",
                table: "CurrencyRates",
                columns: new[] { "FromCurrency", "ToCurrency", "RateDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBalanceTransactions_CustomerId_TransactionDate",
                table: "CustomerBalanceTransactions",
                columns: new[] { "CustomerId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBalanceTransactions_CustomerPaymentId",
                table: "CustomerBalanceTransactions",
                column: "CustomerPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBalanceTransactions_InvoiceId",
                table: "CustomerBalanceTransactions",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_CustomerId",
                table: "CustomerPayments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportFieldSettings_FieldKey",
                table: "ImportFieldSettings",
                column: "FieldKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceCostSummaries_InvoiceId",
                table: "InvoiceCostSummaries",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDynamicFieldValues_InvoiceId_FieldKey",
                table: "InvoiceDynamicFieldValues",
                columns: new[] { "InvoiceId", "FieldKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDynamicFieldValues_InvoiceImportInfoId",
                table: "InvoiceDynamicFieldValues",
                column: "InvoiceImportInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceExpenseAllocations_InvoiceExpenseId_InvoiceItemId",
                table: "InvoiceExpenseAllocations",
                columns: new[] { "InvoiceExpenseId", "InvoiceItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceExpenseAllocations_InvoiceItemId",
                table: "InvoiceExpenseAllocations",
                column: "InvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceExpenseAllocations_ProductId",
                table: "InvoiceExpenseAllocations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceImportInfos_InvoiceId",
                table: "InvoiceImportInfos",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItemTaxes_InvoiceId",
                table: "InvoiceItemTaxes",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItemTaxes_InvoiceItemId_TaxType",
                table: "InvoiceItemTaxes",
                columns: new[] { "InvoiceItemId", "TaxType" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItemTaxes_ProductId",
                table: "InvoiceItemTaxes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItemTaxes_TaxId",
                table: "InvoiceItemTaxes",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePayments_InvoiceId_PaymentDate",
                table: "InvoicePayments",
                columns: new[] { "InvoiceId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTaxAllocations_InvoiceItemId",
                table: "InvoiceTaxAllocations",
                column: "InvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTaxAllocations_InvoiceItemTaxId",
                table: "InvoiceTaxAllocations",
                column: "InvoiceItemTaxId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTaxAllocations_InvoiceTaxId_InvoiceItemId",
                table: "InvoiceTaxAllocations",
                columns: new[] { "InvoiceTaxId", "InvoiceItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTaxAllocations_ProductId",
                table: "InvoiceTaxAllocations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTaxes_InvoiceId_TaxType",
                table: "InvoiceTaxes",
                columns: new[] { "InvoiceId", "TaxType" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalPurchaseSettings_Code",
                table: "LocalPurchaseSettings",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalPurchaseSettingValues_LocalPurchaseSettingId_Key",
                table: "LocalPurchaseSettingValues",
                columns: new[] { "LocalPurchaseSettingId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductTaxes_ProductId_TaxId",
                table: "ProductTaxes",
                columns: new[] { "ProductId", "TaxId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductTaxes_TaxId",
                table: "ProductTaxes",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBalanceTransactions_InvoiceId",
                table: "SupplierBalanceTransactions",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBalanceTransactions_SupplierId_TransactionDate",
                table: "SupplierBalanceTransactions",
                columns: new[] { "SupplierId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBalanceTransactions_SupplierPaymentId",
                table: "SupplierBalanceTransactions",
                column: "SupplierPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Taxes_Code",
                table: "Taxes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostSettings");

            migrationBuilder.DropTable(
                name: "CurrencyRates");

            migrationBuilder.DropTable(
                name: "CustomerBalanceTransactions");

            migrationBuilder.DropTable(
                name: "ImportFieldSettings");

            migrationBuilder.DropTable(
                name: "ImportSettings");

            migrationBuilder.DropTable(
                name: "InvoiceCostSummaries");

            migrationBuilder.DropTable(
                name: "InvoiceDynamicFieldValues");

            migrationBuilder.DropTable(
                name: "InvoiceExpenseAllocations");

            migrationBuilder.DropTable(
                name: "InvoicePayments");

            migrationBuilder.DropTable(
                name: "InvoiceSettings");

            migrationBuilder.DropTable(
                name: "InvoiceTaxAllocations");

            migrationBuilder.DropTable(
                name: "LocalPurchaseSettingValues");

            migrationBuilder.DropTable(
                name: "ProductTaxes");

            migrationBuilder.DropTable(
                name: "StockSettings");

            migrationBuilder.DropTable(
                name: "SupplierBalanceTransactions");

            migrationBuilder.DropTable(
                name: "TaxSettings");

            migrationBuilder.DropTable(
                name: "WarehouseSettings");

            migrationBuilder.DropTable(
                name: "CustomerPayments");

            migrationBuilder.DropTable(
                name: "InvoiceImportInfos");

            migrationBuilder.DropTable(
                name: "InvoiceItemTaxes");

            migrationBuilder.DropTable(
                name: "InvoiceTaxes");

            migrationBuilder.DropTable(
                name: "LocalPurchaseSettings");

            migrationBuilder.DropTable(
                name: "Taxes");

            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseTypes_Code",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DebtAmountLocal",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DebtAmountOriginal",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DebtAmountOriginalCurrency",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DefaultCurrency",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OriginType",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "LocalAmount",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "DiscountUnitShare",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "ExpenseUnitShare",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "FifoDate",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "FinalTotalCost",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "FinalUnitCost",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "IsImportBatch",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "LocalUnitPrice",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "OriginalUnitPrice",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "ProductionDate",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "PurchaseUnitPrice",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "TaxUnitShare",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "AverageCostPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsExciseApplicable",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsImportTaxExempt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsPurchasePriceVatIncluded",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsVatApplicable",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsVatRecoverable",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LastCostPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Volume",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CostExcludedTaxAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CostIncludedExpenseAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CostIncludedTaxAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CostStatus",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FinalCostAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "GrossAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "GrossItemsAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsImport",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LocalItemsTotalAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LocalTotalAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "NetAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "NetItemsAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OriginalDebtAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OriginalItemsTotalAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OriginalPaidAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OriginalTotalAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RecoverableTaxAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "DiscountUnitShare",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "ExpenseUnitShare",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "FinalTotalCost",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "FinalUnitCost",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "GrossAmount",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "IsVatApplicable",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "IsVatIncludedInCost",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "IsVatIncludedInPrice",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "LocalTotalAmount",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "LocalUnitPrice",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "NetAmount",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "OriginalTotalAmount",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "OriginalUnitPrice",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "ProductBarcode",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "ProductCodeSnapshot",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "ProductNameSnapshot",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "TaxUnitShare",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "AllocationMethod",
                table: "InvoiceExpenses");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "InvoiceExpenses");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "InvoiceExpenses");

            migrationBuilder.DropColumn(
                name: "IncludeZeroAmountInCost",
                table: "InvoiceExpenses");

            migrationBuilder.DropColumn(
                name: "IsImportExpense",
                table: "InvoiceExpenses");

            migrationBuilder.DropColumn(
                name: "IsTaxRelated",
                table: "InvoiceExpenses");

            migrationBuilder.DropColumn(
                name: "LocalAmount",
                table: "InvoiceExpenses");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "InvoiceExpenses");

            migrationBuilder.DropColumn(
                name: "ShouldAllocateToItems",
                table: "InvoiceExpenses");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "DefaultAllocationMethod",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "IncludeInProductCost",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "IncludeZeroAmountInCost",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "IsCustomsExpense",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "IsImportExpense",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "IsRecoverableTax",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "IsTaxRelated",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "ShowByDefault",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "UseForImport",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "DebtAmountLocal",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DebtAmountOriginal",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DebtAmountOriginalCurrency",
                table: "Customers");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinStockQuantity",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3);

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
        }
    }
}
