# NekoSubscription

NekoSubscription is a cross-platform desktop application built with .NET and Avalonia UI to help users manage and track their recurring subscriptions.

## Features

- **Dashboard**: Get a quick overview of your subscription expenses and upcoming payments.
- **Calendar**: Visualize your subscription payment schedule on a monthly calendar view.
- **Subscriptions Management**: Easily add, edit, and manage all your active and inactive subscriptions.
- **Cash Flow Projection**: Project your future subscription expenses.
- **Settings**: Configure localization (multiple languages), appearance (dark/light themes), and manage your data.

## Architecture

The application is structured into three main layers following clean architecture principles and the MVVM pattern:

- **NekoSubscription**: The presentation layer. Built using Avalonia UI and `Avalonia.Markup.Declarative` for fluent UI definitions without XAML. It leverages `CommunityToolkit.Mvvm` for ViewModels.
- **NekoSubscription.Core**: Contains the application business logic, data access, and configuration management. It uses Entity Framework Core with SQLite for local data persistence.
- **NekoSubscription.Entities**: Defines the core domain models for the subscription management business.

## Technology Stack

- **Framework**: .NET 10.0
- **UI Framework**: Avalonia UI 12.1.0 (with Declarative Markup)
- **MVVM Toolkit**: CommunityToolkit.Mvvm 8.4
- **ORM**: Entity Framework Core 10.0 (SQLite)
- **Logging**: Serilog

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or newer

## Getting Started

1. Clone the repository.
2. Build the solution using the .NET CLI or your preferred IDE (e.g., Rider, Visual Studio).
   ```bash
   dotnet build NekoSubscription.sln
   ```
3. Run the application:
   ```bash
   dotnet run --project NekoSubscription/NekoSubscription.csproj
   ```

## License

Please refer to the `LICENSE` file for more details.
