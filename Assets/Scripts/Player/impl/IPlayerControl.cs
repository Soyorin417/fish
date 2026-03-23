namespace Game.Player
{
    public interface IPlayerControl
    {
        void SetMoveEnabled(bool enabled);
        void SetLookEnabled(bool enabled);
        void SetJumpEnabled(bool enabled);
        void PlayFishingAnimation(bool isFishing);
    }
}