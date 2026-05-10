using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DainnUser.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTwoFactorSecret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecret",
                table: "Users",
                type: "TEXT",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwoFactorSecret",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 8, 8, 14, 3, 161, DateTimeKind.Utc).AddTicks(2904), new DateTime(2026, 5, 8, 8, 14, 3, 161, DateTimeKind.Utc).AddTicks(2904) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 8, 8, 14, 3, 161, DateTimeKind.Utc).AddTicks(2904), new DateTime(2026, 5, 8, 8, 14, 3, 161, DateTimeKind.Utc).AddTicks(2904) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 8, 8, 14, 3, 161, DateTimeKind.Utc).AddTicks(2904), new DateTime(2026, 5, 8, 8, 14, 3, 161, DateTimeKind.Utc).AddTicks(2904) });
        }
    }
}
