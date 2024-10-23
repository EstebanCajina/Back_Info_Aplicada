using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace microserviceAuth.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
        name: "mempools");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
