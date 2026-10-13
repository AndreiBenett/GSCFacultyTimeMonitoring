using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSCFacultyTimeMonitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddFacultyLateAfter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "LateAfter",
                table: "Faculty",
                type: "time(0) without time zone",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LateAfter",
                table: "Faculty");
        }
    }
}
