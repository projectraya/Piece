using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Piece.Migrations
{
    /// <inheritdoc />
    public partial class AddISO2Code : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ISO2Code",
                table: "Countries",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ISO2Code",
                table: "Countries");
        }
    }
}
