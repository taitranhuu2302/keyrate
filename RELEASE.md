# KeyRate — release and publish

This guide covers building and publishing the **.NET console app** in [`KeyRate/`](KeyRate/).

## Prerequisites

- **Windows** (the app calls `user32.dll` / FilterKeys).
- [.NET SDK](https://dotnet.microsoft.com/download) matching the project (`net10.0-windows` as configured in [`KeyRate/KeyRate.csproj`](KeyRate/KeyRate.csproj)).

Verify:

```powershell
dotnet --version
```

## Choose a publish mode

| Mode | Needs .NET on target PC? | Typical size | Best for |
|------|---------------------------|--------------|----------|
| **Framework-dependent** | Yes (.NET 10 runtime) | Small | Your machines, teams that already install runtimes |
| **Self-contained** | No | Larger | USB sticks, unknown PCs, simple “copy and run” |

## Framework-dependent publish

Produces a small folder. Target machines must have the **.NET 10** runtime for Windows installed.

**64-bit Windows (most desktops/laptops):**

```powershell
cd KeyRate
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish/fdd-win-x64
```

**ARM64 Windows (some Surface / Snapdragon devices):**

```powershell
cd KeyRate
dotnet publish -c Release -r win-arm64 --self-contained false -o ./publish/fdd-win-arm64
```

**What to ship:** the entire output folder (`publish/fdd-win-x64` or `publish/fdd-win-arm64`). At minimum keep:

- `KeyRate.exe`
- `KeyRate.dll`
- `KeyRate.runtimeconfig.json`
- `KeyRate.deps.json`

You may omit `KeyRate.pdb` from archives sent to end users if you do not want debug symbols included.

## Self-contained publish

Bundles the runtime so recipients **do not** install .NET. Publish output is larger.

**Single-file executable (typical distribution):**

```powershell
cd KeyRate
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish/single-win-x64
```

For ARM64:

```powershell
cd KeyRate
dotnet publish -c Release -r win-arm64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish/single-win-arm64
```

**What to ship:** distribute the contents of the publish folder (at least `KeyRate.exe`; keep companion files if present).

## Quick Release build (no dedicated publish folder)

```powershell
dotnet build -c Release KeyRate/KeyRate.csproj
```

Artifacts appear under `KeyRate/bin/Release/net10.0-windows/` (with optional `win-x64` subpaths when using `-r`).

## Smoke test before release

1. Run the published `KeyRate.exe` interactively.
2. Apply a preset or custom delay/repeat and confirm keyboard repeat feels correct.
3. Use menu option **5** (show current filter keys) and confirm values look sensible.
4. If you use startup integration: verify **add** / **remove** and sign-out/sign-in behavior.

## Notes

- **Elevation:** FilterKeys changes apply per interactive session like the original `keyrate.c`; admin rights are usually **not** required for typical setups.
- **Architecture:** Publishing with `-r win-x64` produces binaries that run on **64-bit Windows only**. Use `-r win-arm64` for ARM64 Windows.
- **Native C utility:** The repository may also contain [`keyrate.c`](keyrate.c); this document applies to the **`KeyRate/`** .NET project unless you maintain separate release steps for the C build.
