# Compress Context Skill

Chạy khi conversation quá dài (50+ messages) hoặc bắt đầu session mới.

## Steps

### 1. Summarize

Tạo summary bao gồm:
- **Tasks completed:** Đã làm gì, files nào thay đổi
- **Decisions made:** Architectural decisions, tech choices
- **In progress:** Tasks đang làm dở
- **Blockers:** Issues cần resolve
- **Next steps:** Gì cần làm tiếp

### 2. Archive

Save summary vào archive file:

```bash
# Generate filename with timestamp
TIMESTAMP=$(date +%Y-%m-%d-%H%M)
ARCHIVE_FILE=".claude/memory/archive/${TIMESTAMP}-summary.md"

# Create archive (Claude will write content)
```

**Archive format:**
```markdown
# Context Archive — [Date Time]

## Period
From: [start date/time]
To: [end date/time]

## Summary
[2-3 paragraphs về gì đã xảy ra]

## Tasks Completed
- Task 1: [description] — [files changed]
- Task 2: [description] — [files changed]

## Decisions Made
- Decision 1: [what] — [why]
- Decision 2: [what] — [why]

## In Progress
- Task X: [status] — [blocker if any]

## Key Learnings
- Learning 1
- Learning 2

## Next Steps
1. Step 1
2. Step 2
```

### 3. Rewrite MEMORY.md

Update `.claude/memory/MEMORY.md` với fresh, concise content:
- Current state (dưới 200 lines)
- Active tasks
- Recent context (last 1-2 sessions)
- Reference archive files cho older context

**Template:**
```markdown
# [Project Name] — Memory

**Stack:** [stack] | **DB:** [db] | **Users:** [user type]

**Luôn nhớ:** [key conventions/gotchas]

**Mode:** [DEV AGENT / CONSULTANT AGENT]

---

## Current State

- Status: [current status]
- Active branch: [branch]
- Last task: [last completed task]

## Key Components

[3-5 thành phần quan trọng nhất với file paths]

## In Progress

[Tasks đang làm]

## Recent Decisions

Xem `.claude/memory/decisions.md`

## Archived Context

- [YYYY-MM-DD-HHMM-summary.md](.claude/memory/archive/YYYY-MM-DD-HHMM-summary.md)

---

_Keep this file under 200 lines. Archive old context with compress-context skill._
```

### 4. Update decisions.md

Nếu có architectural decisions mới, append vào `.claude/memory/decisions.md`:

```markdown
---

## Decision: [Title]

**Date:** [date]
**Decision:** [What was decided]
**Reason:** [Why]
**Alternatives considered:** [What else was considered]

---
```

### 5. Confirm

Report back:
```
Context compressed. Archive saved to .claude/memory/archive/[filename].md

Summary:
- [X] tasks completed
- [Y] decisions archived
- MEMORY.md refreshed
- decisions.md updated

You can continue from where we left off.
```

## When to Run

**Triggers:**
- Conversation > 50 messages
- Starting new session after long break
- Context feels cluttered
- User explicitly requests
- Before major milestone (release, demo)

## Benefits

- Keeps MEMORY.md focused and readable
- Preserves history in archives
- Faster context loading
- Easier to onboard new sessions
- Better long-term project memory
