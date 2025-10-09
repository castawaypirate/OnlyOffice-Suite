using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlyOfficeServer.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlyOfficeDocumentSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OnlyOfficeDocumentSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OnlyOfficeToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DocumentKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Config = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlyOfficeDocumentSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnlyOfficeDocumentSessions_FileId_OnlyOfficeToken_IsDeleted_ExpiresAt",
                table: "OnlyOfficeDocumentSessions",
                columns: new[] { "FileId", "OnlyOfficeToken", "IsDeleted", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlyOfficeDocumentSessions_OnlyOfficeToken",
                table: "OnlyOfficeDocumentSessions",
                column: "OnlyOfficeToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnlyOfficeDocumentSessions");
        }
    }
}
