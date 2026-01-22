using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Piece.Migrations
{
    /// <inheritdoc />
    public partial class AddPopularityProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Popularity",
                table: "Artists",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Popularity",
                table: "Artists");
        }
    }
}
