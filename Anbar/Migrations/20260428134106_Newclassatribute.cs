using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anbar.Migrations
{
    /// <inheritdoc />
    public partial class Newclassatribute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_ProductId",
                table: "ProductAttributes");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "ProductAttributes");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "ProductAttributes");

            migrationBuilder.AddColumn<int>(
                name: "AttributeValueId",
                table: "ProductAttributes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AttributeDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeDefinitions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttributeValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttributeDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeValues_AttributeDefinitions_AttributeDefinitionId",
                        column: x => x.AttributeDefinitionId,
                        principalTable: "AttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_AttributeValueId",
                table: "ProductAttributes",
                column: "AttributeValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_ProductId_AttributeValueId",
                table: "ProductAttributes",
                columns: new[] { "ProductId", "AttributeValueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitions_CategoryId_Name",
                table: "AttributeDefinitions",
                columns: new[] { "CategoryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttributeValues_AttributeDefinitionId_Value",
                table: "AttributeValues",
                columns: new[] { "AttributeDefinitionId", "Value" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductAttributes_AttributeValues_AttributeValueId",
                table: "ProductAttributes",
                column: "AttributeValueId",
                principalTable: "AttributeValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductAttributes_AttributeValues_AttributeValueId",
                table: "ProductAttributes");

            migrationBuilder.DropTable(
                name: "AttributeValues");

            migrationBuilder.DropTable(
                name: "AttributeDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_AttributeValueId",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_ProductId_AttributeValueId",
                table: "ProductAttributes");

            migrationBuilder.DropColumn(
                name: "AttributeValueId",
                table: "ProductAttributes");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "ProductAttributes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "ProductAttributes",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_ProductId",
                table: "ProductAttributes",
                column: "ProductId");
        }
    }
}
