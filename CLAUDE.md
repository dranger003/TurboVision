# CLAUDE.md

This project is a work in progress C# 14 / .NET 10 port of [magiblot/tvision](https://github.com/magiblot/tvision), a modern reimplementation of Borland's Turbo Vision TUI framework.

## Project Structure

- `Reference/tvision/` — upstream C++ source (git submodule)
- `TurboVision/` — the C# port (library)
- `Examples/` — ported example applications
- `Documentation/` — porting status and implementation notes
- `TurboVision.Tests/` — testing of the main C# library

## Common Commands

```bash
# Run tests
dotnet test --project TurboVision.Tests/TurboVision.Tests.csproj

# Build the solution
dotnet build

# Run the Hello example
dotnet run --project Examples/Hello/Hello.csproj
```

## Guidelines

- Follow `Reference/SOURCE.md` for upstream source quick reference
- Consult the upstream source in `Reference/tvision/` for original source code to port
- Follow `Documentation/CODING_STYLE.md` for code style conventions
- No external dependencies (other than Microsoft nugets) unless otherwise noted
- Follow `Documentation/IMPLEMENTATION_STATUS.md` for current state summary and next steps
