using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeepApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_JobHistory_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobDefinitions",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    JOB_TYPE_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    JOB_NAME = table.Column<string>(type: "NVARCHAR2(200)", nullable: false),
                    DESCRIPTION = table.Column<string>(type: "NVARCHAR2(500)", nullable: true),
                    CRON_EXPRESSION = table.Column<string>(type: "VARCHAR2(100)", nullable: false),
                    IS_ACTIVE = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    STATUS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    IS_DELETED = table.Column<bool>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobDefinitions", x => x.ID);
                    table.UniqueConstraint("AK_JobDefinitions_ID_JOB_TYPE_ID", x => new { x.ID, x.JOB_TYPE_ID });
                });

            migrationBuilder.CreateTable(
                name: "JobHistories",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    JOB_DEFINITION_ID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    JOB_TYPE_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    TRANSACTION_ID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    UserId = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    USERNAME = table.Column<string>(type: "NVARCHAR2(256)", nullable: true),
                    STARTED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    COMPLETED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    ERROR_MESSAGE = table.Column<string>(type: "NCLOB", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    STATUS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    IS_DELETED = table.Column<bool>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobHistories", x => x.ID);
                    table.ForeignKey(
                        name: "FK_JobHistories_JobDefinitions_JOB_DEFINITION_ID_JOB_TYPE_ID",
                        columns: x => new { x.JOB_DEFINITION_ID, x.JOB_TYPE_ID },
                        principalTable: "JobDefinitions",
                        principalColumns: new[] { "ID", "JOB_TYPE_ID" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobDefinitions_JOB_TYPE_ID",
                table: "JobDefinitions",
                column: "JOB_TYPE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_JobDefinitions_JOB_TYPE_ID_IS_ACTIVE",
                table: "JobDefinitions",
                columns: new[] { "JOB_TYPE_ID", "IS_ACTIVE" });

            migrationBuilder.CreateIndex(
                name: "IX_JobHistories_JOB_DEFINITION_ID_JOB_TYPE_ID",
                table: "JobHistories",
                columns: new[] { "JOB_DEFINITION_ID", "JOB_TYPE_ID" });

            migrationBuilder.CreateIndex(
                name: "IX_JobHistories_JOB_DEFINITION_ID_STARTED_AT",
                table: "JobHistories",
                columns: new[] { "JOB_DEFINITION_ID", "STARTED_AT" });

            migrationBuilder.CreateIndex(
                name: "IX_JobHistories_JOB_TYPE_ID_STARTED_AT",
                table: "JobHistories",
                columns: new[] { "JOB_TYPE_ID", "STARTED_AT" });

            migrationBuilder.CreateIndex(
                name: "IX_JobHistories_TRANSACTION_ID",
                table: "JobHistories",
                column: "TRANSACTION_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobHistories_USERNAME_STARTED_AT",
                table: "JobHistories",
                columns: new[] { "USERNAME", "STARTED_AT" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobHistories");

            migrationBuilder.DropTable(
                name: "JobDefinitions");
        }
    }
}
