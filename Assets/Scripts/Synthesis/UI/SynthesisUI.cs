using Game.Inventory;
using Game.Synthesis.Core;
using Game.Synthesis.Data;
using Game.Synthesis.Interface;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Synthesis.UI
{
    public class SynthesisUI : MonoBehaviour
    {
        private enum InputSlot
        {
            None,
            A,
            B
        }

        [SerializeField] private InventoryToggle inventoryToggle;

        [Header("Service")]
        [SerializeField] private MonoBehaviour synthesisServiceProvider;

        [Header("Linked Views")]
        [SerializeField] private InventoryUI inventoryUI;
        [SerializeField] private bool showInventoryWhenOpened = true;
        [SerializeField] private bool hideInventoryWhenClosed = true;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Input A")]
        [SerializeField] private Image inputAIcon;
        [SerializeField] private TMP_Text inputANameText;
        [SerializeField] private Button selectInputAButton;
        [SerializeField] private Button clearInputAButton;

        [Header("Input B")]
        [SerializeField] private Image inputBIcon;
        [SerializeField] private TMP_Text inputBNameText;
        [SerializeField] private Button selectInputBButton;
        [SerializeField] private Button clearInputBButton;

        [Header("Output Preview")]
        [SerializeField] private Image outputIcon;
        [SerializeField] private TMP_Text outputNameText;
        [SerializeField] private TMP_Text recipeDescriptionText;

        [Header("Actions")]
        [SerializeField] private Button synthesizeButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private Image inputAItemIcon;
        [SerializeField] private Image inputBItemIcon;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;

        private ISynthesisService synthesisService;
        private InventoryItem selectedInputA;
        private InventoryItem selectedInputB;
        private InputSlot activeInputSlot = InputSlot.A;
        private bool initialized;
        private bool uiModeRequested;
        private bool openedInventoryForSelf;

        public bool IsVisible => panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            ResolveInventoryUI();
            ResolveInventoryToggle();
            SetVisible(false);
            RefreshView(false);
        }

        private void OnDisable()
        {
            CleanupVisibleState();
        }

        private void OnDestroy()
        {
            CleanupVisibleState();
            UnbindButtons();

            if (synthesisService != null)
            {
                synthesisService.OnSynthesisFinished -= HandleSynthesisFinished;
            }

            if (inventoryUI != null)
            {
                inventoryUI.ClearSelectionCallback();
            }
        }

        public void Show()
        {
            EnsureInitialized();
            ResolveInventoryUI();
            ResolveInventoryToggle();

            Debug.Log(
                "SynthesisUI.Show inventoryUIAssigned=" + (inventoryUI != null) +
                " inventoryToggleAssigned=" + (inventoryToggle != null) +
                " panelRootAssigned=" + (panelRoot != null));

            RequestUIMode();
            OpenInventoryIfNeeded();

            SetVisible(true);
            BindInventorySelection();
            activeInputSlot = selectedInputA == null ? InputSlot.A : InputSlot.B;
            SetStatus("Select two fish to synthesize.");
            RefreshView();
        }

        public void Hide()
        {
            CleanupVisibleState();
        }

        public void SelectInputA()
        {
            Debug.Log("Select A");
            activeInputSlot = InputSlot.A;
            SetStatus("Choose fish material A from inventory.");
        }

        public void SelectInputB()
        {
            Debug.Log("Select B");
            activeInputSlot = InputSlot.B;
            SetStatus("Choose fish material B from inventory.");
        }

        public void ClearInputA()
        {
            selectedInputA = null;
            if (activeInputSlot == InputSlot.None)
            {
                activeInputSlot = InputSlot.A;
            }

            RefreshView();
        }

        public void ClearInputB()
        {
            selectedInputB = null;
            if (activeInputSlot == InputSlot.None)
            {
                activeInputSlot = InputSlot.B;
            }

            RefreshView();
        }

        public void TrySynthesize()
        {
            Debug.Log("TrySynthesize");

            if (synthesisService == null)
            {
                SetStatus("No synthesis service found.");
                return;
            }

            string fishId1 = GetSelectedFishId(selectedInputA);
            string fishId2 = GetSelectedFishId(selectedInputB);
            Debug.Log("fishId1=" + fishId1 + ", fishId2=" + fishId2);

            SynthesisResult result = synthesisService.TrySynthesize(fishId1, fishId2);
            Debug.Log(JsonUtility.ToJson(result));

            if (result == null)
            {
                SetStatus("Synthesis returned no result.");
                return;
            }

            SetStatus("Synthesis success!");
            Debug.Log("Synthesis success");

            // 这里继续处理成功后的逻辑
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            ResolveSynthesisService();
            BindButtons();
        }

        private void ResolveSynthesisService()
        {
            if (synthesisService != null)
            {
                return;
            }

            synthesisService = synthesisServiceProvider as ISynthesisService;
            if (synthesisService == null && synthesisServiceProvider != null)
            {
                Debug.LogError("synthesisServiceProvider does not implement ISynthesisService.");
            }

            if (synthesisService == null)
            {
                foreach (MonoBehaviour behaviour in FindObjectsOfType<MonoBehaviour>(true))
                {
                    if (behaviour is ISynthesisService service)
                    {
                        synthesisServiceProvider = behaviour;
                        synthesisService = service;
                        break;
                    }
                }
            }

            if (synthesisService != null)
            {
                synthesisService.OnSynthesisFinished -= HandleSynthesisFinished;
                synthesisService.OnSynthesisFinished += HandleSynthesisFinished;
            }
        }

        private void BindInventorySelection()
        {
            ResolveInventoryUI();
            if (inventoryUI == null)
            {
                Debug.LogError("SynthesisUI.BindInventorySelection failed because inventoryUI is not assigned and could not be resolved.");
                return;
            }

            Debug.Log("SynthesisUI.BindInventorySelection binding HandleInventorySelection to InventoryUI.");
            inventoryUI.SetSelectionCallback(HandleInventorySelection);
            if (showInventoryWhenOpened && inventoryToggle == null)
            {
                inventoryUI.Show();
            }
        }

        private void UnbindInventorySelection()
        {
            if (inventoryUI == null)
            {
                return;
            }

            Debug.Log("SynthesisUI.UnbindInventorySelection clearing InventoryUI selection callback.");
            inventoryUI.ClearSelectionCallback();
            if (hideInventoryWhenClosed && inventoryToggle == null)
            {
                inventoryUI.Hide();
            }
        }

        private void HandleInventorySelection(InventoryItem item)
        {
            Debug.Log(
                "SynthesisUI.HandleInventorySelection activeInputSlot=" + activeInputSlot +
                " itemId=" + (item != null ? item.itemId : "(null)") +
                " amount=" + (item != null ? item.amount : 0));

            if (item == null)
            {
                Debug.LogWarning("SynthesisUI.HandleInventorySelection received a null item.");
                return;
            }

            if (activeInputSlot == InputSlot.None)
            {
                activeInputSlot = selectedInputA == null ? InputSlot.A : InputSlot.B;
            }

            if (activeInputSlot == InputSlot.A)
            {
                selectedInputA = item;
                activeInputSlot = selectedInputB == null ? InputSlot.B : InputSlot.None;
            }
            else
            {
                selectedInputB = item;
                activeInputSlot = selectedInputA == null ? InputSlot.A : InputSlot.None;
            }

            RefreshView();
        }

        private void HandleSynthesisFinished(SynthesisResult result)
        {
            if (result == null)
            {
                return;
            }

            SetStatus(result.message);

            if (result.success)
            {
                selectedInputA = null;
                selectedInputB = null;
                activeInputSlot = InputSlot.A;
            }

            RefreshView(false);
        }

        private void RefreshView(bool updateStatus = true)
        {
            UpdateInputView(selectedInputA, inputAIcon, inputANameText, "Input A: Empty");
            UpdateInputView(selectedInputB, inputBIcon, inputBNameText, "Input B: Empty");

            string fishId1 = GetSelectedFishId(selectedInputA);
            string fishId2 = GetSelectedFishId(selectedInputB);

            SynthesisRecipeData recipe = synthesisService != null
                ? synthesisService.GetMatchedRecipe(fishId1, fishId2)
                : null;
            int recipeCount = synthesisService != null
                ? synthesisService.GetMatchedRecipeCount(fishId1, fishId2)
                : 0;

            UpdateOutputPreview(recipe, recipeCount);

            bool canSynthesize = synthesisService != null && synthesisService.CanSynthesize(fishId1, fishId2);
            if (synthesizeButton != null)
            {
                synthesizeButton.interactable = canSynthesize;
            }

            if (!updateStatus)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(fishId1) || string.IsNullOrWhiteSpace(fishId2))
            {
                SetStatus("Select two fish to synthesize.");
                return;
            }

            if (recipe == null)
            {
                SetStatus("No matching fusion recipe was found.");
                return;
            }

            SetStatus(canSynthesize
                ? "Fusion recipe matched. Ready to synthesize."
                : "Recipe matched, but materials are insufficient.");
        }

        private void UpdateInputView(InventoryItem item, Image iconImage, TMP_Text nameText, string emptyText)
        {
            ItemDataRuntime itemData = GetItemDataRuntime(item);

            if (iconImage != null)
            {
                Sprite icon = itemData != null ? itemData.LoadIcon() : null;
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (nameText != null)
            {
                if (item == null)
                {
                    nameText.text = emptyText;
                }
                else
                {
                    string displayName = itemData != null ? itemData.itemName : item.itemId;
                    nameText.text = displayName + " x" + item.amount;
                }
            }
        }

        private void UpdateOutputPreview(SynthesisRecipeData recipe, int recipeCount)
        {
            ItemDataRuntime outputItem = null;
            bool hasMultipleResults = recipeCount > 1;
            if (!hasMultipleResults && recipe != null && !string.IsNullOrWhiteSpace(recipe.resultFishId))
            {
                outputItem = ItemDatabaseRuntime.FindById(recipe.resultFishId);
            }

            if (outputIcon != null)
            {
                Sprite icon = outputItem != null ? outputItem.LoadIcon() : null;
                outputIcon.sprite = icon;
                outputIcon.enabled = icon != null;
            }

            if (outputNameText != null)
            {
                if (recipe == null)
                {
                    outputNameText.text = "No Output";
                }
                else if (hasMultipleResults)
                {
                    outputNameText.text = "Random Result";
                }
                else
                {
                    outputNameText.text = outputItem != null ? outputItem.itemName : recipe.resultFishId;
                }
            }

            if (recipeDescriptionText != null)
            {
                if (recipe == null)
                {
                    recipeDescriptionText.text = string.Empty;
                }
                else if (hasMultipleResults)
                {
                    recipeDescriptionText.text =
                        "Route " + recipe.routeId +
                        " has " + recipeCount +
                        " possible results. One will be chosen randomly.";
                }
                else
                {
                    recipeDescriptionText.text = "Recipe " + recipe.recipeId + " / Route " + recipe.routeId;
                }
            }
        }

        private static ItemDataRuntime GetItemDataRuntime(InventoryItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemId))
            {
                return null;
            }

            return ItemDatabaseRuntime.FindById(item.itemId);
        }

        private static string GetSelectedFishId(InventoryItem item)
        {
            return item != null ? item.itemId : null;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }

        private void ResolveInventoryToggle()
        {
            if (inventoryToggle == null)
            {
                inventoryToggle = FindObjectOfType<InventoryToggle>(true);
            }
        }

        private void ResolveInventoryUI()
        {
            if (inventoryUI == null)
            {
                inventoryUI = FindObjectOfType<InventoryUI>(true);
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

        private void OpenInventoryIfNeeded()
        {
            openedInventoryForSelf = false;
            if (!showInventoryWhenOpened)
            {
                return;
            }

            if (inventoryToggle != null)
            {
                if (!inventoryToggle.IsOpen())
                {
                    inventoryToggle.OpenInventory();
                    openedInventoryForSelf = true;
                }

                return;
            }

            if (inventoryUI != null)
            {
                inventoryUI.Show();
                openedInventoryForSelf = true;
            }
        }

        private void CloseInventoryIfNeeded()
        {
            if (!openedInventoryForSelf)
            {
                return;
            }

            if (inventoryToggle != null)
            {
                if (hideInventoryWhenClosed)
                {
                    inventoryToggle.CloseInventory();
                }
            }
            else if (inventoryUI != null && hideInventoryWhenClosed)
            {
                inventoryUI.Hide();
            }

            openedInventoryForSelf = false;
        }

        private void CleanupVisibleState()
        {
            UnbindInventorySelection();
            selectedInputA = null;
            selectedInputB = null;
            activeInputSlot = InputSlot.A;
            SetVisible(false);
            RefreshView(false);
            CloseInventoryIfNeeded();
            ReleaseUIMode();
        }

        private void BindButtons()
        {
            if (selectInputAButton != null)
            {
                selectInputAButton.onClick.AddListener(SelectInputA);
            }

            if (clearInputAButton != null)
            {
                clearInputAButton.onClick.AddListener(ClearInputA);
            }

            if (selectInputBButton != null)
            {
                selectInputBButton.onClick.AddListener(SelectInputB);
            }

            if (clearInputBButton != null)
            {
                clearInputBButton.onClick.AddListener(ClearInputB);
            }

            if (synthesizeButton != null)
            {
                synthesizeButton.onClick.AddListener(TrySynthesize);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
        }

        private void UnbindButtons()
        {
            if (selectInputAButton != null)
            {
                selectInputAButton.onClick.RemoveListener(SelectInputA);
            }

            if (clearInputAButton != null)
            {
                clearInputAButton.onClick.RemoveListener(ClearInputA);
            }

            if (selectInputBButton != null)
            {
                selectInputBButton.onClick.RemoveListener(SelectInputB);
            }

            if (clearInputBButton != null)
            {
                clearInputBButton.onClick.RemoveListener(ClearInputB);
            }

            if (synthesizeButton != null)
            {
                synthesizeButton.onClick.RemoveListener(TrySynthesize);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }
        }
    }
}
