namespace CorgiCommando.Player
{
    /// <summary>
    /// Player character state machine states.
    /// State machine: Idle → Walk → Attack(1/2/3) → Hit → Knockdown → GetUp → Special → PickupHold.
    /// </summary>
    public enum CorgiState
    {
        Idle,
        Walk,
        Attack1,
        Attack2,
        Attack3,
        Hit,
        Knockdown,
        GetUp,
        Special,
        PickupHold,
        Dead
    }
}
