using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XanhNow.Security.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSecuritySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "security");

            migrationBuilder.CreateTable(
                name: "security_audit_logs",
                schema: "security",
                columns: table => new
                {
                    audit_log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    outcome = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    trace_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    event_data_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_audit_logs", x => x.audit_log_id);
                });

            migrationBuilder.CreateTable(
                name: "security_grants",
                schema: "security",
                columns: table => new
                {
                    grant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grant_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    audience = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    purpose = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    terminal_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_grants", x => x.grant_id);
                });

            migrationBuilder.CreateTable(
                name: "security_operation_requests",
                schema: "security",
                columns: table => new
                {
                    operation_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    terminal_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    step_states = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_operation_requests", x => x.operation_request_id);
                });

            migrationBuilder.CreateTable(
                name: "security_outbox_messages",
                schema: "security",
                columns: table => new
                {
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    headers_json = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    next_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_outbox_messages", x => x.outbox_message_id);
                });

            migrationBuilder.CreateTable(
                name: "security_policies",
                schema: "security",
                columns: table => new
                {
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    policy_version = table.Column<int>(type: "integer", nullable: false),
                    rules_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_policies", x => x.policy_id);
                });

            migrationBuilder.CreateTable(
                name: "security_policy_decisions",
                schema: "security",
                columns: table => new
                {
                    decision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    policy_version = table.Column<int>(type: "integer", nullable: false),
                    result = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    operation_request_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_policy_decisions", x => x.decision_id);
                });

            migrationBuilder.CreateTable(
                name: "security_profiles",
                schema: "security",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    passkey_count = table.Column<int>(type: "integer", nullable: false),
                    smart_otp_device_count = table.Column<int>(type: "integer", nullable: false),
                    password_login_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_stale = table.Column<bool>(type: "boolean", nullable: false),
                    snapshot_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_profiles", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "security_recovery_cases",
                schema: "security",
                columns: table => new
                {
                    recovery_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    terminal_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_recovery_cases", x => x.recovery_case_id);
                });

            migrationBuilder.CreateTable(
                name: "security_sessions",
                schema: "security",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jti_hash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_sessions", x => x.session_id);
                });

            migrationBuilder.CreateTable(
                name: "security_users",
                schema: "security",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    risk_level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_users", x => x.user_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_logs_trace_id",
                schema: "security",
                table: "security_audit_logs",
                column: "trace_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_logs_user_occurred_at",
                schema: "security",
                table: "security_audit_logs",
                columns: new[] { "user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_security_grants_type_status",
                schema: "security",
                table: "security_grants",
                columns: new[] { "grant_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_security_grants_user_status",
                schema: "security",
                table: "security_grants",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_security_operation_requests_status_expires_at",
                schema: "security",
                table: "security_operation_requests",
                columns: new[] { "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_security_operation_requests_user_status",
                schema: "security",
                table: "security_operation_requests",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_security_operation_requests_idempotency_key",
                schema: "security",
                table: "security_operation_requests",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_security_outbox_messages_status_next_retry_at",
                schema: "security",
                table: "security_outbox_messages",
                columns: new[] { "status", "next_retry_at" });

            migrationBuilder.CreateIndex(
                name: "ux_security_outbox_messages_event_id",
                schema: "security",
                table: "security_outbox_messages",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_security_policies_code_status",
                schema: "security",
                table: "security_policies",
                columns: new[] { "policy_code", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_security_policies_code_version",
                schema: "security",
                table: "security_policies",
                columns: new[] { "policy_code", "policy_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_security_policy_decisions_correlation_id",
                schema: "security",
                table: "security_policy_decisions",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_policy_decisions_operation_request_id",
                schema: "security",
                table: "security_policy_decisions",
                column: "operation_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_policy_decisions_policy_decided_at",
                schema: "security",
                table: "security_policy_decisions",
                columns: new[] { "policy_code", "decided_at" });

            migrationBuilder.CreateIndex(
                name: "ix_security_policy_decisions_user_decided_at",
                schema: "security",
                table: "security_policy_decisions",
                columns: new[] { "user_id", "decided_at" });

            migrationBuilder.CreateIndex(
                name: "ix_security_profiles_is_stale",
                schema: "security",
                table: "security_profiles",
                column: "is_stale");

            migrationBuilder.CreateIndex(
                name: "ix_security_recovery_cases_user_reason",
                schema: "security",
                table: "security_recovery_cases",
                columns: new[] { "user_id", "reason_code" });

            migrationBuilder.CreateIndex(
                name: "ix_security_recovery_cases_user_status",
                schema: "security",
                table: "security_recovery_cases",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_security_sessions_user_expires_at",
                schema: "security",
                table: "security_sessions",
                columns: new[] { "user_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ux_security_sessions_jti_hash",
                schema: "security",
                table: "security_sessions",
                column: "jti_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_security_users_risk_level",
                schema: "security",
                table: "security_users",
                column: "risk_level");

            migrationBuilder.CreateIndex(
                name: "ix_security_users_status",
                schema: "security",
                table: "security_users",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "security_audit_logs",
                schema: "security");

            migrationBuilder.DropTable(
                name: "security_grants",
                schema: "security");

            migrationBuilder.DropTable(
                name: "security_operation_requests",
                schema: "security");

            migrationBuilder.DropTable(
                name: "security_outbox_messages",
                schema: "security");

            migrationBuilder.DropTable(
                name: "security_policies",
                schema: "security");

            migrationBuilder.DropTable(
                name: "security_policy_decisions",
                schema: "security");

            migrationBuilder.DropTable(
                name: "security_profiles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "security_recovery_cases",
                schema: "security");

            migrationBuilder.DropTable(
                name: "security_sessions",
                schema: "security");

            migrationBuilder.DropTable(
                name: "security_users",
                schema: "security");
        }
    }
}
