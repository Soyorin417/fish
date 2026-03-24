using System;
using Game.Inventory;
using Game.Synthesis.Core;
using Game.Synthesis.Data;

namespace Game.Synthesis.Interface
{
    public interface ISynthesisService
    {
        event Action<SynthesisResult> OnSynthesisFinished;

        bool CanSynthesize(InventoryItem itemA, InventoryItem itemB);
        SynthesisRecipeData GetMatchedRecipe(InventoryItem itemA, InventoryItem itemB);
        SynthesisResult TrySynthesize(InventoryItem itemA, InventoryItem itemB);
    }
}
