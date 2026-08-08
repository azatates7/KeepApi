using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeepApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_ImageUrl_Column_Notes_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Checklist",
                table: "NOTES",
                type: "NUMBER(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ImageAdded",
                table: "NOTES",
                type: "NUMBER(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "NOTES",
                type: "NVARCHAR2(2000)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Checklist",
                table: "NOTES");

            migrationBuilder.DropColumn(
                name: "ImageAdded",
                table: "NOTES");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "NOTES");
        }
    }
}
