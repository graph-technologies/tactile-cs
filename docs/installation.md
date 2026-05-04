# Installing TactileCs

TactileCs is published as a NuGet package and can be installed in any .NET project
in seconds.

## Option 1 — dotnet add package (recommended)

```bash
dotnet add package TactileCs
```

Or specify a version explicitly:

```bash
dotnet add package TactileCs --version 1.0.0
```

## Option 2 — GitHub Actions (reusable workflow)

Reference the built-in reusable installer workflow from any GitHub Actions workflow.

```yaml
jobs:
  install-tactilecs:
    uses: graph-technologies/tactile-cs/.github/workflows/install-tactile-cs.yml@main
    with:
      version: '1.0.0'          # or 'latest'
      target-framework: net8.0
      install-dir: ./lib/TactileCs

  build:
    needs: install-tactilecs
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Use installed version
        run: echo "Installed TactileCs ${{ needs.install-tactilecs.outputs.version }}"
```

### Inputs

| Input | Description | Default |
|---|---|---|
| `version` | Package version (`"latest"` resolves automatically) | `latest` |
| `target-framework` | TFM whose DLLs to extract (e.g. `net8.0`) | `net8.0` |
| `install-dir` | Directory to place the extracted DLLs | `./lib/TactileCs` |

### Outputs

| Output | Description |
|---|---|
| `version` | The resolved package version that was installed |

## Option 3 — PowerShell script

For machines where GitHub Actions is not available, use the companion
PowerShell script.

### Basic usage (latest stable)

```powershell
.\scripts\Install-TactileCs.ps1
```

### Specific version

```powershell
.\scripts\Install-TactileCs.ps1 -Version 1.0.0
```

### Custom framework and install directory

```powershell
.\scripts\Install-TactileCs.ps1 `
    -Version 1.0.0 `
    -TargetFramework net8.0 `
    -InstallDir C:\MyApp\lib\TactileCs
```

### Parameters

| Parameter | Description | Default |
|---|---|---|
| `-Version` | Package version (`"latest"` resolves automatically) | `latest` |
| `-TargetFramework` | TFM whose DLLs to extract | `net8.0` |
| `-InstallDir` | Directory to place the extracted DLLs | `./lib/TactileCs` |

The script returns the resolved version string, which can be captured in a
calling script:

```powershell
$installed = .\scripts\Install-TactileCs.ps1 -Version latest
Write-Host "Installed version: $installed"
```

## Verifying the installation

After installing, reference `TactileCs.dll` in your project or add it as a
hint path:

```xml
<Reference Include="TactileCs">
  <HintPath>lib\TactileCs\TactileCs.dll</HintPath>
</Reference>
```

Or use `dotnet add package` (recommended) — no manual DLL management required.
