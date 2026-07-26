# XanhNow.Security Worker Boundary

RB09 builds the worker foundation only.

- Worker is a host and scheduler. Business decisions stay in Application and Domain.
- Each cycle creates a fresh DI scope and calls Application request handlers.
- Worker does not expose controllers, routes, HTTP DTOs, or OpenAPI.
- Worker does not inject `SecurityDbContext` or child app clients into hosted services.
- Worker does not publish Kafka from API request flow. Outbox dispatch belongs here.
- Worker does not retry unsupported operation or recovery strategies.
- Worker does not log payloads, token values, OTP values, passkey assertions, TOTP secrets, or raw downstream responses.
