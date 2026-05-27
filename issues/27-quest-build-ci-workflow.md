# 27. Quest Build CI Workflow

## Goal
Add a separate GitHub Actions workflow that builds the Quest 3 Android APK on every PR (or on `workflow_dispatch`) and uploads it as a build artifact. Mirrors the existing `unity-tests.yml` infrastructure but with `game-ci/unity-builder@v4` instead of unity-test-runner, targeting `Android`. No auto-deploy to devices — manual `adb install` from the artifact.

## Acceptance Criteria

### New workflow file
- [ ] Create `.github/workflows/quest-build.yml`
- [ ] Triggers: `pull_request` to main (paths-filter: only fire when `Assets/**`, `Packages/**`, `ProjectSettings/**`, or the workflow itself changes — avoids burning minutes on docs-only PRs), `push` to main, `workflow_dispatch`
- [ ] Top-level `permissions:` block matching `unity-tests.yml` (contents: read, checks: write, pull-requests: write, actions: read) — addresses the same "Resource not accessible by integration" issue PR #41 fixed for tests
- [ ] One job: `quest-build` on `ubuntu-latest`

### Build step
- [ ] `actions/checkout@v4` with `lfs: true`
- [ ] `actions/cache@v4` for `Library/` keyed on `Library-Android-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}`
- [ ] `game-ci/unity-builder@v4` with:
  - `unityVersion: 2022.3.62f1` (match `unity-tests.yml`)
  - `targetPlatform: Android`
  - `buildName: CorgiCommando`
  - `androidExportType: androidPackage` (APK, not Bundle for sideload)
  - `androidKeystoreName: ${{ secrets.ANDROID_KEYSTORE_NAME }}`
  - `androidKeystoreBase64: ${{ secrets.ANDROID_KEYSTORE_BASE64 }}`
  - `androidKeystorePass: ${{ secrets.ANDROID_KEYSTORE_PASS }}`
  - `androidKeyaliasName: ${{ secrets.ANDROID_KEYALIAS_NAME }}`
  - `androidKeyaliasPass: ${{ secrets.ANDROID_KEYALIAS_PASS }}`
- [ ] `env:` Unity license vars (same as tests workflow)
- [ ] Concurrency group: `quest-build-${{ github.ref }}` with `cancel-in-progress: true`

### Artifact upload
- [ ] `actions/upload-artifact@v4` on completion: APK at `build/Android/CorgiCommando.apk`, retention 30 days
- [ ] Upload also runs on failure (`if: always()`) so a partial build's logs survive

### Documentation in the workflow
- [ ] Header comment explaining: this workflow does NOT deploy; sideload is manual via `adb install` from the downloaded artifact

## Tests to Pass

This issue is workflow infra — no EditMode tests. Validation is:
- [ ] First merge / `workflow_dispatch` produces a downloadable `CorgiCommando.apk` artifact
- [ ] Manual `adb install CorgiCommando.apk` to a real Quest 3 succeeds
- [ ] APK appears in Quest "Unknown Sources" library and launches without crashing

## Dependencies
- Issue #23 (Android Build Target & Meta XR SDK) — must merge so the project actually has an Android build configuration
- Issue #24 (XR Rig & Theater Scene) — soft dep; build will succeed without #24, but the running app won't render correctly until #24 lands
- Issue #41-equivalent on this workflow — already covered by including `permissions:` block from the start

## Notes for Implementer

### Secrets required
Before the workflow runs successfully, these repo secrets must be set (one-time setup in repo Settings → Secrets → Actions):
- `UNITY_LICENSE` (or `UNITY_EMAIL` + `UNITY_PASSWORD`) — same as test workflow
- `ANDROID_KEYSTORE_BASE64` — base64-encoded contents of the dev keystore from #23
- `ANDROID_KEYSTORE_NAME` — file name (e.g. `corgi-commando-dev.keystore`)
- `ANDROID_KEYSTORE_PASS` — keystore password
- `ANDROID_KEYALIAS_NAME` — `corgi-commando`
- `ANDROID_KEYALIAS_PASS` — key alias password

Document the `base64 < corgi-commando-dev.keystore | pbcopy` command in `docs/quest-deploy.md` (created in #28).

### Why a separate workflow file
Keeping `quest-build.yml` separate from `unity-tests.yml` means:
- Test failures don't block the build artifact (useful for local-iteration "I just want the APK" cases)
- Concurrency groups are independent — build and test runs don't cancel each other
- Quest build is heavier than tests (~15 min cold) and shouldn't gate fast test feedback

### Paths filter
The `paths` filter on the trigger means docs-only changes don't trigger a 15-min Android build. Inspect that PR #39 / #40 (docs-only) would NOT trigger this workflow.

### Out of scope
- Auto-deploy to a device (would require a self-hosted runner with a Quest connected — possible later for nightly smoke tests)
- Quest Store / App Lab submission automation
- AAB (Android App Bundle) build for store submission (APK only for sideload now)
- Build matrix across multiple Quest variants (Quest 2, Quest 3, Quest 3S share the same target so no matrix needed)
