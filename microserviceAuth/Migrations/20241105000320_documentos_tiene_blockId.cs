using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace microserviceAuth.Migrations
{
    /// <inheritdoc />
    public partial class documentos_tiene_blockId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlockId",
                table: "Documents",
                nullable: true); // Asegúrate de que sea nullable si es opcional

            migrationBuilder.CreateIndex(
                name: "IX_Documents_BlockId",
                table: "Documents",
                column: "BlockId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Blocks_BlockId",
                table: "Documents",
                column: "BlockId",
                principalTable: "Blocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict); // Cambia a Cascade si deseas que elimine los documentos al eliminar un bloque
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Blocks_BlockId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_BlockId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "BlockId",
                table: "Documents");
        }

    }
}
