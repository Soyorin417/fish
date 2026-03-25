using System;
using Game.Synthesis.Core;
using Game.Synthesis.Data;

namespace Game.Synthesis.Interface
{
    public interface ISynthesisService
    {
        event Action<SynthesisResult> OnSynthesisFinished;

        bool CanSynthesize(string fishId1, string fishId2);
        SynthesisResult TrySynthesize(string fishId1, string fishId2);
        string GetResultFishId(string fishId1, string fishId2);
        int GetMatchedRecipeCount(string fishId1, string fishId2);
        SynthesisRecipeData GetMatchedRecipe(string fishId1, string fishId2);
    }
}
