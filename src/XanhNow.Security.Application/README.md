# XanhNow.Security.Application

Application điều phối use case và chỉ reference Domain.

Boundary bắt buộc:
- Không phụ thuộc ASP.NET Core, EF Core, Npgsql, Redis, Kafka, Vault, HTTP hoặc gRPC implementation.
- Không trả HTTP status, proto message, DbSet, IQueryable hoặc DTO hạ tầng.
- Không mở transaction qua lời gọi app con.
- Controller/API gọi Application; Application gọi port; Infrastructure mới implement port.