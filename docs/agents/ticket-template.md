# Ticket template (for the agent workflow)

Used by the planner when writing `.scratch/<stage>/issues/NN-slug.md`. This overrides the default `/to-tickets` shape: a ticket must be self-sufficient for a lighter implementer that reads only `AGENTS.md` and this file.

**Sizing:** at most about **80k tokens** of work for one implementer (reading, writing, tests, fixes), one project per ticket where possible, split by layer (Core, then component, then page). A ticket whose Read first list needs more than about 10 files is too big.

**Parallelism:** two tickets in the same wave must not edit the same file. Shared files (`_Imports.razor`, `Program.cs`, `*.csproj`, shared CSS) are edited by one ticket, and every other ticket touching them is `Blocked by` it.

```markdown
# NN: <title>

Read `AGENTS.md` first.

Stage: <stage>   Wave: <n>   Blocked by: <ticket numbers or None>
Branch: <stage>/NN-slug   Project(s) you may edit: <Core | Ui | Web | tests>

## What to build
<Concrete behaviour. Names of the types and functions to create, their signatures where it matters, and the rules in plain language with the glossary terms. Include the exact rule text or numbers from the spec that this ticket needs, copied in, so the implementer does not open the spec.>

## Read first
- `<path>`: <why, one line>
- (the existing types or tests to imitate; at most about 10 files)
- Glossary terms used: <Term>, <Term> (look up in `GLOSSARY.md`)

## Files you may create or edit
- <exact paths or folders>
Anything else is out of scope. If you need it, write a blocked report.

## Acceptance
- [ ] <observable, testable criterion>
- [ ] <...>
- [ ] Tests written first (red run shown in the report), full `dotnet build` and `dotnet test` green
- [ ] No data model, exchange format or storage change (unless stated above)

## Do not
<Traps specific to this ticket: things the planner expects a lighter model to get wrong.>
```
