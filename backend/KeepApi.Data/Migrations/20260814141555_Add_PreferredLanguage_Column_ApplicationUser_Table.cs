using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeepApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_PreferredLanguage_Column_ApplicationUser_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Status",
                table: "AspNetUsers",
                type: "NUMBER(19)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<long>(
                name: "AccessFailedCount",
                table: "AspNetUsers",
                type: "NUMBER(19)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "AspNetUsers",
                type: "NVARCHAR2(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "tr");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "APP_USER_CLAIMS",
                type: "NUMBER(19)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "APP_ROLE_CLAIMS",
                type: "NUMBER(19)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "AspNetUsers",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "NUMBER(19)");

            migrationBuilder.AlterColumn<int>(
                name: "AccessFailedCount",
                table: "AspNetUsers",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "NUMBER(19)");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "APP_USER_CLAIMS",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "NUMBER(19)")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "APP_ROLE_CLAIMS",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "NUMBER(19)")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");
        }
    }
}
