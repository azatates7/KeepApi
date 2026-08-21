using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeepApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_RefreshTokens_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "REMINDER_NOTIFIED_AT",
                table: "NOTES",
                type: "TIMESTAMP(7)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "REFRESH_TOKENS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    UserId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TOKEN_HASH = table.Column<string>(type: "VARCHAR2(128)", nullable: false),
                    EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    REVOKED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    REPLACED_BY_TOKEN_HASH = table.Column<string>(type: "VARCHAR2(128)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    Status = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REFRESH_TOKENS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_REFRESH_TOKENS_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_REFRESH_TOKENS_TOKEN_HASH",
                table: "REFRESH_TOKENS",
                column: "TOKEN_HASH",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_REFRESH_TOKENS_UserId",
                table: "REFRESH_TOKENS",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "REFRESH_TOKENS");

            migrationBuilder.DropColumn(
                name: "REMINDER_NOTIFIED_AT",
                table: "NOTES");
        }
    }
}
