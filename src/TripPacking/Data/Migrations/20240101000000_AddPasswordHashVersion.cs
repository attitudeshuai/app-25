using Microsoft.EntityFrameworkCore.Migrations;

namespace TripPacking.Data.Migrations;

public partial class AddPasswordHashVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "PasswordHashVersion",
            table: "Users",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PasswordHashVersion",
            table: "Users");
    }
}
