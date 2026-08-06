using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeepApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_CreatedUser_UpdatedUser_Column_Notes_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "NOTES",
                type: "RAW(16)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "NOTES",
                type: "RAW(16)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "NOTES",
                type: "RAW(16)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NOTES_CreatedById",
                table: "NOTES",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_NOTES_DeletedById",
                table: "NOTES",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_NOTES_UpdatedById",
                table: "NOTES",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_NOTES_AspNetUsers_CreatedById",
                table: "NOTES",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTES_AspNetUsers_DeletedById",
                table: "NOTES",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTES_AspNetUsers_UpdatedById",
                table: "NOTES",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NOTES_AspNetUsers_CreatedById",
                table: "NOTES");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTES_AspNetUsers_DeletedById",
                table: "NOTES");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTES_AspNetUsers_UpdatedById",
                table: "NOTES");

            migrationBuilder.DropIndex(
                name: "IX_NOTES_CreatedById",
                table: "NOTES");

            migrationBuilder.DropIndex(
                name: "IX_NOTES_DeletedById",
                table: "NOTES");

            migrationBuilder.DropIndex(
                name: "IX_NOTES_UpdatedById",
                table: "NOTES");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "NOTES");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "NOTES");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "NOTES");
        }
    }
}
