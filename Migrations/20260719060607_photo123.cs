using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeIssueManagement.Migrations
{
    /// <inheritdoc />
    public partial class photo123 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastLogin",
                value: new DateTime(2026, 7, 18, 23, 6, 6, 937, DateTimeKind.Local).AddTicks(1170));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastLogin",
                value: new DateTime(2026, 7, 18, 23, 0, 42, 288, DateTimeKind.Local).AddTicks(5049));
        }
    }
}
