using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NekoSubscription.Core.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddVisualStyleSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AcrylicOpacity",
                table: "ApplicationSettings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.84999999999999998);

            migrationBuilder.AddColumn<int>(
                name: "VisualStyle",
                table: "ApplicationSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcrylicOpacity",
                table: "ApplicationSettings");

            migrationBuilder.DropColumn(
                name: "VisualStyle",
                table: "ApplicationSettings");
        }
    }
}
