# Fitness-App
Customisable fitness application for me 

## Build, test, run

```
dotnet build
dotnet test
dotnet run --project src/Web --launch-profile lan
```

The `lan` profile listens on all interfaces at port 5276. Open `http://<your-PC-LAN-IP>:5276` on a phone on the same network (allow port 5276 through the Windows firewall if the phone can't connect). Use `--launch-profile http` to open a browser on localhost instead.

## CI

`.github/workflows/ci.yml` runs on every pull request: it installs the SDK pinned in `global.json`, then runs `dotnet build -c Release` and `dotnet test -c Release --no-build` from the repo root. Warnings are errors, so any warning or failing test fails the check. Deployment is a separate workflow.
