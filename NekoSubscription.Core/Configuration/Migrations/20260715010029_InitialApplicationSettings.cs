using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NekoSubscription.Core.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class InitialApplicationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Theme = table.Column<int>(type: "INTEGER", nullable: false),
                    CultureName = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    MinimumLogLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationSettings");
        }
    }
}
