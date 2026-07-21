using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollegeIssueManagement.Migrations
{
    /// <inheritdoc />
    public partial class section : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProfessionalClass",
                table: "Teachers",
                newName: "Section");

            migrationBuilder.RenameColumn(
                name: "ProfessionalClass",
                table: "TeacherFeedbacks",
                newName: "Section");

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastLogin",
                value: new DateTime(2026, 7, 20, 19, 14, 4, 746, DateTimeKind.Local).AddTicks(7691));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Section",
                table: "Teachers",
                newName: "ProfessionalClass");

            migrationBuilder.RenameColumn(
                name: "Section",
                table: "TeacherFeedbacks",
                newName: "ProfessionalClass");

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastLogin",
                value: new DateTime(2026, 7, 19, 7, 3, 41, 669, DateTimeKind.Local).AddTicks(746));
        }
    }
}
