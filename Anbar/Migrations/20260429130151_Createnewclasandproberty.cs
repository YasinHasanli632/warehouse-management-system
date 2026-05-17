using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anbar.Migrations
{
    /// <inheritdoc />
    public partial class Createnewclasandproberty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_Products_ProductId",
                table: "InvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_Shelves_ShelfId",
                table: "InvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductAttributes_AttributeDefinitions_AttributeDefinitionId",
                table: "ProductAttributes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductAttributes_AttributeValues_AttributeValueId1",
                table: "ProductAttributes");

            migrationBuilder.DropForeignKey(
                name: "FK_ShelfStocks_Products_ProductId",
                table: "ShelfStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_ShelfStocks_Shelves_ShelfId",
                table: "ShelfStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Shelves_Warehouses_WarehouseId",
                table: "Shelves");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Products_ProductId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_ShelfStocks_ProductId",
                table: "ShelfStocks");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_AttributeValueId1",
                table: "ProductAttributes");

            migrationBuilder.DropColumn(
                name: "AttributeValueId1",
                table: "ProductAttributes");

            migrationBuilder.CreateTable(
                name: "ShelfAttributeDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsLimit = table.Column<bool>(type: "bit", nullable: false),
                    IsNumeric = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShelfAttributeDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShelfAttributeValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShelfId = table.Column<int>(type: "int", nullable: false),
                    ShelfAttributeDefinitionId = table.Column<int>(type: "int", nullable: false),
                    NumericValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TextValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShelfAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShelfAttributeValues_ShelfAttributeDefinitions_ShelfAttributeDefinitionId",
                        column: x => x.ShelfAttributeDefinitionId,
                        principalTable: "ShelfAttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShelfAttributeValues_Shelves_ShelfId",
                        column: x => x.ShelfId,
                        principalTable: "Shelves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShelfStocks_ProductId_ShelfId",
                table: "ShelfStocks",
                columns: new[] { "ProductId", "ShelfId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShelfAttributeDefinitions_Key",
                table: "ShelfAttributeDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShelfAttributeValues_ShelfAttributeDefinitionId",
                table: "ShelfAttributeValues",
                column: "ShelfAttributeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfAttributeValues_ShelfId_ShelfAttributeDefinitionId",
                table: "ShelfAttributeValues",
                columns: new[] { "ShelfId", "ShelfAttributeDefinitionId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_Products_ProductId",
                table: "InvoiceItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_Shelves_ShelfId",
                table: "InvoiceItems",
                column: "ShelfId",
                principalTable: "Shelves",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductAttributes_AttributeDefinitions_AttributeDefinitionId",
                table: "ProductAttributes",
                column: "AttributeDefinitionId",
                principalTable: "AttributeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfStocks_Products_ProductId",
                table: "ShelfStocks",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfStocks_Shelves_ShelfId",
                table: "ShelfStocks",
                column: "ShelfId",
                principalTable: "Shelves",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Shelves_Warehouses_WarehouseId",
                table: "Shelves",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Products_ProductId",
                table: "StockMovements",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_Products_ProductId",
                table: "InvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_Shelves_ShelfId",
                table: "InvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductAttributes_AttributeDefinitions_AttributeDefinitionId",
                table: "ProductAttributes");

            migrationBuilder.DropForeignKey(
                name: "FK_ShelfStocks_Products_ProductId",
                table: "ShelfStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_ShelfStocks_Shelves_ShelfId",
                table: "ShelfStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Shelves_Warehouses_WarehouseId",
                table: "Shelves");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Products_ProductId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "ShelfAttributeValues");

            migrationBuilder.DropTable(
                name: "ShelfAttributeDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ShelfStocks_ProductId_ShelfId",
                table: "ShelfStocks");

            migrationBuilder.AddColumn<int>(
                name: "AttributeValueId1",
                table: "ProductAttributes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShelfStocks_ProductId",
                table: "ShelfStocks",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_AttributeValueId1",
                table: "ProductAttributes",
                column: "AttributeValueId1");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_Products_ProductId",
                table: "InvoiceItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_Shelves_ShelfId",
                table: "InvoiceItems",
                column: "ShelfId",
                principalTable: "Shelves",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductAttributes_AttributeDefinitions_AttributeDefinitionId",
                table: "ProductAttributes",
                column: "AttributeDefinitionId",
                principalTable: "AttributeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductAttributes_AttributeValues_AttributeValueId1",
                table: "ProductAttributes",
                column: "AttributeValueId1",
                principalTable: "AttributeValues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfStocks_Products_ProductId",
                table: "ShelfStocks",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfStocks_Shelves_ShelfId",
                table: "ShelfStocks",
                column: "ShelfId",
                principalTable: "Shelves",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Shelves_Warehouses_WarehouseId",
                table: "Shelves",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Products_ProductId",
                table: "StockMovements",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
