using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XanhNow.Security.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationDeviceBindingToSecurityUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "registration_device_id",
                schema: "security",
                table: "security_users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registration_phone_number_hash",
                schema: "security",
                table: "security_users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_security_users_registration_device_id",
                schema: "security",
                table: "security_users",
                column: "registration_device_id",
                unique: true,
                filter: "registration_device_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_security_users_registration_device_id",
                schema: "security",
                table: "security_users");

            migrationBuilder.DropColumn(
                name: "registration_device_id",
                schema: "security",
                table: "security_users");

            migrationBuilder.DropColumn(
                name: "registration_phone_number_hash",
                schema: "security",
                table: "security_users");
        }
    }
}
