using UnityEngine;

public class FishingUIBinder : MonoBehaviour
{
    public FishingUI fishingUI;

    private void Awake()
    {
        if (fishingUI == null)
        {
            fishingUI = GetComponentInChildren<FishingUI>(true);
        }
    }
}
