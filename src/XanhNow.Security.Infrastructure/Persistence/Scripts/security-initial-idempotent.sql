DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'security') THEN
        CREATE SCHEMA security;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS security.__ef_migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___ef_migrations_history" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'security') THEN
            CREATE SCHEMA security;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE TABLE security.security_audit_logs (
        audit_log_id uuid NOT NULL,
        user_id uuid,
        action character varying(128) NOT NULL,
        outcome character varying(128) NOT NULL,
        reason_code character varying(128) NOT NULL,
        trace_id character varying(128) NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        event_data_json jsonb,
        CONSTRAINT "PK_security_audit_logs" PRIMARY KEY (audit_log_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE TABLE security.security_grants (
        grant_id uuid NOT NULL,
        user_id uuid NOT NULL,
        grant_type character varying(64) NOT NULL,
        status character varying(64) NOT NULL,
        audience character varying(128) NOT NULL,
        purpose character varying(128) NOT NULL,
        issued_at timestamp with time zone NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        terminal_at timestamp with time zone,
        row_version bigint NOT NULL DEFAULT 1,
        CONSTRAINT "PK_security_grants" PRIMARY KEY (grant_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE TABLE security.security_operation_requests (
        operation_request_id uuid NOT NULL,
        user_id uuid NOT NULL,
        operation_type character varying(128) NOT NULL,
        idempotency_key character varying(160) NOT NULL,
        status character varying(64) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        terminal_at timestamp with time zone,
        row_version bigint NOT NULL DEFAULT 1,
        step_states jsonb NOT NULL,
        CONSTRAINT "PK_security_operation_requests" PRIMARY KEY (operation_request_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE TABLE security.security_outbox_messages (
        outbox_message_id uuid NOT NULL,
        event_id uuid NOT NULL,
        event_type character varying(160) NOT NULL,
        aggregate_type character varying(160) NOT NULL,
        aggregate_id uuid NOT NULL,
        payload_json jsonb NOT NULL,
        headers_json jsonb,
        status character varying(64) NOT NULL,
        retry_count integer NOT NULL,
        last_error character varying(512),
        next_retry_at timestamp with time zone,
        published_at timestamp with time zone,
        occurred_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_security_outbox_messages" PRIMARY KEY (outbox_message_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE TABLE security.security_policies (
        policy_id uuid NOT NULL,
        policy_code character varying(128) NOT NULL,
        policy_version integer NOT NULL,
        rules_json jsonb NOT NULL,
        status character varying(64) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        activated_at timestamp with time zone,
        approved_by uuid,
        row_version bigint NOT NULL DEFAULT 1,
        CONSTRAINT "PK_security_policies" PRIMARY KEY (policy_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE TABLE security.security_policy_decisions (
        decision_id uuid NOT NULL,
        user_id uuid NOT NULL,
        policy_code character varying(128) NOT NULL,
        policy_version integer NOT NULL,
        result character varying(64) NOT NULL,
        reason_code character varying(128) NOT NULL,
        decided_at timestamp with time zone NOT NULL,
        correlation_id character varying(128),
        operation_request_id uuid,
        CONSTRAINT "PK_security_policy_decisions" PRIMARY KEY (decision_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE TABLE security.security_profiles (
        user_id uuid NOT NULL,
        passkey_count integer NOT NULL,
        smart_otp_device_count integer NOT NULL,
        password_login_enabled boolean NOT NULL,
        is_stale boolean NOT NULL,
        snapshot_at timestamp with time zone NOT NULL,
        row_version bigint NOT NULL DEFAULT 1,
        CONSTRAINT "PK_security_profiles" PRIMARY KEY (user_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE TABLE security.security_recovery_cases (
        recovery_case_id uuid NOT NULL,
        user_id uuid NOT NULL,
        status character varying(64) NOT NULL,
        reason_code character varying(128) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        terminal_at timestamp with time zone,
        row_version bigint NOT NULL DEFAULT 1,
        CONSTRAINT "PK_security_recovery_cases" PRIMARY KEY (recovery_case_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE TABLE security.security_sessions (
        session_id uuid NOT NULL,
        user_id uuid NOT NULL,
        jti_hash character varying(160) NOT NULL,
        issued_at timestamp with time zone NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        revoked_at timestamp with time zone,
        row_version bigint NOT NULL DEFAULT 1,
        CONSTRAINT "PK_security_sessions" PRIMARY KEY (session_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE TABLE security.security_users (
        user_id uuid NOT NULL,
        status character varying(64) NOT NULL,
        risk_level character varying(64) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        last_reason_code character varying(128),
        row_version bigint NOT NULL DEFAULT 1,
        CONSTRAINT "PK_security_users" PRIMARY KEY (user_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_audit_logs_trace_id ON security.security_audit_logs (trace_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_audit_logs_user_occurred_at ON security.security_audit_logs (user_id, occurred_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_grants_type_status ON security.security_grants (grant_type, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_grants_user_status ON security.security_grants (user_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_operation_requests_status_expires_at ON security.security_operation_requests (status, expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_operation_requests_user_status ON security.security_operation_requests (user_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE UNIQUE INDEX ux_security_operation_requests_idempotency_key ON security.security_operation_requests (idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_outbox_messages_status_next_retry_at ON security.security_outbox_messages (status, next_retry_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE UNIQUE INDEX ux_security_outbox_messages_event_id ON security.security_outbox_messages (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_policies_code_status ON security.security_policies (policy_code, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE UNIQUE INDEX ux_security_policies_code_version ON security.security_policies (policy_code, policy_version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_policy_decisions_correlation_id ON security.security_policy_decisions (correlation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_policy_decisions_operation_request_id ON security.security_policy_decisions (operation_request_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_policy_decisions_policy_decided_at ON security.security_policy_decisions (policy_code, decided_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_policy_decisions_user_decided_at ON security.security_policy_decisions (user_id, decided_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_profiles_is_stale ON security.security_profiles (is_stale);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_recovery_cases_user_reason ON security.security_recovery_cases (user_id, reason_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_recovery_cases_user_status ON security.security_recovery_cases (user_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_sessions_user_expires_at ON security.security_sessions (user_id, expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE UNIQUE INDEX ux_security_sessions_jti_hash ON security.security_sessions (jti_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_users_risk_level ON security.security_users (risk_level);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    CREATE INDEX ix_security_users_status ON security.security_users (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM security.__ef_migrations_history WHERE "MigrationId" = '20260720142230_InitialSecuritySchema') THEN
    INSERT INTO security.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260720142230_InitialSecuritySchema', '10.0.4');
    END IF;
END $EF$;
COMMIT;

