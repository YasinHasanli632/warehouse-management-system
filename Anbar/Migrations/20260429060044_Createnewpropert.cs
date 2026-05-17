using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anbar.Migrations
{
    /// <inheritdoc />
    public partial class Createnewpropert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_ProductId_AttributeValueId",
                table: "ProductAttributes");

            migrationBuilder.AddColumn<int>(
                name: "AttributeDefinitionId",
                table: "ProductAttributes",
                type: "int",
                nullable: false,
                defaultValue: 0);
            migrationBuilder.Sql(@"
    UPDATE pa
    SET pa.AttributeDefinitionId = av.AttributeDefinitionId
    FROM ProductAttributes pa
    INNER JOIN AttributeValues av ON av.Id = pa.AttributeValueId
    WHERE pa.AttributeDefinitionId = 0
");
            migrationBuilder.AddColumn<int>(
                name: "AttributeValueId1",
                table: "ProductAttributes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_AttributeDefinitionId",
                table: "ProductAttributes",
                column: "AttributeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_AttributeValueId1",
                table: "ProductAttributes",
                column: "AttributeValueId1");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_ProductId_AttributeDefinitionId",
                table: "ProductAttributes",
                columns: new[] { "ProductId", "AttributeDefinitionId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductAttributes_AttributeDefinitions_AttributeDefinitionId",
                table: "ProductAttributes",
                column: "AttributeDefinitionId",
                principalTable: "AttributeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductAttributes_AttributeValues_AttributeValueId1",
                table: "ProductAttributes",
                column: "AttributeValueId1",
                principalTable: "AttributeValues",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductAttributes_AttributeDefinitions_AttributeDefinitionId",
                table: "ProductAttributes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductAttributes_AttributeValues_AttributeValueId1",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_AttributeDefinitionId",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_AttributeValueId1",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_ProductId_AttributeDefinitionId",
                table: "ProductAttributes");

            migrationBuilder.DropColumn(
                name: "AttributeDefinitionId",
                table: "ProductAttributes");

            migrationBuilder.DropColumn(
                name: "AttributeValueId1",
                table: "ProductAttributes");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_ProductId_AttributeValueId",
                table: "ProductAttributes",
                columns: new[] { "ProductId", "AttributeValueId" },
                unique: true);
        }
    }
}
