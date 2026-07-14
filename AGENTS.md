# AGENTS DOCUMENT

## Architecture

Dependency chain: `NekoSubscription` -> `NekoSubscription.Core` -> `NekoSubscription.Entities`

- **`NekoSubscription.Entities`** only holds core domain models for the subscription management business (e.g., subscription plans, billing, user entitlements). Logging, configuration, infrastructure, or other cross-cutting models do not belong here.

## Key Conventions

- **No XAML.** Views are written in C# using `Avalonia.Markup.Declarative` (fluent API). There are no `.axaml` files.
- **MVVM** via `CommunityToolkit.Mvvm` source generators. ViewModels inherit `ViewModelBase` and use `[ObservableProperty]` partial properties.
- **ViewLocator** resolves views from view models by convention: replaces `ViewModel` with `View` in the type name (e.g., `MainViewModel` → class named `MainView`). New View/ViewModel pairs must follow this naming convention.
- **EF Core** with SQLite lives in `NekoSubscription.Core`. Migrations tooling is installed there (`Microsoft.EntityFrameworkCore.Tools`).
- **Leverage all available skills.** When writing, reviewing, or refactoring code, proactively use any skills configured for the project or individual to ensure consistency and best practices.

