namespace Game.Fishing.Data
{
    public interface IFishingResultHandler
    {
        void HandleFishResult(FishData fishData, int amount = 1);
    }
}