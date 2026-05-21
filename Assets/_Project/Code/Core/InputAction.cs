using System;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Enumeration of all player input actions recognized by the game.
    /// Maps 1:1 to the Unity Input System action map bindings.
    /// </summary>
    public enum InputAction
    {
        None = 0,
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown,
        Punch,
        Kick,
        Jump,
        Special,
        Pause
    }
}
