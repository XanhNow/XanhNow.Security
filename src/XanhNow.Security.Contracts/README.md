# XanhNow.Security.Contracts

Public REST contract surface for XanhNow.Security.

Rules:
- This project must stay independent. It does not reference Domain, Application, Infrastructure, Api, Worker, Migrator, or child apps.
- Types here are DTOs, route/header/version constants, public enums, and marker attributes only.
- Do not place business logic, persistence, framework types, downstream DTOs, secrets, or internal state here.
- Public REST contracts use version v1 and response contract version 1.0.
