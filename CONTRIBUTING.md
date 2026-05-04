# Contributing to TactileCs

Thank you for your interest in contributing! This document describes how to
set up a development environment, run the tests, and submit a pull request.

## Prerequisites

| Tool | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 or later |
| [Git](https://git-scm.com/) | any recent version |
| A C# IDE / editor | Visual Studio 2022, Rider, or VS Code with the C# Dev Kit |

Optional (for GPU features):

| Tool | Version |
|---|---|
| CUDA Toolkit | 12.x |
| NVIDIA GPU | Kepler or later |

## Cloning and building

```bash
git clone https://github.com/graph-technologies/tactile-cs.git
cd tactile-cs
dotnet restore tactile-cs.slnx
dotnet build tactile-cs.slnx
```

## Running tests

```bash
dotnet test tactile-cs.slnx
```

To collect code coverage:

```bash
dotnet test tactile-cs.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
```

Generate an HTML report with ReportGenerator:

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"./coverage/**/*.xml" -targetdir:./coverage-html
```

## Running benchmarks

```bash
dotnet run -c Release --project TactileCs.Benchmarks
```

## Branch naming convention

| Branch type | Pattern | Example |
|---|---|---|
| Feature | `feature/<short-description>` | `feature/add-type-44` |
| Bug fix | `fix/<issue-or-description>` | `fix/polygon-centroid-nan` |
| Documentation | `docs/<short-description>` | `docs/installation-guide` |
| Release | `release/v<version>` | `release/v1.1.0` |
| Hotfix | `hotfix/<description>` | `hotfix/nupkg-icon-missing` |

All feature and fix branches should target `main`.

## Commit style

We follow the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/)
specification:

```
<type>(<scope>): <short summary>

[optional body]

[optional footer(s)]
```

Allowed types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`,
`build`, `ci`, `chore`, `revert`.

**Examples:**

```
feat(tiling): add IsohedralTiling.AllTypes registry
fix(geometry): correct Polygon.Contains for concave shapes
docs: update installation guide with PowerShell example
test(tiling): add FillRegionBounds round-trip test
```

Keep the summary line under 72 characters. Reference issues or PRs in the
footer when applicable:

```
fix(analysis): prevent infinite loop in GetShortestPath

Closes #42
```

## Submitting a pull request

1. Fork the repository and create a branch following the naming convention above.
2. Make your changes, add/update tests, and ensure `dotnet test` passes.
3. Update `CHANGELOG.md` with a note under `[Unreleased]`.
4. Open a pull request against `main` with a clear description.
5. The CI workflow will run automatically; ensure the build and tests pass.
6. A maintainer will review your PR and may request changes before merging.

## Code style

- Follow the existing indentation (tabs for indentation, spaces for alignment).
- Use `var` for local variables where the type is obvious.
- Prefer C# primary constructors and collection expressions where appropriate.
- Add XML doc-comments (`<summary>`, `<param>`, `<returns>`) to all public members.
- Keep methods short and focused; extract helpers for complex logic.
