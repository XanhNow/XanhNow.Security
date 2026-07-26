# XanhNow.Security.Api boundary

API project là inbound adapter duy nhất cho REST HTTPS.

Quy tắc RB08:
- Controller chỉ map HTTP request sang Application request.
- Controller không gọi trực tiếp DbContext, child-app client, Redis, Kafka hoặc Vault.
- Controller shell không có action nghiệp vụ trước RB12-RB14.
- HealthController là controller duy nhất có action ở RB08.
- Error response không trả stack trace, secret, endpoint nội bộ hoặc downstream body.
