# .NET 10 modernization audit — 2026-10-17

Scope: application and tests, NuGet direct/transitive packages, browser assets, GitHub Actions and the Linux runtime image. This records known findings in the queried feeds, not an absolute security guarantee.

## Changes

- .NET 7 (unsupported) → .NET 10; SDK 10.0.402 and ASP.NET Core runtime 10.0.13 available from Microsoft's container registry. Dockerfiles pin Alpine 3.24 manifests by SHA256, run as UID 1654, listen on 8080 and include a HTTP health check. The root Dockerfile builds from repository root; the project Dockerfile builds with `src` as context.
- Updated Blazored LocalStorage 4.5.0, Toast 4.2.1, Serilog ASP.NET Core 10.0.0 / Console 6.1.1, Visual Studio container tooling 1.23.0 and test dependencies. Docker restore copies Razor sources before evaluation because their presence controls the implicit framework static-assets package. Lockfiles cover both projects; `NuGetAuditMode=all`, low severity and NU1901–NU1904 as errors apply at restore. CI and Docker restore locked graphs.
- Actions checkout 7.0.1, setup-dotnet 6.0.0 and upload-artifact 7.0.2 use immutable commit SHAs. CI grants only contents:read, removes persisted checkout credentials, tests before publishing, and passes Docker credentials via stdin. Publishing remains restricted to version tags.
- No npm manifest, vendored JavaScript framework or frontend package lock exists. Browser JavaScript consists of framework assets shipped with the patched runtime and `wwwroot/js/custom-dialog.js`. Google Fonts serves CSS/fonts, not executable scripts; frontend rendering was exercised through the real Blazor circuit.
- Existing unit tests constructed RoomService without its required logger: repaired using NullLogger. Missing stored usernames now return empty string instead of violating a non-null contract.

## Reproduction

```sh
dotnet restore src/PokerPlanning.sln --locked-mode
dotnet build src/PokerPlanning.sln -c Release --no-restore -warnaserror
dotnet test src/PokerPlanning.sln -c Release --no-build
dotnet format analyzers src/PokerPlanning.sln --verify-no-changes --no-restore --severity warn
dotnet list src/PokerPlanning.sln package --vulnerable --include-transitive --format json > nuget-audit.json
python3 scripts/check-nuget-audit.py nuget-audit.json
docker build --network=host -t poker-planning:net10-audit .
trivy image --scanners vuln --format json --output image-audit.json poker-planning:net10-audit
trivy fs --scanners vuln,misconfig --format json --output filesystem-audit.json .
```

The host required `--network=host` for container restore and HTTP smoke; the regular Docker bridge could not reach the expected services. This is a validation host limitation, not an application network requirement. Trivy version: 0.76.0. Its current database was fetched during the audit. Raw reports are checked in alongside this document.

## Browser automation

The C# xUnit Selenium test uses two isolated Chrome sessions. Alice creates a room, Bob joins by URL, both vote (3 and 5), hidden values remain hidden, reveal propagates to both players with average 4, and a new round hides the values again. It saves screenshots, DOM and browser logs on failures. The ordinary suite explicitly skips this test unless `POKER_BASE_URL` is configured; CI starts the real app and always enables it.

```sh
docker run -d --name poker-app --network host -e ASPNETCORE_HTTP_PORTS=5300 poker-planning:net10-audit
docker run -d --name poker-browser --network host --shm-size=2g \
  -e SE_NODE_MAX_SESSIONS=2 -e SE_NODE_OVERRIDE_MAX_SESSIONS=true \
  selenium/standalone-chrome@sha256:d6843a37cd2852fac7f33099709a203769f864dcf260e740a34e9605055c1dd5
POKER_BASE_URL=http://127.0.0.1:5300 SELENIUM_REMOTE_URL=http://127.0.0.1:4444 \
  dotnet test src/PokerPlanning.sln -c Release --no-build
```

Without `SELENIUM_REMOTE_URL`, Selenium Manager resolves a local Chrome driver automatically. The Selenium service must have two browser slots. CI uploads the TRX results, app logs and failure artifacts and stops the background app with its captured PID.

## Recorded results

- Locked restore succeeded with SDK 10.0.402.
- Release build: zero warnings and zero errors.
- Complete suite with real Selenium browsers: 10 passed, zero failed, zero skipped (`tests-selenium.txt`).
- Compiler analyzers: `dotnet format analyzers ... --severity warn` passed without changes.
- NuGet direct/transitive audit: zero known vulnerable dependencies.
- Trivy runtime image: zero known vulnerabilities across Alpine 3.24.2, app dependencies and .NET 10.0.13 runtime.
- Trivy filesystem: zero vulnerability or Dockerfile misconfiguration findings.
- Real container: home returned HTTP 200, non-root runtime user 1654, health check healthy; Selenium connected through its real Blazor WebSocket circuit.

## GitHub and Sonar Cloud limitations

Dependabot API returned an empty alert array. Code scanning returned 404 `no analysis found`; secret scanning returned 404 `Secret scanning is disabled on this repository.` These two results are unavailable coverage, not clean scans. No existing Sonar configuration or token was available, so no Sonar Cloud quality gate has run or passed. Compiler analyzers are the locally executed static analysis.

For the free public-repository Sonar Cloud plan, create/import the public project, disable **Automatic Analysis**, set repository variables `SONAR_ORGANIZATION`, `SONAR_PROJECT_KEY`, `SONAR_ENABLED=true`, and add secret `SONAR_TOKEN`. `.github/workflows/sonar.yml` installs pinned SonarScanner for .NET 12.0.0 and dotnet-coverage 18.12.0, imports VS coverage XML and waits for the quality gate. Missing configuration fails when enabled. Internal PRs and main are supported; fork PRs receive no secret-based scan. While disabled the workflow explicitly reports that no gate was evaluated.

## Sources checked

- https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- https://mcr.microsoft.com/v2/dotnet/sdk/tags/list
- https://mcr.microsoft.com/v2/dotnet/aspnet/tags/list
- https://api.nuget.org/v3/index.json
- https://api.github.com/repos/actions/checkout/releases/latest
- https://api.github.com/repos/actions/setup-dotnet/releases/latest
- https://api.github.com/repos/actions/upload-artifact/releases/latest
- https://docs.sonarsource.com/sonarqube-cloud/advanced-setup/ci-based-analysis/sonarscanner-for-dotnet/
- https://docs.sonarsource.com/sonarqube-cloud/advanced-setup/ci-based-analysis/github-actions-for-sonarcloud/

No PR, merge, Docker Hub push or deployment was performed during this phase. Independent validation is pending.
