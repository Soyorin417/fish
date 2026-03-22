namespace Game.Fishing.Core
{
    public enum FishingState
    {
        None,
        Ready,
        WaitingForBite,
        PlayingMiniGame,
        Success,
        Failed,
        Cooldown
    }
}