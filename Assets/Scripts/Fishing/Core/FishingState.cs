namespace Game.Fishing.Core
{
    public enum FishingState
    {
        None,
        Idle,
        Ready,
        WaitingForBite,
        PlayingMiniGame,
        Success,
        Failed,
        Result,
        Cooldown
    }
}
