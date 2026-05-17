using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anbar.Migrations
{
    /// <inheritdoc />
    public partial class newpropwerehousecode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductAttributes_AttributeDefinitions_AttributeDefinitionId",
                table: "ProductAttributes");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Warehouses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductAttributes_AttributeDefinitions_AttributeDefinitionId",
                table: "ProductAttributes",
                column: "AttributeDefinitionId",
                principalTable: "AttributeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductAttributes_AttributeDefinitions_AttributeDefinitionId",
                table: "ProductAttributes");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Warehouses");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductAttributes_AttributeDefinitions_AttributeDefinitionId",
                table: "ProductAttributes",
                column: "AttributeDefinitionId",
                principalTable: "AttributeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
