# Fitness-App
Customisable fitness application for me 

## CI

`.github/workflows/ci.yml` runs on every pull request: it installs the SDK pinned in `global.json`, then runs `dotnet build -c Release` and `dotnet test -c Release --no-build` from the repo root. Warnings are errors, so any warning or failing test fails the check. Deployment is a separate workflow.
