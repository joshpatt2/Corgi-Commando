# 19. Art Pipeline Foundation

## Goal
Stand up the project plumbing required before any 2D art asset can land in the repo and render correctly: folder structure, Git LFS configuration, Pixel-Perfect rendering package, sort layers, and tags. **No art assets are created in this issue** — this is the bed every sprite/anim/atlas sits on top of.

Tech doc explicitly calls for:
> 2D Built-in Render Pipeline. Pixel-perfect package. […] Aseprite → PNG sprite sheets, sliced 32×32 (players) and 32×32–64×64 (enemies/bosses). Source control: Git + Git LFS for assets.

The repo currently has none of this wired.

## Acceptance Criteria

### Folder structure
- [ ] Create `Assets/_Project/Art/` with subfolders: `Characters/`, `Enemies/`, `Environment/`, `UI/`, `VFX/`
- [ ] Add a `Assets/_Project/Art/README.md` documenting expected sprite specs: 32×32 player frames, 32–64px enemy frames, PNG with alpha, no premultiply, point filter (no bilinear)
- [ ] Each subfolder has an empty `.gitkeep` so it's tracked

### Git LFS
- [ ] Add `.gitattributes` at repo root with LFS rules for: `*.png`, `*.psd`, `*.aseprite`, `*.ase`, `*.tga`, `*.tif`, `*.tiff`, `*.fbx`, `*.wav`, `*.mp3`, `*.ogg`
- [ ] Run `git lfs install` documented in `README.md` setup section
- [ ] `.gitattributes` MUST NOT capture `.cs`, `.unity`, `.prefab`, `.asset`, `.meta` — those stay text

### Pixel-Perfect package
- [ ] Add `com.unity.2d.pixel-perfect` (latest 2022.3-compatible version, ~5.0.x) to `Packages/manifest.json`
- [ ] Add `Pixel Perfect Camera` component to `Main Camera` in `Level_Backyard.unity` with: Asset Pixels Per Unit = 32, Reference Resolution = 480×270 (1080p ÷ 4) or 640×360, Crop Frame = None, Pixel Snapping = on, Upscale Render Texture = off
- [ ] Camera orthographic, size auto-managed by Pixel Perfect Camera

### Sort layers
- [ ] Define sort layers in `ProjectSettings/TagManager.asset`: `Background`, `Midground`, `Gameplay`, `Foreground` (in that order)
- [ ] `Gameplay` is the default for all current `SpriteRenderer`s — update `Player.prefab` and `Ground` in `Level_Backyard.unity` to use it explicitly

### Tags
- [ ] Add tags in `ProjectSettings/TagManager.asset`: `Player`, `Enemy`, `EnvWeapon`, `Hitbox`, `Hurtbox`
- [ ] Apply `Player` tag to `Player.prefab` root GameObject

## Tests to Pass
- ArtPipelineSetup_PixelPerfectCameraConfigured (EditMode) — loads scene, asserts Main Camera has `PixelPerfectCamera` component with PPU=32
- ArtPipelineSetup_SortLayersDefined (EditMode) — asserts `SortingLayer.layers` contains the four expected names in correct order
- ArtPipelineSetup_TagsDefined (EditMode) — asserts `UnityEditorInternal.InternalEditorUtility.tags` contains all five expected tags
- ArtPipelineSetup_ArtFoldersExist (EditMode) — asserts `AssetDatabase.IsValidFolder("Assets/_Project/Art/Characters")` etc. for all five subfolders

## Dependencies
None — this is foundational.

## Notes for Implementer

### Why now
We need to extend `CorgiData` / `EnemyData` / `AttackData` with sprite/animator/VFX fields in issue #20, but doing that without sort layers and Pixel-Perfect set up means anything we drop in will render at the wrong scale or sort order. Land this first.

### Things to verify
- `Packages/manifest.json` already pins `com.unity.inputsystem` and others — match the version-pinning style.
- `TagManager.asset` is a YAML file; layer/tag additions go into the `m_TagNames` and `m_SortingLayers` arrays respectively. Unity will re-serialize but be careful to preserve existing entries.
- Pixel Perfect Camera needs the Camera to be orthographic — confirm `Level_Backyard.unity`'s Main Camera is orthographic, change if needed.

### Out of scope
- Actual sprite/animator assets — issues #20, #21, #22
- AI-image-gen pipeline for placeholder sprites — separate workflow doc
- Sprite Atlases (deferred until at least one character has a sprite sheet to pack)
