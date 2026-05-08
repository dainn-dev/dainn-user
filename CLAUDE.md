# DainnUser

Class library .NET/C# cung cấp đầy đủ tính năng user management: authentication, authorization, profile management, và security features. Được phân phối qua NuGet package để developers dễ dàng integrate vào ứng dụng của họ.

---

## Project Context

| | |
|---|---|
| **Stack** | .NET/C# Class Library, Entity Framework Core |
| **Database** | Dynamic provider support (SQL Server, PostgreSQL, MySQL, SQLite) |
| **Kiến trúc** | Reusable library với backend API components + frontend web components |
| **Deployment** | NuGet package |
| **Users** | .NET Developers integrate vào apps của họ |

**Luôn nhớ:** 
- Library phải flexible để developers custom theo business logic
- Features enable/disable qua appsettings.json
- OWASP Top 10 compliance là bắt buộc
- Hỗ trợ multiple database providers qua EF Core

---

## Làm việc với Claude (CONSULTANT AGENT mode)

### Flow

Claude nhận yêu cầu → tự plan → tự implement → tự review → bàn giao.
Không cần xác nhận từng bước trừ khi có ambiguity thực sự không thể tự quyết định.

### Triển khai

1. Đọc CLAUDE.md + memory + docs liên quan
2. Nếu yêu cầu không đủ rõ để bắt đầu: hỏi tối đa 2 câu, sau đó tự quyết định
3. Plan → implement → test, không dừng hỏi giữa chừng

Khi code, luôn kiểm tra:
- **Security:** Input đã validate chưa? Có lỗ hổng injection, auth bypass không? OWASP Top 10 compliance?
- **Cluster-safe:** Có dùng in-memory state không? Nếu có → chuyển qua distributed cache
- **Performance:** Có N+1 query không? Cần cache không? Batch được không?
- **Pattern nhất quán:** Có theo đúng .NET conventions và library design patterns không?
- **Side effects:** Thay đổi này có break backward compatibility không?
- **Extensibility:** Code có dễ extend/override cho custom business logic không?

### Self-Review Checklist (chạy trước khi bàn giao)

| # | Kiểm tra | Kết quả |
|---|---|---|
| 1 | Input validation đầy đủ chưa? | ✓ / ✗ |
| 2 | Có lỗ hổng injection / auth bypass không? OWASP Top 10? | ✓ / ✗ |
| 3 | Có dùng in-memory state không an toàn không? | ✓ / ✗ |
| 4 | N+1 query? Cache cần thiết chưa? | ✓ / ✗ |
| 5 | Theo đúng .NET conventions và library patterns chưa? | ✓ / ✗ |
| 6 | Backward compatibility OK? Breaking changes documented? | ✓ / ✗ |
| 7 | Dễ extend/customize cho business logic không? | ✓ / ✗ |
| 8 | Tests pass? | ✓ / ✗ |

### Bàn giao

Sau khi hoàn thành, tóm tắt cho user:
- Đã làm gì
- File nào thay đổi
- Test results
- Kết quả self-review checklist
- Điều gì cần user biết (nếu có)

---

## Project Structure

```
DainnUser/
├── src/
│   ├── DainnUser.Core/              # Core domain models, interfaces
│   ├── DainnUser.Infrastructure/    # EF Core, database implementations
│   ├── DainnUser.Application/       # Business logic, services
│   ├── DainnUser.Api/              # API controllers, DTOs
│   └── DainnUser.Web/              # Web components (Razor, Blazor)
├── tests/
│   ├── DainnUser.UnitTests/
│   ├── DainnUser.IntegrationTests/
│   └── DainnUser.SecurityTests/    # OWASP compliance tests
├── samples/
│   ├── WebApiSample/
│   └── MvcSample/
└── docs/
    ├── architecture.md
    ├── getting-started.md
    └── security.md
```

---

## Key Commands

| Command | Mô tả |
|---|---|
| `dotnet build` | Build solution |
| `dotnet test` | Run all tests |
| `dotnet pack` | Create NuGet package |
| `dotnet format` | Format code |

---

## Skills

| Skill | Khi nào dùng |
|---|---|
| `.claude/skills/testing.md` | Chạy tests, viết tests |
| `.claude/skills/dotnet-library.md` | Build .NET class library, NuGet packaging |
| `.claude/skills/security-review.md` | Review OWASP Top 10 compliance |
| `.claude/skills/parallel-agents.md` | Task lớn có nhiều phần độc lập |
| `.claude/skills/compress-context.md` | Context quá dài |

---

## Memory System

Đọc trước khi bắt đầu task:
- `.claude/memory/MEMORY.md` — project state hiện tại (< 200 lines)
- `.claude/memory/project.md` — stable facts về project
- `.claude/memory/decisions.md` — architectural decisions đã được đưa ra

Cập nhật sau khi hoàn thành task:
- Update `MEMORY.md` nếu project state thay đổi
- Thêm vào `decisions.md` nếu có architectural decision mới

---

## Testing

- Framework: xUnit (unit tests), Integration tests với TestServer
- Run: `dotnet test`
- Coverage: `dotnet test /p:CollectCoverage=true`
- Security: OWASP ZAP automated scans
- Pattern: Test projects mirror src structure

---

## Git & GitHub

- Branches: `feat/<task>`, `fix/<task>`, `chore/<task>` (kebab-case, max 4 từ)
- Commits: nhỏ, thường xuyên, descriptive
- PR: tạo khi task xong, bao gồm change summary + test results

---

## Code Conventions

- **Naming:** PascalCase cho public members, camelCase cho private
- **Async:** Tất cả I/O operations phải async
- **Nullable:** Enable nullable reference types
- **Documentation:** XML comments cho public APIs
- **Dependency Injection:** Constructor injection, avoid service locator
- **Configuration:** Options pattern với IOptions<T>
- **Logging:** ILogger<T> với structured logging
- **Validation:** FluentValidation cho complex validation logic

---

## Context Management

Khi context quá dài (nhiều messages, conversation cũ):
Run compress-context skill → summarize → archive → rewrite MEMORY.md
