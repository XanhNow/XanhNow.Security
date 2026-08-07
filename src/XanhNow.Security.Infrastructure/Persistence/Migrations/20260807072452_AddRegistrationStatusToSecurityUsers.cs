using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XanhNow.Security.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationStatusToSecurityUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "passkey_registered_at_utc",
                schema: "security",
                table: "security_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_registered_at_utc",
                schema: "security",
                table: "security_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "registration_completed_at_utc",
                schema: "security",
                table: "security_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registration_status",
                schema: "security",
                table: "security_users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Completed");

            migrationBuilder.Sql("""
                UPDATE security.security_users
                SET
                    registration_status = 'Completed',
                    password_registered_at_utc = COALESCE(password_registered_at_utc, created_at),
                    passkey_registered_at_utc = COALESCE(passkey_registered_at_utc, created_at),
                    registration_completed_at_utc = COALESCE(registration_completed_at_utc, created_at)
                WHERE registration_status IS NULL OR registration_status = '' OR registration_status = 'Completed';
                """);

            migrationBuilder.CreateIndex(
                name: "ix_security_users_registration_status",
                schema: "security",
                table: "security_users",
                column: "registration_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_security_users_registration_status",
                schema: "security",
                table: "security_users");

            migrationBuilder.DropColumn(
                name: "passkey_registered_at_utc",
                schema: "security",
                table: "security_users");

            migrationBuilder.DropColumn(
                name: "password_registered_at_utc",
                schema: "security",
                table: "security_users");

            migrationBuilder.DropColumn(
                name: "registration_completed_at_utc",
                schema: "security",
                table: "security_users");

            migrationBuilder.DropColumn(
                name: "registration_status",
                schema: "security",
                table: "security_users");
        }
    }
}
