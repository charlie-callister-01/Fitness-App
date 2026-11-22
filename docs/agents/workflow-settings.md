# Workflow settings

Every skill in the Workflow toolbox reads this file first. Change a value here, not in the skills.
Delete a line to use the default shown in `CUSTOMISING.md`.

## Who and how

- owner: Charlie
- preset: full
- platform: github
- workflow_version: 61d2625
- workflow_track: beta
- workflow_repo: C:\Users\calli\source\repos\Workflow

## Repo

- main_branch: main
- branch_prefixes: feature/, fix/, chore/, throwaway/, research/
- commit_format: type(scope): message
- test_command: dotnet test
- code_pattern: ^(src|tests|tools)/|\.(csproj|props|slnx|sln)$|^\.github/

## State files (presets full and fast-lane)

- status_file: docs/STATUS.md
- handoff_file: docs/HANDOFF.md
- log_file: docs/LOG.md
- plan_file: docs/build-plan.md
- spec_folder: .scratch/<stage>/
- refactor_backlog: docs/REFACTOR.md

## Platform

- pr_command: gh pr create --base main --title "<title>" --body-file <file>
- pr_list_command: gh pr list --state open --json number,title,headRefName --jq '.[] | [.number,.title,.headRefName] | @tsv'

## Optional: Obsidian vault (off)

- vault_folder:
