using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeepApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_AppSettings_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "APP_SETTINGS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    SETTING_KEY = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    SETTING_VALUE = table.Column<string>(type: "NCLOB", nullable: false),
                    IS_ENCRYPTED = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    DESCRIPTION = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    TARGET_PROJECT = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APP_SETTINGS", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_APP_SETTINGS_SETTING_KEY_TARGET_PROJECT",
                table: "APP_SETTINGS",
                columns: new[] { "SETTING_KEY", "TARGET_PROJECT" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "APP_SETTINGS");
        }
    }
}
