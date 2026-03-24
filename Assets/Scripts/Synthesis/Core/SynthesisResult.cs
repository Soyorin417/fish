using Game.Inventory;
using Game.Synthesis.Data;

namespace Game.Synthesis.Core
{
    [System.Serializable]
    public class SynthesisResult
    {
        public bool success;
        public string message;
        public InventoryItem inputA;
        public InventoryItem inputB;
        public ItemData outputItemData;
        public int outputAmount;
        public SynthesisRecipeData recipe;

        public static SynthesisResult CreateFailure(string message, InventoryItem inputA, InventoryItem inputB, SynthesisRecipeData recipe = null)
        {
            return new SynthesisResult
            {
                success = false,
                message = message,
                inputA = inputA,
                inputB = inputB,
                outputItemData = recipe != null ? recipe.outputItem : null,
                outputAmount = 0,
                recipe = recipe
            };
        }

        public static SynthesisResult CreateSuccess(string message, InventoryItem inputA, InventoryItem inputB, SynthesisRecipeData recipe, int outputAmount)
        {
            return new SynthesisResult
            {
                success = true,
                message = message,
                inputA = inputA,
                inputB = inputB,
                outputItemData = recipe != null ? recipe.outputItem : null,
                outputAmount = outputAmount,
                recipe = recipe
            };
        }
    }
}
