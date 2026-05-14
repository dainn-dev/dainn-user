using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DainnUser.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContactVerificationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContactId",
                table: "UserTokens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 14, 5, 8, 15, 57, DateTimeKind.Utc).AddTicks(2442), new DateTime(2026, 5, 14, 5, 8, 15, 57, DateTimeKind.Utc).AddTicks(2442) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 14, 5, 8, 15, 57, DateTimeKind.Utc).AddTicks(2442), new DateTime(2026, 5, 14, 5, 8, 15, 57, DateTimeKind.Utc).AddTicks(2442) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 14, 5, 8, 15, 57, DateTimeKind.Utc).AddTicks(2442), new DateTime(2026, 5, 14, 5, 8, 15, 57, DateTimeKind.Utc).AddTicks(2442) });

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_ContactId",
                table: "UserTokens",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_ContactId_TokenType",
                table: "UserTokens",
                columns: new[] { "ContactId", "TokenType" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserTokens_UserContacts_ContactId",
                table: "UserTokens",
                column: "ContactId",
                principalTable: "UserContacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTokens_UserContacts_ContactId",
                table: "UserTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserTokens_ContactId",
                table: "UserTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserTokens_ContactId_TokenType",
                table: "UserTokens");

            migrationBuilder.DropColumn(
                name: "ContactId",
                table: "UserTokens");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 10, 19, 16, 57, 89, DateTimeKind.Utc).AddTicks(6300), new DateTime(2026, 5, 10, 19, 16, 57, 89, DateTimeKind.Utc).AddTicks(6300) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 10, 19, 16, 57, 89, DateTimeKind.Utc).AddTicks(6300), new DateTime(2026, 5, 10, 19, 16, 57, 89, DateTimeKind.Utc).AddTicks(6300) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 10, 19, 16, 57, 89, DateTimeKind.Utc).AddTicks(6300), new DateTime(2026, 5, 10, 19, 16, 57, 89, DateTimeKind.Utc).AddTicks(6300) });
        }
    }
}
