# Repository Guidelines

## Project Structure & Module Organization
- Root files: `fufu-toolbox.csproj`, `App.xaml`, `MainWindow.xaml`, `README.md`, `ARCHITECTURE.md`.
- Core folders:
  - `Pages/`: UI pages (`HomePage`, `MergeTxtPage`, `SettingsPage`).
  - `ViewModels/`: page/window state and user actions.
  - `Services/`: reusable business logic (`NavigationService`, `ThemeService`, `TxtMergeService`).
  - `Models/`: simple data objects used by views and services.
  - `Assets/`: icons and image resources.
  - `Properties/PublishProfiles/`: publish profiles for packaging/release.

## Build, Test, and Development Commands
- Restore dependencies: `dotnet restore`
- Build (x64): `dotnet build -p:Platform=x64`
- Publish (Release): `dotnet publish -c Release -p:Platform=x64`
- Run locally: open `fufu-toolbox.csproj` in Visual Studio and start debugging.

## Coding Style & Naming Conventions
- Language/framework: C# + WinUI 3, MVVM layering.
- Indentation: 4 spaces; keep one responsibility per class/method.
- Naming:
  - `PascalCase` for classes, methods, properties.
  - Private fields use `_camelCase`.
  - Service classes end with `Service`; view models end with `ViewModel`.
- Keep UI interaction in `Pages`, state flow in `ViewModels`, and file/theme/navigation logic in `Services`.

## Testing Guidelines
- Current baseline is build validation.
- Run: `dotnet build -p:Platform=x64`
- When adding tests later, place them under a dedicated test project (for example, `tests/fufu-toolbox.Tests`) and name files by target class (example: `TxtMergeServiceTests.cs`).

## Commit & Pull Request Guidelines
- No Git history is available in this workspace, so use a clear default format:
  - `type(scope): short summary` (example: `feat(merge-txt): support alias ordering`)
  - Common types: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`.
- PRs should include:
  - What changed and why (short).
  - How to verify (commands run).
  - UI screenshots for page/layout changes.
  - Linked task/issue if one exists.

## Security & Configuration Notes
- Do not hardcode paths, keys, or secrets.
- Keep publish/build settings in project files or publish profiles, not in source code constants.
