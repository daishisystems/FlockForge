# Contributing to FlockForge

## Coding Style & Naming Conventions
- **Indentation**: use 4 spaces for C# files and 2 spaces for XAML.
- **C# Naming**:
  - Classes, methods, and properties use `PascalCase`.
  - Private fields use `camelCase` prefixed with an underscore (e.g., `_service`).
  - Async methods are suffixed with `Async`.
- **XAML Naming**:
  - Controls and resources follow `PascalCase`.
  - Bindable property names mirror the backing ViewModel properties.
- **Platform-specific shims**: place platform code under `Platforms/<OS>/` and use partial classes or interfaces to keep shared code clean.
- **Formatting tools**: the repository relies on the default .NET formatting rules. Run `dotnet format` before committing to normalize style.

## Testing Guidelines
- Unit tests are expected to use [xUnit](https://xunit.net/).
- Aim for at least 70% line coverage for new code.
- Name test methods using `MethodUnderTest_Condition_ExpectedResult`.
- Run all tests locally with:
  ```bash
  dotnet test
  ```
  CI runs the same command.

## Commit & Pull Request Guidelines
- **Commit messages** follow `type(scope): summary` in the present tense, e.g. `fix(iOS): handle keyboard observers`.
- Keep commits focused; avoid mixing unrelated changes.
- **Pull Requests** should include:
  - Clear description of the changes.
  - Linked issue or task reference when applicable.
  - Screenshots for any UI changes.
  - Notes on how the change was tested.

Thanks for contributing!
