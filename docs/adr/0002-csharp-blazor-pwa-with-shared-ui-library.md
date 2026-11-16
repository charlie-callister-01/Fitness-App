---
status: accepted
---

# C# Blazor WebAssembly PWA with a shared Razor class library

The app is a Blazor WebAssembly PWA written in C#, with all screens in a Razor class library (`Ui`) and all rules in a dependency-free `Core` project. A later installable app uses .NET MAUI Blazor Hybrid as a second thin shell over the same `Ui`. The owner works in C#, wants the app built quickly and maintained comfortably, and wants the move to an installable app to be cheap.

## Considered Options

- **TypeScript PWA, wrapped later with Capacitor.** Lowest-friction web-to-app path and a bigger community, but the owner would be reviewing unfamiliar code in every stage.
- **Native app first (MAUI, React Native, Flutter).** Rejected for now: each test needs a build and install on the phone, iOS testing needs a Mac and a paid developer account, and iteration is slower.
- **Obsidian plugin.** Rejected: ties the app to Obsidian and the phone-only requirement.

## Consequences

- First load downloads a larger runtime (cached afterwards); the PWA-specific community is smaller; browser storage needs a small JavaScript bridge.
- The rules must stay out of the screens, and platform differences go behind interfaces, or the later MAUI move stops being cheap.
- Only `index.html`/`App.razor` and the storage implementation differ per host.
- The exact .NET LTS version and template details were confirmed in stage A0: .NET 10 LTS and the standalone Blazor WebAssembly PWA template ([ADR 0004](0004-dotnet-10-lts-pinned.md)).
