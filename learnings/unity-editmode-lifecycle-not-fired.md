# Unity EditMode Tests Do Not Auto-Fire MonoBehaviour Lifecycle Callbacks

**Date:** 2026-05-26
**Context:** Three Copilot-authored fix PRs (#67, #74) targeting `HUDController_Awake_CreatesVisualElements` shipped diff-clean fixes inside `HUDController.EnsureVisualHierarchy()` based on speculative hypotheses about Canvas-component initialization failures in headless EditMode. Each fix was defensive, well-scoped, and matched documented patterns. None of them fixed the test. The actual root cause was orthogonal to anything inside `EnsureVisualHierarchy`: the method was never being invoked because **Awake() does not fire on `AddComponent<T>()` in EditMode tests**.

## The rule

In Unity 2022.3 with Unity Test Framework 1.4.x:

- **`[UnityTest]` PlayMode tests:** MonoBehaviour `Awake()`, `OnEnable()`, `Start()`, `Update()` all fire normally — these tests enter Play mode.
- **`[Test]` EditMode tests:** lifecycle callbacks **do not fire automatically** on `AddComponent<T>()` for a regular `MonoBehaviour`. The editor is not in Play mode, so Unity does not invoke `Awake`/`Start`/etc.
- **`[UnityTest]` EditMode tests:** the test enumerator can drive editor updates with `yield return null`, but you still don't get automatic `Awake` from `AddComponent` — only `EditorApplication.update` ticks happen between yields.
- **`[ExecuteAlways]` / `[ExecuteInEditMode]` MonoBehaviours:** these *do* receive `Awake` on `AddComponent` in EditMode, because Unity considers them editor-active. Almost never the right tool for production gameplay code.

## How the codebase already handles this

`Assets/_Project/Tests/EditMode/SceneBootstrapTests.cs:32` encodes the established workaround:

```csharp
private static void InvokePrivate(object instance, string methodName)
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    var method = instance.GetType().GetMethod(methodName, Flags);
    Assert.NotNull(method, $"Expected method '{methodName}' on {instance.GetType().Name}.");
    method.Invoke(instance, null);
}
```

Every SceneBootstrap test explicitly invokes the lifecycle method it needs:

```csharp
using var fixture = new SceneBootstrapFixture();
InvokePrivate(fixture.Bootstrap, "Start");   // <-- explicit invocation
```

The reflection-based invocation is the standard pattern for any EditMode test that depends on post-Awake / post-Start state.

## How tests can pass *coincidentally* without Awake firing

The trap that caused three failed fix attempts: most HUD-related tests pass *despite* Awake not firing, because they call public methods that internally rebuild the same state Awake would have set up.

For example, `HUDController_UpdateHealthBar_StoresPerPlayerState`:

```csharp
var go = new GameObject("HUD");
var hud = go.AddComponent<HUDController>();
hud.UpdateHealthBar(0, 65, 100);          // calls EnsureVisualHierarchy() internally
Assert.AreEqual(65, hud.GetCurrentHealth(0));  // passes
```

`UpdateHealthBar` begins with `EnsureVisualHierarchy()`, so the visual tree gets built lazily on the first invocation. The test passes whether or not Awake fired. The `_currentHealth[0] = 0` default (uninitialized) is harmless because `UpdateHealthBar` overwrites it.

The *one* test that fails — `HUDController_Awake_CreatesVisualElements` — is the only one that:

1. Does not call any public method on the component after `AddComponent`, AND
2. Asserts on state that requires Awake to have run.

That's a structural test bug — relying on a callback that EditMode doesn't fire.

## Detection heuristics

Reach for "EditMode lifecycle not fired" as a hypothesis when:

- A `[Test]` (not `[UnityTest]`) creates a MonoBehaviour via `AddComponent<T>()` and asserts on side effects of `Awake`/`OnEnable` without calling any method on the instance.
- Other tests for the same MonoBehaviour pass but they all happen to invoke a method that internally bootstraps the same state.
- Multiple fix attempts targeting the implementation of the lifecycle method itself have all failed — strong signal the method isn't being called at all.

## Why this trap is hard for hypothesis-driven fixes

The natural reading of "test asserts X is true, X depends on `Awake()` running" is "something inside Awake is broken." It takes a step back to ask "is Awake even running?" The blind spot is that the fix-author's mental model of Unity defaults to PlayMode semantics, where `AddComponent` does fire `Awake` synchronously. In EditMode, it doesn't. Three iterations of fix-by-hypothesis on `HUDController` (PR #67, PR #74) burned through the speculative space — defensive null-guards, AddComponent-individually pattern, etc. — before someone (the reviewer) finally asked the right question by grepping `InvokePrivate` and finding the codebase's existing answer.

## Anti-pattern, captured

**"This `[Test]` depends on Awake — let me make Awake more robust."** Wrong direction. The right direction is "this `[Test]` depends on Awake — does Awake actually fire here?" In EditMode, the answer is almost always no, and the fix is in the test (call `InvokePrivate(instance, "Awake")` or call any public method that internally invokes the needed initialization), not in the production code.
