using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XanhNow.Security.Infrastructure.Persistence.Migrations;

public partial class AddRegistrationPhoneNumberToSecurityUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "registration_phone_number",
            schema: "security",
            table: "security_users",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "registration_phone_number",
            schema: "security",
            table: "security_users");
    }
}
