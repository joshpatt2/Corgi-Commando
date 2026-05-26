# Milestone 1 Acceptance Checklist

**Source:** Corgi_Commando_Demo_Spec.docx
**Purpose:** Pin down "done" criteria so the loop and the human agree on the target.

A Ralph loop or human reviewer can declare M1 complete iff every checkbox under **Success Criteria** and **Verification** is checked, AND zero checkboxes under **Kill Criteria** are triggered.

---

## The Three Pillars (binary go/no-go)

Everything else is secondary. If these three stand, M1 is a success.

- [ ] **P1. Controls work.** Inputs respond immediately. Mapping is documented and stable. A first-time player understands movement and combat within 30 seconds.
- [ ] **P2. Core mechanics work.** You can attack, take damage, see health bars rise and fall, land combos, build and spend meter, pick up environmental weapons.
- [ ] **P3. Wave progression and the boss work.** Three waves of escalating difficulty culminate in a readable, beatable Whiskerbot fight.

---

## Success Criteria

### Controls
- [ ] Keyboard bindings wired on macOS: WASD/Arrows (move), Space (jump), J (punch), K (kick), L (special), E (pickup/throw), Esc (pause).
- [ ] Gamepad bindings wired on macOS: L-Stick/D-Pad (move), South (jump), West (punch), North (kick), East (special), RB/R1 (pickup), Start (pause).
- [ ] Keyboard and gamepad produce identical in-game behavior (input abstraction layer respected).
- [ ] Input lag imperceptible — under one frame at 60fps.
- [ ] iOS build accepts paired Bluetooth controller with identical behavior to macOS gamepad.
- [ ] Two gamepads can join on macOS for co-op.
- [ ] Second player can drop in mid-game.
- [ ] Second player can drop out mid-game without breaking state.

### Combat & Mechanics
- [ ] Punch, Punch, Kick combo lands correctly with launcher finisher.
- [ ] Player health bar decreases visibly on enemy hits.
- [ ] Enemy health bar decreases visibly on player hits.
- [ ] Knockback applies in the expected direction and feels weighty.
- [ ] Hitstop on contact is present and tuned to 3–6 frames.
- [ ] Screen shake fires on heavy hits and on specials.
- [ ] Special meter fills on hits landed.
- [ ] Special meter triggers Bark Shockwave when full.
- [ ] Environmental weapons can be picked up.
- [ ] Environmental weapons can be swung.
- [ ] Environmental weapons can be thrown.
- [ ] Same-Z-band hit detection: attacks visibly whiff when targets are out of depth band.

### Wave Progression & Boss
- [ ] Wave 1: 2 feral cats spawn. 3rd cat waves in once first two are low HP.
- [ ] Wave 2: 2 cats + 1 raccoon bandit + 1 sprinkler turret spawn.
- [ ] Wave 3: 3 cats + 2 raccoons + 1 turret spawn. Environmental weapons available.
- [ ] Next wave does not start until previous wave is cleared.
- [ ] Aggro slot manager prevents dogpiling: max 2 attackers per player.
- [ ] Boss door opens after Wave 3 cleared.
- [ ] Whiskerbot-9000 enters with cartoon-style banner.
- [ ] Whiskerbot phase transition at 75% HP fires reliably.
- [ ] Whiskerbot phase transition at 35% HP fires reliably.
- [ ] All three boss phases run without crashes.
- [ ] Pilot fight triggers at 0% mech HP.
- [ ] Boss attack patterns are readable — a skilled player can identify and dodge each within 1–2 attempts.
- [ ] Boss is beatable by a competent player.
- [ ] Boss is challenging enough to feel earned.

### Demo Flow End-to-End
- [ ] Spawn in first backyard with Sarge ready.
- [ ] Walk right to arena trigger — camera locks.
- [ ] Wave 1 → Wave 2 → Wave 3 → boss door → boss intro → boss fight runs without intervention.
- [ ] Win path: boss defeated → victory screen.
- [ ] Lose path: party wipe → retry from boss intro.
- [ ] Total runtime spawn→boss-intro: 2–3 minutes.
- [ ] Boss fight runtime: 1–2 minutes.
- [ ] Full demo session: ~4–5 minutes.

---

## Kill Criteria (any one = FAIL, regardless of other checkboxes)

- [ ] **Input lag is perceptible.** Even with everything else working, perceptible lag = M1 fails.
- [ ] **Hit detection is unclear.** Players can't tell if an attack connected.
- [ ] **Boss is impossible to read.** Players die to attacks they couldn't see coming.
- [ ] **Boss is trivial to cheese.** Players can spam one move and win without phase engagement.
- [ ] **iOS build does not run.** Half the demo target missing.
- [ ] **Co-op breaks the camera.** Two players cause the camera to lose them or framing to fail.
- [ ] **Frequent crashes or soft-locks during a 5-minute play session.**

---

## Verification

These are how we prove the checkboxes above. Acceptance requires all of these to be performed and pass:

- [ ] All EditMode + PlayMode unit tests pass on main with zero failures (not "expected failures").
- [ ] Internal solo playtest: dev plays full demo end-to-end at least **5 times** solo without intervention.
- [ ] Internal co-op playtest: at least **3 times** end-to-end with two gamepads.
- [ ] External playtest: at least **3 people who have not seen the game** play it. Watch them. Don't coach.
- [ ] iOS device test: demo runs on a physical iPhone with a Bluetooth controller, end-to-end.
- [ ] Playtest findings documented (becomes the Milestone 2 task list).

---

## Out of Scope (do not block M1 on these)

Anything below is deferred to M2 or later. Do not let it gate M1:

- Real art / animation (solid-color primitives are correct for M1).
- Other playable corgis (Biscuit, Pixel, Duchess locked).
- HQ doghouse / upgrade screens.
- Save system or persistence.
- Title screen polish, character select polish.
- Touch controls on iOS.
- Audio beyond placeholder beeps.
- Levels 2 through 5.

---

## Decision Gate (after playtest)

When the above checklist is complete, make a single explicit call:

- **Go:** Three pillars stand. Playtesters had fun. Fix list is finite. → Proceed to Milestone 2.
- **Pivot:** Pillars stand but feel is off in a fixable way. → 2–4 more weeks on combat tuning before M2.
- **No-go:** One or more pillars failed. → Re-evaluate scope / mechanics / fit. Do not invest in art on a foundation that doesn't work.

---

## How to use this file

- **Ralph loop completion-promise** should reference this file: *"All checkboxes in MILESTONE_1_ACCEPTANCE.md under Success Criteria and Verification are checked, and zero Kill Criteria are triggered."*
- **PR reviewers** should check whether their PR moves any checkbox from unchecked to checked, and call that out in the PR description.
- **Issue authors** should reference the specific checkbox(es) their issue covers.
- **Edit this file** when scope is officially renegotiated — do not let drift happen silently.
