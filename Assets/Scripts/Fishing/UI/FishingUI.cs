using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text hintText;
    public GameObject hintRoot;

    public GameObject biteBarRoot;
    public RectTransform barArea;

    [Header("Mini Game Objects")]
    public RectTransform fishZone;
    public RectTransform playerBar;

    [Header("Progress UI")]
    public GameObject progressRoot;
    public Image progressFill;

    private void Start()
    {
        HideAll();
    }

    public void ShowHint(string message)
    {
        if (hintRoot != null)
            hintRoot.SetActive(true);

        if (hintText != null)
            hintText.text = message;
    }

    public void HideHint()
    {
        if (hintRoot != null)
            hintRoot.SetActive(false);
    }

    public void ShowMiniGame()
    {
        if (biteBarRoot != null)
            biteBarRoot.SetActive(true);

        if (progressRoot != null)
            progressRoot.SetActive(true);
    }

    public void HideMiniGame()
    {
        if (biteBarRoot != null)
            biteBarRoot.SetActive(false);

        if (progressRoot != null)
            progressRoot.SetActive(false);
    }

    public void SetProgress(float value01)
    {
        if (progressFill != null)
        {
            progressFill.fillAmount = Mathf.Clamp01(value01);
        }
    }

    public void SetFishZonePosition(float normalizedX)
    {
        SetBarElementX(fishZone, normalizedX);
    }

    public void SetPlayerBarPosition(float normalizedX)
    {
        SetBarElementX(playerBar, normalizedX);
    }

    private void SetBarElementX(RectTransform target, float normalizedX)
    {
        if (target == null || barArea == null) return;

        normalizedX = Mathf.Clamp01(normalizedX);

        float areaWidth = barArea.rect.width;
        float x = Mathf.Lerp(-areaWidth * 0.5f, areaWidth * 0.5f, normalizedX);

        target.anchoredPosition = new Vector2(x, target.anchoredPosition.y);
    }

    public void HideAll()
    {
        HideHint();
        HideMiniGame();
    }
}