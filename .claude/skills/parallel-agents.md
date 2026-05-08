# Parallel Agents Skill

Dùng khi task có nhiều phần lớn, độc lập với nhau.

## Luôn Hỏi Trước

KHÔNG dispatch agents mà không có confirmation. Present:
- Agent nào làm gì
- File nào mỗi agent owns
- Tradeoffs của parallel vs sequential

Chờ explicit approval.

## Khi Nào Dùng Parallel Agents

**Good candidates:**
- Implement multiple independent features
- Create multiple unrelated components
- Write tests cho nhiều modules
- Generate documentation cho nhiều areas
- Setup multiple independent services

**Bad candidates:**
- Tasks có shared state
- Sequential dependencies (A phải xong trước B)
- Single file modifications
- Tightly coupled changes

## Dispatching

Mỗi agent prompt phải include:
1. Mô tả task chính xác
2. File paths agent owns
3. File paths agent KHÔNG được touch
4. Cách run tests
5. Definition of done

**Example:**
```
Agent 1: Implement User Registration API
- Own: src/DainnUser.Api/Controllers/AuthController.cs (Register methods only)
- Own: src/DainnUser.Application/Services/RegistrationService.cs
- Own: tests/DainnUser.UnitTests/RegistrationServiceTests.cs
- Don't touch: Login-related code, other controllers
- Test: dotnet test tests/DainnUser.UnitTests/RegistrationServiceTests.cs
- Done: Registration endpoint works, tests pass, validation complete

Agent 2: Implement User Profile API
- Own: src/DainnUser.Api/Controllers/ProfileController.cs
- Own: src/DainnUser.Application/Services/ProfileService.cs
- Own: tests/DainnUser.UnitTests/ProfileServiceTests.cs
- Don't touch: Auth-related code
- Test: dotnet test tests/DainnUser.UnitTests/ProfileServiceTests.cs
- Done: Profile CRUD endpoints work, tests pass
```

## Sau khi Hoàn Thành

1. **Review tất cả changes cùng nhau**
   - Check conflicts
   - Verify integration points
   - Ensure consistent patterns

2. **Run full test suite**
   ```bash
   dotnet test
   ```

3. **Resolve conflicts**
   - Merge changes carefully
   - Test integration

4. **Commit cùng nhau**
   - Single commit hoặc logical commits
   - Descriptive commit message

## Example Workflow

```bash
# 1. Create feature branch
git checkout -b feat/user-management

# 2. Dispatch parallel agents (via Agent tool)
# Agent 1: Registration
# Agent 2: Profile
# Agent 3: Password Reset

# 3. Wait for completion

# 4. Review all changes
git status
git diff

# 5. Run tests
dotnet test

# 6. Commit
git add .
git commit -m "feat: implement user management APIs

- Registration with email verification
- Profile CRUD operations
- Password reset flow

Co-Authored-By: Claude Opus 4 <noreply@anthropic.com>"
```
