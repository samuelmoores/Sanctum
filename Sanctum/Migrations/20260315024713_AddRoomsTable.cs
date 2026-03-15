using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanctum.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Time_Limit = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    Availbility = table.Column<int>(type: "INTEGER", nullable: false),
                    Building = table.Column<string>(type: "TEXT", nullable: false),
                    RoomName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rooms");
        }
    }
}
