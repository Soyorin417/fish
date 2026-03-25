using Game.Synthesis.Data;

namespace Game.Synthesis.Core
{
    [System.Serializable]
    public class SynthesisResult
    {
        public bool success;
        public string message;
        public string failureReason;
        public string material1Id;
        public string material2Id;
        public string resultFishId;
        public string recipeId;
        public string routeId;

        public static SynthesisResult CreateFailure(
            string failureReason,
            string material1Id,
            string material2Id,
            SynthesisRecipeData recipe = null)
        {
            return new SynthesisResult
            {
                success = false,
                message = failureReason,
                failureReason = failureReason,
                material1Id = material1Id,
                material2Id = material2Id,
                resultFishId = recipe != null ? recipe.resultFishId : null,
                recipeId = recipe != null ? recipe.recipeId : null,
                routeId = recipe != null ? recipe.routeId : null
            };
        }

        public static SynthesisResult CreateSuccess(
            string material1Id,
            string material2Id,
            SynthesisRecipeData recipe)
        {
            string resultFishId = recipe != null ? recipe.resultFishId : null;

            return new SynthesisResult
            {
                success = true,
                message = "Synthesis succeeded: " + resultFishId,
                failureReason = string.Empty,
                material1Id = material1Id,
                material2Id = material2Id,
                resultFishId = resultFishId,
                recipeId = recipe != null ? recipe.recipeId : null,
                routeId = recipe != null ? recipe.routeId : null
            };
        }
    }
}
