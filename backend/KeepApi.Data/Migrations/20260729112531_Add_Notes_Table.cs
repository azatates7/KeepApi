using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeepApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Notes_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NOTES",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(32)", nullable: false),
                    TITLE = table.Column<string>(type: "NVARCHAR2(300)", nullable: true),
                    CONTENT = table.Column<string>(type: "NCLOB", nullable: true),
                    COLOR = table.Column<string>(type: "VARCHAR2(30)", nullable: false, defaultValue: "default"),
                    PINNED = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    ARCHIVED = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    REMINDER_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    STATUS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    IS_DELETED = table.Column<bool>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTES", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NOTES");
        }
    }
}
