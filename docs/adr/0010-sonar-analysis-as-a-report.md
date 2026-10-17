# Sonar analysis as a report

Every pull request and every push to `main` carries a SonarQube Cloud analysis of `Domingax/oryndra`: its quality gate, its new issues, its duplications and the coverage of the movement core. It runs in a second job of `.github/workflows/verify.yml` — `sonar`, beside `verify` — wrapping the SonarScanner for .NET around a build and the core tests, with the token in the `SONAR_TOKEN` repository secret and the scope of the analysis in `SonarQube.Analysis.xml`.

It is a **report, not a gate**. `./verify.sh` stays the authority on whether a change is acceptable, and it stays ignorant of Sonar: no token, no network, no analysis. The quality gate is *shown* on the pull request and can be read there or from an agent through the SonarQube tooling the harness already has; no rule requires it to pass for a merge to be allowed.

## Considered and rejected

- **A self-hosted SonarQube**: hosting, upgrading, backing up and securing a server for one public repository. SonarQube Cloud is free for public code, and it is already what the agents' SonarQube tooling in this environment points at, so a second instance would be a second truth.
- **Automatic analysis** (SonarQube Cloud can analyse a .NET repository with no CI at all): no coverage — which is half of what this analysis is for — no analysis of non-default branches, no readable logs, and it refuses to coexist with a CI-based analysis, so that toggle has to be off (see the consequences).
- **Folding the scanner into `./verify.sh`**: the scanner rewrites the build's ruleset and turns warnings-as-errors off, so the gate would stop being the command that is trusted exactly while it runs; and it needs a token, so the local loop would stop working without one. A second command, and a second job, keeps the gate one command that works on a machine that has never heard of Sonar.
- **Making the quality gate block the merge**: the gate's conditions and thresholds live in the SonarCloud UI, outside review, so a merge would depend on a setting no commit records — and a red report would become indistinguishable from a broken build. Turning the check into a gate is a decision of its own, not a side effect of adding a report.
- **Coverage of the whole solution**: nothing in this repository records line coverage for the Godot layer, which is proven by booting the project and by the runtime tests. Counting it would report a percentage nobody can move and hide the one that matters, so `sonar.coverage.exclusions` narrows coverage to `src/Oryndra.Core` (see `docs/testing.md`).
- **`sonar-project.properties`** (the obvious file to reach for, and the one this ticket asked for): the SonarScanner for .NET does **not** read it. It is the CLI scanner's convention; the .NET scanner reads its own `SonarQube.Analysis.xml`, passed with `/s:`, and its log says so (`sonar-project.properties files are not understood by the SonarScanner for MSBuild`). A file that looks authoritative and changes nothing is worse than no file, so the scope lives in the file this scanner actually reads.
- **Excluding the test projects from the analysis** (`sonar.dotnet.excludeTestProjects`): tests are analysed as test code, which is what lets Sonar tell a test from a source file instead of asking for tests of tests.

## Consequences

- CI has two jobs. `verify` is the gate: the same `./verify.sh` as always. `sonar` is the report: it builds, runs the core tests for coverage, and uploads both. Only the default branch and pull requests are analysed, because the free plan offers no branch analysis, and a pull request from a fork is skipped rather than failed, because it carries no secret.
- The `sonar` job builds with warnings *not* treated as errors: the Sonar rules arrive as build warnings, and a report that fails the build on its own findings is a gate by another name.
- `SonarQube.Analysis.xml` at the repository root carries what is analysed, what is measured and what is excluded, and CI passes it to the scanner with `/s:`. The project key, the organization and the token are not in it: the key and the organization belong to the CI step, the token to the repository secret.
- Coverage, as reported, means `src/Oryndra.Core`. A change to the movement rules moves it; a change to the Godot layer does not, and is proven at the boot and runtime levels instead.
- The SonarQube Cloud project must not have *automatic analysis* enabled (project → Administration → Analysis Method): SonarQube Cloud fails a CI-based analysis while both are on. A failed `sonar` job is worth reading before assuming the code is at fault.
- How to read a report — the pull request, and the tools an agent uses — is in `docs/testing.md`.

## Evidence

Issue #17, the `sonar` job and `SonarQube.Analysis.xml` themselves, and the SonarQube Cloud documentation: *SonarScanner for .NET* (begin / build / end, `sonar.token`, `/s:`), *.NET test coverage* (`sonar.cs.cobertura.reportsPaths`), *Automatic analysis* (its limits, and the conflict with a CI analysis). For `sonar-project.properties`: the scanner's own log line and the Sonar community's answer, *"You cannot define properties in a sonar-project.properties file"* when using the SonarScanner for .NET. `docs/testing.md` states what this report does **not** replace.
