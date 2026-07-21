using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeIssueManagement.Migrations
{
    /// <inheritdoc />
    public partial class pc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfessionalClass",
                table: "TeacherFeedbacks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastLogin",
                value: new DateTime(2026, 7, 19, 7, 3, 41, 669, DateTimeKind.Local).AddTicks(746));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfessionalClass",
                table: "TeacherFeedbacks");

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastLogin",
                value: new DateTime(2026, 7, 18, 23, 8, 20, 932, DateTimeKind.Local).AddTicks(6492));
        }
    }
}
