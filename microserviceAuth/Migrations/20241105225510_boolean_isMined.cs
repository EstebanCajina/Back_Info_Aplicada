using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace microserviceAuth.Migrations
{
    /// <inheritdoc />
    public partial class boolean_isMined : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMined",
                table: "Blocks",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMined",
                table: "Blocks");
        }
    }
}
