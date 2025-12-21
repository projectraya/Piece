using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Piece.Migrations
{
    /// <inheritdoc />
    public partial class ChangingSubscriptionPlanFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanDownload",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CanSkipAds",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxDevices",
                table: "SubscriptionPlans");

            migrationBuilder.RenameColumn(
                name: "HighQualityAudio",
                table: "SubscriptionPlans",
                newName: "CanUseMap");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CanUseMap",
                table: "SubscriptionPlans",
                newName: "HighQualityAudio");

            migrationBuilder.AddColumn<bool>(
                name: "CanDownload",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanSkipAds",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxDevices",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
