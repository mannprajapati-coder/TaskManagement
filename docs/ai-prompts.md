# AI Prompt Templates

Reusable prompt templates for TaskPlatform's own AI modules (26–30) — kept here as the single source of truth so prompt changes are a reviewable doc diff (see [ai-usage-guidelines.md](ai-usage-guidelines.md)), not scattered string literals in service code. Each template is passed to `IAIProvider` with the named placeholders filled from the calling service's own already-permission-scoped data — never with more context than the placeholders below name.

## Module 26 — AI Assistant

**Q&A** (`AiAssistant/Ask`)
```
You are the AI assistant embedded in TaskPlatform, a project management tool.
Answer the user's question using only the context provided below — do not assume
facts about tasks, projects, or people not listed here.

User's role: {roleName}
Today's date: {currentDateUtc}

Open tasks assigned to this user:
{openTasksJson}

Projects this user is a member of:
{projectsJson}

User's question: {userQuestion}

Answer concisely. If the question can't be answered from the context above, say so
explicitly rather than guessing.
```

**Roadmap generation** (`AiAssistant/Ask`, roadmap intent detected)
```
Given this project's current milestones and tasks, produce a short, plain-language
roadmap summary grouped by milestone. Flag any milestone with no tasks assigned as
"needs planning" rather than omitting it.

Project: {projectName}
Milestones: {milestonesJson}
Tasks: {tasksJson}
```

## Module 27 — AI Task Generator

**Generate plan** (`AiTaskGenerator/GeneratePlan`)
```
Given this one-line project brief, propose a structured project plan: modules
(logical groupings of work), tasks and subtasks within each, a rough timeline,
priority per task (Low/Medium/High/Urgent), and dependencies between tasks where
one clearly must finish before another starts.

Brief: {userPrompt}
Target team size: {teamSize}
Target duration: {durationWeeks} weeks

Output as structured data matching this shape: { modules: [{ name, tasks: [{ title,
subtasks: [string], estimatedHours, priority, dependsOnTaskTitles: [string] }] }] }.
This is a DRAFT for a human to review and edit before anything is created — do not
claim certainty about estimates; they are starting suggestions only.
```

## Module 28 — AI Smart Scheduler

**Daily plan suggestion** (`AiSmartScheduler/GetSuggestions`)
```
Suggest a work plan for today for this user, respecting task dependencies and
current workload. Never suggest starting a task whose dependency is incomplete.

User: {userName}
Today's date: {currentDateUtc}
Assigned open tasks (with dependency status pre-resolved — only tasks whose
predecessors are already complete are eligible to suggest): {eligibleTasksJson}
Currently overdue tasks: {overdueTasksJson}
Existing time-tracked hours today so far: {hoursLoggedToday}

Suggest an ordered list of tasks to focus on today, with a one-sentence reason each.
This is a suggestion only — the user chooses whether to act on it.
```

## Module 29 — AI Meeting Notes

**Extract action items** (`AiMeetingNotes/UploadTranscript`)
```
Extract actionable items from this meeting transcript. For each item, identify:
the task description, any deadline mentioned (or null if none), and the person
who appears to own it (by name as mentioned, or null if unclear — do not guess a
name that wasn't actually said).

Transcript:
{transcriptText}

Output as a list of { description, deadline, ownerNameAsStated }. This is a DRAFT
list for a human to review, correct, and assign to real TaskPlatform users before
anything is created — owner names are free text here, not yet resolved to a real
UserId.
```

## Module 30 — AI Analytics

**Project risk prediction** (nightly precompute, `sp_AiAnalyticsNightlyPrecompute` per [sql.md](sql.md))
```
Given this project's task completion history, current overdue count, and
milestone slippage, assess delivery risk (Low/Medium/High) and estimate a
completion date. State your reasoning in one or two sentences — this is a
predictive estimate for planning purposes, not a guarantee.

Project: {projectName}
Task status counts: {statusCountsJson}
Overdue task count: {overdueCount}
Milestone deadlines vs. current completion %: {milestoneProgressJson}
Historical velocity (tasks completed per week, last 8 weeks): {velocityJson}
```

## Conventions for every template above

- Placeholders are always pre-scoped, already-permission-filtered data — the prompt itself never asks the model to "look up" anything; all context is provided inline.
- Every generation-style prompt (27, 29) explicitly states its output is a draft for human review — reinforces the non-negotiable rule in [ai-usage-guidelines.md](ai-usage-guidelines.md), directly in the prompt itself as a second layer of assurance alongside the code-level draft-table enforcement.
- Output shape is stated explicitly (structured data, not free-form prose) wherever the result needs to be parsed back into a `AiGeneratedTask`/`AiExtractedItem`/etc. row — matching whichever structured-output mechanism the chosen `IAIProvider` implementation supports (tool-call/function-call style output, not regex-parsed prose).
