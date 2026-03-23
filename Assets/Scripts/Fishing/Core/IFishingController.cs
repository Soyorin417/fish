namespace Game.Fishing.Core
{
    public interface IFishingController
    {
        FishingState State { get; }
        bool IsFishing { get; }

        void CancelFishing();
    }
}
