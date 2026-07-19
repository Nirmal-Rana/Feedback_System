using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeIssueManagement.Migrations
{
    /// <inheritdoc />
    public partial class photo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "Teachers",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastLogin",
                value: new DateTime(2026, 7, 18, 23, 0, 42, 288, DateTimeKind.Local).AddTicks(5049));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "Teachers");

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastLogin",
                value: new DateTime(2026, 7, 2, 19, 52, 21, 666, DateTimeKind.Local).AddTicks(2282));
        }
    }
}
