using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanctum.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePhotoBytes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ProfilePhoto", table: "Users");

            migrationBuilder.AddColumn<byte[]>(
                name: "ProfilePhoto",
                table: "Users",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoType",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePhotoType",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePhoto",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);
        }
    }
}
