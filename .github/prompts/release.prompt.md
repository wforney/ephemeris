<!-- Updated: 2026-03-22 -->
---
mode: agent
model: anthropic/claude-sonnet-4-5
tools: [codebase, editFiles, runCommands, fetch]
description: Prepare and execute a versioned release — bumps version, validates build, tags, and confirms CI artifacts.
---

You are preparing a versioned release of the Ephemeris project. Follow each step in order and stop if any step fails.

## Pre-flight checks

1. Confirm `main` branch is clean (`git status` shows no uncommitted changes).
2. Run `dotnet build -c Release` — must succeed with 0 errors and 0 warnings.
3. Run `dotnet test` — all tests must pass.
4. Check that the four publish profiles exist:
   `Ephemeris.UI.Avalonia/Properties/PublishProfiles/{win-x64,linux-x64,osx-x64,osx-arm64}.pubxml`

## Determine the version

- Ask the user for the version number if not provided (e.g. `1.2.0`).
- Version must follow **SemVer**: `MAJOR.MINOR.PATCH` (no `v` prefix in the file, but the git tag gets a `v` prefix: `v1.2.0`).
- Check `git tag --list 'v*'` to confirm the tag does not already exist.

## Bump version in project files

Update `<Version>` (and `<AssemblyVersion>`, `<FileVersion>` if present) in:
- `Ephemeris/Ephemeris.csproj`
- `Ephemeris.UI.Avalonia/Ephemeris.UI.Avalonia.csproj`
- `Ephemeris.UI.Shared/Ephemeris.UI.Shared.csproj`

If a `Directory.Build.props` exists with a shared `<Version>` property, update only that file.

## Update CHANGELOG / release notes

- If a `CHANGELOG.md` exists at the repo root, prepend a new `## [VERSION] - YYYY-MM-DD` section summarising the changes since the previous tag (`git log --oneline <prev-tag>..HEAD`).
- Keep the format: Added / Changed / Fixed / Removed sub-sections.

## Commit the version bump

```
chore(release): bump version to X.Y.Z

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

## Tag and push

```bash
git tag v<VERSION>
git push origin main
git push origin v<VERSION>
```

The `v*` tag push triggers `.github/workflows/release.yml`, which:
- Packs and uploads `Ephemeris` NuGet packages to the GitHub Release.
- Publishes four single-file UI binaries (win-x64, linux-x64, osx-x64, osx-arm64) and attaches them as `EphemerisApp-<rid>[.exe]` release assets.

## Verify CI

After pushing the tag:
1. Use the GitHub MCP server to check the `release` workflow run status.
2. Confirm both jobs (`nuget` and `publish-ui` matrix × 4) complete. Individual platform failures are non-fatal (`continue-on-error: true` + `fail-fast: false`); the release succeeds as long as the `nuget` job passes and at least one platform binary uploads.
3. Verify the GitHub Release at `https://github.com/wforney/ephemeris/releases/tag/v<VERSION>` has all five artifacts:
   - `*.nupkg` (core library NuGet)
   - `EphemerisApp-win-x64.exe`
   - `EphemerisApp-linux-x64`
   - `EphemerisApp-osx-x64`
   - `EphemerisApp-osx-arm64`

## Rollback procedure

If the release workflow fails:
1. Delete the remote tag: `git push origin :refs/tags/v<VERSION>`
2. Delete the local tag: `git tag -d v<VERSION>`
3. Revert the version bump commit: `git revert HEAD`
4. Fix the issue, then restart from **Pre-flight checks**.
