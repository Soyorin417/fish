using System;
using System.Collections;
using System.Collections.Generic;
using Game.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gacha
{
    public class GachaRollController : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private FishProbabilityPool probabilityPool;

        [Header("UI")]
        public RectTransform viewport;
        public RectTransform contentRoot;
        public RectTransform centerIndicator;
        public GameObject itemPrefab;
        public Button startButton;
        public Button closeButton;
        public Image resultIcon;
        public TMP_Text resultText;
        public TMP_Text debugText;

        [Header("Roll Config")]
        public int totalItemCount = 56;
        public int targetIndexMin = 42;
        public int targetIndexMax = 48;
        public float itemWidth = 180f;
        public float itemSpacing = 12f;
        public float rollDuration = 2.6f;
        public float startExtraOffset = 320f;

        public event Action<string> onRollCompleted;

        private readonly List<GachaRollItemUI> itemPool = new List<GachaRollItemUI>();
        private Coroutine rollCoroutine;
        private bool isRolling;
        private Vector2 contentBaseAnchoredPosition;
        private bool hasContentBaseAnchoredPosition;
        [SerializeField] private InventoryToggle inventoryToggle;
        private bool uiModeRequested;
        private bool warnedMissingInventoryToggle;

        private void Awake()
        {
            ResolveProbabilityPool();
            ResolveUiReferences();
            ResolveInventoryToggle();
            CacheContentBaseAnchoredPosition();

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleStartButtonClicked);
                startButton.onClick.AddListener(HandleStartButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
                closeButton.onClick.AddListener(HandleCloseButtonClicked);
            }

            ClearResult();
        }

        private void OnEnable()
        {
            ResolveInventoryToggle();
            RequestUIMode();
        }

        private void OnDisable()
        {
            StopCurrentRoll();
            ReleaseUIMode();
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleStartButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
            }

            ClearItemPool();
            ReleaseUIMode();
        }

        public bool PlayRandomRoll()
        {
            ResolveProbabilityPool();
            if (probabilityPool == null)
            {
                Debug.LogError("GachaRollController.PlayRandomRoll failed because FishProbabilityPool is not assigned.");
                return false;
            }

            string resultFishId = probabilityPool.RollFishId();
            if (string.IsNullOrWhiteSpace(resultFishId))
            {
                Debug.LogError("GachaRollController.PlayRandomRoll failed because RollFishId returned null.");
                return false;
            }

            return PlayRoll(resultFishId);
        }

        public bool PlayRoll(string resultFishId)
        {
            if (string.IsNullOrWhiteSpace(resultFishId))
            {
                Debug.LogError("GachaRollController.PlayRoll received an empty resultFishId.");
                return false;
            }

            if (resultFishId == "fish_025")
            {
                Debug.LogError("GachaRollController.PlayRoll rejected fish_025 because ticket fish can not be a gacha result.");
                return false;
            }

            ResolveProbabilityPool();
            if (probabilityPool == null || !probabilityPool.EnsureLoaded())
            {
                Debug.LogError("GachaRollController.PlayRoll failed because FishProbabilityPool is empty.");
                return false;
            }

            if (!ResolveUiReferences())
            {
                Debug.LogError("GachaRollController.PlayRoll failed because required UI references are invalid.");
                return false;
            }

            if (!ValidateItemPrefab())
            {
                return false;
            }

            ResolveInventoryToggle();
            RequestUIMode();

            StopCurrentRoll();

            gameObject.SetActive(true);
            ClearResult();
            rollCoroutine = StartCoroutine(PlayRollRoutine(resultFishId));
            return true;
        }

        private IEnumerator PlayRollRoutine(string resultFishId)
        {
            isRolling = true;
            SetStartButtonInteractable(false);
            ClearItemPool();
            CacheContentBaseAnchoredPosition();

            int safeItemCount = Mathf.Clamp(totalItemCount, 50, 60);
            int safeMin = Mathf.Clamp(targetIndexMin, 0, safeItemCount - 1);
            int safeMax = Mathf.Clamp(targetIndexMax, safeMin, safeItemCount - 1);
            int targetIndex = UnityEngine.Random.Range(safeMin, safeMax + 1);
            float slotStep = itemWidth + itemSpacing;

            for (int i = 0; i < safeItemCount; i++)
            {
                GameObject itemObject = Instantiate(itemPrefab, contentRoot, false);
                if (itemObject == null)
                {
                    FailCurrentRoll("GachaRollController failed to instantiate itemPrefab.");
                    yield break;
                }

                GachaRollItemUI itemUI = itemObject.GetComponent<GachaRollItemUI>();
                if (itemUI == null)
                {
                    Destroy(itemObject);
                    FailCurrentRoll("GachaRollController instantiated an item without GachaRollItemUI.");
                    yield break;
                }

                if (!itemUI.transform.IsChildOf(contentRoot))
                {
                    Destroy(itemObject);
                    FailCurrentRoll("GachaRollController instantiated an item outside ContentRoot.");
                    yield break;
                }

                if (!itemUI.ValidateBindings())
                {
                    Destroy(itemObject);
                    FailCurrentRoll("GachaRollController instantiated an item with invalid internal bindings.");
                    yield break;
                }

                itemPool.Add(itemUI);

                string fishId = i == targetIndex ? resultFishId : GetRollCandidateFishId(resultFishId);
                FishProbability probability = probabilityPool.FindById(fishId);
                string fishName = probabilityPool.GetFishName(fishId);
                int rarityLevel = probability != null ? probability.rarityLevel : probabilityPool.GetRarityLevel(fishId);
                Sprite iconSprite = LoadFishIcon(fishId);

                RectTransform itemRect = itemUI.RectTransform;
                Vector2 anchoredPosition = itemRect.anchoredPosition;
                anchoredPosition.x += i * slotStep;
                itemRect.anchoredPosition = anchoredPosition;

                itemUI.SetData(fishId, fishName, rarityLevel, iconSprite);
                itemUI.SetHighlighted(false);
            }

            if (itemPool.Count <= targetIndex)
            {
                FailCurrentRoll("GachaRollController did not create enough roll items.");
                yield break;
            }

            float indicatorX = GetIndicatorLocalXInViewport();
            float targetLocalX = itemPool[targetIndex].RectTransform.anchoredPosition.x;
            float endX = indicatorX - targetLocalX;
            float startX = endX + Mathf.Max(startExtraOffset, slotStep * 5f);

            contentRoot.anchoredPosition = new Vector2(startX, contentBaseAnchoredPosition.y);
            SetDebugText("Rolling...");

            float elapsed = 0f;
            while (elapsed < rollDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(rollDuration, 0.01f));
                float eased = EaseOutQuint(t);
                float currentX = Mathf.Lerp(startX, endX, eased);
                contentRoot.anchoredPosition = new Vector2(currentX, contentBaseAnchoredPosition.y);
                yield return null;
            }

            contentRoot.anchoredPosition = new Vector2(endX, contentBaseAnchoredPosition.y);
            itemPool[targetIndex].SetHighlighted(true);
            ShowResult(resultFishId);

            SetDebugText("抽卡完成: " + resultFishId);
            Debug.Log("抽卡完成: " + resultFishId);

            isRolling = false;
            rollCoroutine = null;
            SetStartButtonInteractable(true);
            onRollCompleted?.Invoke(resultFishId);
        }

        private static float EaseOutQuint(float t)
        {
            float inv = 1f - t;
            return 1f - inv * inv * inv * inv * inv;
        }

        private void StopCurrentRoll()
        {
            if (rollCoroutine != null)
            {
                StopCoroutine(rollCoroutine);
                rollCoroutine = null;
            }

            isRolling = false;
            SetStartButtonInteractable(true);
            ClearItemPool();
        }

        private void FailCurrentRoll(string message)
        {
            Debug.LogError(message);
            SetDebugText(message);

            isRolling = false;
            rollCoroutine = null;
            SetStartButtonInteractable(true);
            ClearItemPool();
        }

        private void ClearItemPool()
        {
            for (int i = 0; i < itemPool.Count; i++)
            {
                GachaRollItemUI itemUI = itemPool[i];
                if (itemUI != null)
                {
                    Destroy(itemUI.gameObject);
                }
            }

            itemPool.Clear();
        }

        private bool ResolveUiReferences()
        {
            bool valid = true;

            valid &= ResolveRectTransformReference(ref viewport, "Viewport");
            valid &= ResolveRectTransformReference(ref contentRoot, "ContentRoot");
            valid &= ResolveRectTransformReference(ref centerIndicator, "CenterIndicator");
            valid &= ResolveButtonReference(ref startButton, "StartButton");
            valid &= ResolveButtonReference(ref closeButton, "CloseButton", false);
            valid &= ResolveImageReference(ref resultIcon, "ResultIcon");
            valid &= ResolveResultTextReference();
            valid &= ResolveTextReference(ref debugText, "DebugText", true);

            return valid;
        }

        private bool ResolveRectTransformReference(ref RectTransform target, string objectName)
        {
            if (target != null && target.transform.IsChildOf(transform) && target.name == objectName)
            {
                return true;
            }

            Transform resolved = FindDescendantByName(transform, objectName);
            if (resolved == null)
            {
                Debug.LogError("GachaRollController could not resolve RectTransform: " + objectName);
                target = null;
                return false;
            }

            RectTransform rectTransform = resolved as RectTransform;
            if (rectTransform == null)
            {
                Debug.LogError("GachaRollController found object but it is not a RectTransform: " + objectName);
                target = null;
                return false;
            }

            if (target != rectTransform)
            {
                Debug.LogWarning("GachaRollController auto-corrected RectTransform reference: " + objectName);
            }

            target = rectTransform;
            return true;
        }

        private bool ResolveButtonReference(ref Button target, string objectName, bool required = true)
        {
            if (target != null && target.transform.IsChildOf(transform) && target.gameObject.name == objectName)
            {
                return true;
            }

            Transform resolved = FindDescendantByName(transform, objectName);
            if (resolved == null)
            {
                if (required)
                {
                    Debug.LogError("GachaRollController could not resolve Button: " + objectName);
                }
                else
                {
                    Debug.LogWarning("GachaRollController optional Button not found: " + objectName);
                }
                target = null;
                return !required;
            }

            Button button = resolved.GetComponent<Button>();
            if (button == null)
            {
                if (required)
                {
                    Debug.LogError("GachaRollController found object but it has no Button: " + objectName);
                }
                else
                {
                    Debug.LogWarning("GachaRollController optional Button object has no Button component: " + objectName);
                }
                target = null;
                return !required;
            }

            if (target != button)
            {
                Debug.LogWarning("GachaRollController auto-corrected Button reference: " + objectName);
            }

            target = button;
            return true;
        }

        private bool ResolveImageReference(ref Image target, string objectName)
        {
            if (target != null && target.transform.IsChildOf(transform) && target.gameObject.name == objectName)
            {
                return true;
            }

            Transform resolved = FindDescendantByName(transform, objectName);
            if (resolved == null)
            {
                Debug.LogError("GachaRollController could not resolve Image: " + objectName);
                target = null;
                return false;
            }

            Image image = resolved.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError("GachaRollController found object but it has no Image: " + objectName);
                target = null;
                return false;
            }

            if (target != image)
            {
                Debug.LogWarning("GachaRollController auto-corrected Image reference: " + objectName);
            }

            target = image;
            return true;
        }

        private bool ResolveTextReference(ref TMP_Text target, string objectName, bool required)
        {
            if (target != null && target.transform.IsChildOf(transform))
            {
                return true;
            }

            Transform resolved = FindDescendantByName(transform, objectName);
            if (resolved == null)
            {
                if (required)
                {
                    Debug.LogError("GachaRollController could not resolve TMP_Text: " + objectName);
                }
                else
                {
                    Debug.LogWarning("GachaRollController optional TMP_Text not found: " + objectName);
                }
                target = null;
                return !required;
            }

            TMP_Text text = resolved.GetComponent<TMP_Text>() ?? resolved.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
            {
                if (required)
                {
                    Debug.LogError("GachaRollController found object but it has no TMP_Text: " + objectName);
                }
                else
                {
                    Debug.LogWarning("GachaRollController optional TMP_Text container has no TMP_Text: " + objectName);
                }
                target = null;
                return !required;
            }

            if (target != text)
            {
                Debug.LogWarning("GachaRollController auto-corrected TMP_Text reference: " + objectName);
            }

            target = text;
            return true;
        }

        private bool ResolveResultTextReference()
        {
            if (resultText != null && resultText.transform.IsChildOf(transform) && !IsUnderBlockedButton(resultText.transform))
            {
                return true;
            }

            Transform resultTextRoot = FindDescendantByName(transform, "ResultText");
            if (resultTextRoot != null)
            {
                TMP_Text textFromRoot = resultTextRoot.GetComponent<TMP_Text>() ?? resultTextRoot.GetComponentInChildren<TMP_Text>(true);
                if (textFromRoot != null && !IsUnderBlockedButton(textFromRoot.transform))
                {
                    if (resultText != textFromRoot)
                    {
                        Debug.LogWarning("GachaRollController auto-corrected TMP_Text reference: ResultText");
                    }

                    resultText = textFromRoot;
                    return true;
                }
            }

            Transform resultArea = FindDescendantByName(transform, "ResultArea");
            if (resultArea != null)
            {
                TMP_Text[] texts = resultArea.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    TMP_Text candidate = texts[i];
                    if (candidate == null || IsUnderBlockedButton(candidate.transform))
                    {
                        continue;
                    }

                    Debug.LogWarning("GachaRollController auto-corrected TMP_Text reference using ResultArea fallback.");
                    resultText = candidate;
                    return true;
                }
            }

            Debug.LogWarning("GachaRollController optional TMP_Text not found: ResultText");
            resultText = null;
            return true;
        }

        private bool IsUnderBlockedButton(Transform target)
        {
            return IsUnderNamedAncestor(target, "StartButton") || IsUnderNamedAncestor(target, "CloseButton");
        }

        private static bool IsUnderNamedAncestor(Transform target, string ancestorName)
        {
            Transform current = target;
            while (current != null)
            {
                if (current.name == ancestorName)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private bool ValidateItemPrefab()
        {
            if (itemPrefab == null)
            {
                Debug.LogError("GachaRollController.itemPrefab is null.");
                return false;
            }

            if (itemPrefab.scene.IsValid())
            {
                Debug.LogError("GachaRollController.itemPrefab must be a prefab asset from the Project window, not a scene instance.");
                return false;
            }

            GachaRollItemUI itemUI = itemPrefab.GetComponent<GachaRollItemUI>();
            if (itemUI == null)
            {
                Debug.LogError("GachaRollController.itemPrefab is missing GachaRollItemUI.");
                return false;
            }

            return true;
        }

        private string GetRollCandidateFishId(string fallbackFishId)
        {
            for (int i = 0; i < 8; i++)
            {
                string fishId = probabilityPool.RollFishId();
                if (!string.IsNullOrWhiteSpace(fishId) && fishId != "fish_025")
                {
                    return fishId;
                }
            }

            return fallbackFishId;
        }

        private void ShowResult(string resultFishId)
        {
            Sprite iconSprite = LoadFishIcon(resultFishId);

            if (resultIcon != null)
            {
                resultIcon.sprite = iconSprite;
                resultIcon.enabled = iconSprite != null;
            }

            if (resultText != null)
            {
                string fishName = probabilityPool != null ? probabilityPool.GetFishName(resultFishId) : resultFishId;
                resultText.text = fishName + " (" + resultFishId + ")";
            }
        }

        private void ClearResult()
        {
            if (resultIcon != null)
            {
                resultIcon.sprite = null;
                resultIcon.enabled = false;
            }

            if (resultText != null)
            {
                resultText.text = string.Empty;
            }

            SetDebugText(string.Empty);
        }

        private void HandleStartButtonClicked()
        {
            if (isRolling)
            {
                return;
            }

            PlayRandomRoll();
        }

        private void HandleCloseButtonClicked()
        {
            if (isRolling)
            {
                SetDebugText("Rolling in progress...");
                return;
            }

            ClosePanel();
        }

        private void ClosePanel()
        {
            gameObject.SetActive(false);
        }

        private void SetStartButtonInteractable(bool interactable)
        {
            if (startButton != null)
            {
                startButton.interactable = interactable;
            }

            if (closeButton != null)
            {
                closeButton.interactable = interactable;
            }
        }

        private float GetIndicatorLocalXInViewport()
        {
            if (viewport == null || centerIndicator == null)
            {
                return 0f;
            }

            Vector3 localPoint = viewport.InverseTransformPoint(centerIndicator.position);
            return localPoint.x;
        }

        private Sprite LoadFishIcon(string fishId)
        {
            if (string.IsNullOrWhiteSpace(fishId))
            {
                return null;
            }

            Sprite icon = Resources.Load<Sprite>("Icons/" + fishId);
            if (icon == null)
            {
                Debug.LogWarning("GachaRollController could not find icon at Resources/Icons/" + fishId);
            }

            return icon;
        }

        private void ResolveProbabilityPool()
        {
            if (probabilityPool == null)
            {
                probabilityPool = FindObjectOfType<FishProbabilityPool>(true);
            }
        }

        private void ResolveInventoryToggle()
        {
            if (inventoryToggle == null)
            {
                inventoryToggle = FindObjectOfType<InventoryToggle>(true);
            }

            if (inventoryToggle == null && !warnedMissingInventoryToggle)
            {
                Debug.LogError("GachaRollController could not find InventoryToggle. Cursor/UI mode can not be synchronized.");
                warnedMissingInventoryToggle = true;
            }
        }

        private void RequestUIMode()
        {
            if (inventoryToggle == null || uiModeRequested)
            {
                return;
            }

            inventoryToggle.PushUIMode();
            uiModeRequested = true;
        }

        private void ReleaseUIMode()
        {
            if (inventoryToggle == null || !uiModeRequested)
            {
                return;
            }

            if (inventoryToggle.isActiveAndEnabled)
            {
                inventoryToggle.PopUIMode();
            }

            uiModeRequested = false;
        }

        private void CacheContentBaseAnchoredPosition()
        {
            if (contentRoot == null)
            {
                return;
            }

            if (!hasContentBaseAnchoredPosition)
            {
                contentBaseAnchoredPosition = contentRoot.anchoredPosition;
                hasContentBaseAnchoredPosition = true;
            }
        }

        private void SetDebugText(string message)
        {
            if (debugText != null)
            {
                debugText.text = message;
            }
        }

        private static Transform FindDescendantByName(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendantByName(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static float EaseOutCubic(float t)
        {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }
    }
}
