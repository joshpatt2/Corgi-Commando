# 28. Quest Deploy & QA Guide

## Goal
Author `docs/quest-deploy.md` — the practical reference doc for deploying Corgi Commando to a Quest 3 and the QA checklist for validating each build. This issue is a docs deliverable, not code. It's the bridge between "the CI workflow produced an APK" and "we know the build works on the device."

## Acceptance Criteria

### File location & structure
- [ ] New `docs/quest-deploy.md`
- [ ] Sections in order:
  1. Prerequisites
  2. One-time device setup
  3. Per-build deploy (sideload)
  4. QA checklist
  5. Common issues
  6. Distribution path (App Lab / Quest Store) — placeholder for future work

### Prerequisites section
- [ ] Document required local tools: Android Studio (or standalone `adb`), Meta Quest Developer Hub (optional but useful), Unity Hub with Android Build Support module
- [ ] Document required Meta account state: developer organization created at developer.meta.com, headset paired to that org

### One-time device setup
- [ ] Steps to enable Developer Mode on the Quest 3 via the Meta Quest app on phone
- [ ] Connect Quest 3 via USB-C, allow USB debugging prompt
- [ ] Verify with `adb devices` shows the headset

### Per-build deploy
- [ ] Step 1: Download the latest `CorgiCommando.apk` from GitHub Actions artifacts (link to the `quest-build` workflow)
- [ ] Step 2: `adb install -r CorgiCommando.apk` (`-r` = reinstall, replacing previous version)
- [ ] Step 3: On the Quest, open Library → Unknown Sources → Corgi Commando
- [ ] Troubleshooting: signing mismatch errors require `adb uninstall com.corgicommando.app` first

### QA checklist (per-build manual validation)
- [ ] App launches without crash, lands on the theater scene
- [ ] Headset orientation and recenter (both grips + 1s) work
- [ ] Theater quad is at a comfortable distance (no eye strain)
- [ ] Frame rate steady at 72 Hz minimum (use Meta Quest Developer Hub's overlay)
- [ ] Left thumbstick moves the corgi
- [ ] Right A button = Jump
- [ ] Right B button = Special
- [ ] Left X button = Punch
- [ ] Left Y button = Kick
- [ ] Either menu button pauses the game
- [ ] Pause menu renders (TextMeshPro, not blank — verifies #26)
- [ ] Combat hits register; hitsparks visible (verifies #22 if landed)
- [ ] HUD readable from quad distance
- [ ] No audio (until audio pass lands)
- [ ] Boss arena flow works (if `Level_Backyard.unity` includes the boss waves)

### Common issues
- [ ] `Failed to install` with signing mismatch — uninstall first
- [ ] `INSTALL_FAILED_INSUFFICIENT_STORAGE` — clear other sideloaded apps
- [ ] App launches but rendering is upside-down — XR camera offset wrong, see #24
- [ ] Black quad with no game visible — RenderTexture not bound, see #24
- [ ] Controllers don't respond — verify Quest interaction profile enabled in OpenXR settings (see #23)
- [ ] Performance drops below 72 Hz — reduce RenderTexture resolution in #24, or check the Library cache key for fresh builds

### Distribution placeholder
- [ ] Section "Future: App Lab Submission" — bullet stub list with: app entitlement registration, content rating, store listing assets, privacy policy URL. **Do not write the full process yet** — wait until production decision.

## Tests to Pass
This is a docs issue. No EditMode tests. Acceptance is:
- [ ] Another team member (or you a week later) can read the doc and successfully sideload a build to a Quest 3 without external help
- [ ] The QA checklist catches at least one real regression during validation of #23–#27 PRs

## Dependencies
- Issue #23 (Android Build Target) — prerequisites section references its setup
- Issue #24 (XR Rig) — QA checklist references the theater scene
- Issue #25 (Quest Controller Input) — QA checklist references the button mapping
- Issue #27 (Quest Build CI) — deploy section references the workflow artifact location

## Notes for Implementer

### Tone
Concise, action-first. Reader is either an engineer doing a fresh deploy or a designer running QA. Avoid theoretical explanations.

### Keep button-mapping section synced
The QA checklist embeds the controller-to-action map (Right A = Jump, etc.). If #25's mapping changes, this doc must update. Add a one-line comment at the top: *"Button mapping mirrored from `Assets/_Project/CorgiCommando.inputactions` (Quest scheme). If they diverge, the .inputactions file is the source of truth."*

### Out of scope
- Quest Store policy guide (App Lab submission is a separate effort, weeks of work)
- Production signing / release builds — only dev keystore covered here
- Multi-language docs
- Video walkthrough
