# XanhNow.Security Infrastructure Persistence

RB06 owns only persistence for the `security` schema.

Rules:

- Map exactly 10 tables in schema `security`.
- Do not create cross-schema foreign keys to auth/token/passkey/auth_smart_otp.
- Do not expose DbContext, DbSet or IQueryable to Application.
- Do not store password, raw refresh token, OTP, TOTP secret, passkey private key or WebAuthn assertion.
- Writers add rows only; `ILocalUnitOfWork.CommitAsync` is the only save boundary.
