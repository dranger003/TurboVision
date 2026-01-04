# CLAUDE.md

This project is a work in progress C# 14 / .NET 10 port of [magiblot/tvision](https://github.com/magiblot/tvision), a modern reimplementation of Borland's Turbo Vision TUI framework.

## Project Structure

- `Reference/tvision/` — upstream C++ source (git submodule)
- `TurboVision/` — the C# port (library)
- `Examples/` — ported example applications
- `Documentation/` — porting status and implementation notes
- `TurboVision.Tests/` — testing of the main C# library

## Common Commands

**IMPORTANT:** Always build individual projects, never the solution directly. The environment may have `Platform=x64` set which causes MSB4126 errors with solution-level builds.

```bash
# Build the library
dotnet build TurboVision/TurboVision.csproj

# Run tests
dotnet test --project TurboVision.Tests/TurboVision.Tests.csproj

# Build and run the Hello example
dotnet build Examples/Hello/Hello.csproj
dotnet run --project Examples/Hello/Hello.csproj
```

## Build Notes

- **NEVER use `dotnet build` without specifying a project** - it will try to build the solution and fail with MSB4126
- **Use `dotnet test --project <path>` NOT `dotnet test <path>`** - the `--project` flag is required in .NET 10
- **Do NOT specify platform-specific configurations** like `Debug|x64` or `/p:Platform=x64` - this solution only supports `Any CPU`

## Guidelines

### Porting Reference
- `Reference/tvision/` — upstream C++ source to port from
- `Reference/SOURCE.md` — quick reference for upstream structure

### Code Standards
- `Documentation/CODING_STYLE.md` — C# 14/.NET 10 style conventions
- `Documentation/BCL_MAPPING.md` — C++ → BCL type decisions
- `Documentation/SERIALIZATION_DESIGN.md` — streaming architecture

### Project Status
- `Documentation/IMPLEMENTATION_STATUS.md` — completion status and next steps
- `Documentation/IMPLEMENTATION_PHASES.md` — roadmap and phase dependencies

### Constraints
- No external dependencies beyond Microsoft NuGet packages
