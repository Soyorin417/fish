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

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;

        private ISynthesisService synthesisService;
        private InventoryItem selectedInputA;
        private InventoryItem selectedInputB;
        private InputSlot activeInputSlot = InputSlot.A;

        public bool IsVisible => panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;

        private void Start()
        {
            ResolveSynthesisService();
            BindButtons();
            SetVisible(false);
            RefreshView(false);
        }

        private void OnDestroy()
        {
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
            ResolveSynthesisService();
            SetVisible(true);
            BindInventorySelection();
            activeInputSlot = selectedInputA == null ? InputSlot.A : InputSlot.B;
            SetStatus("Select two fish to synthesize.");
            RefreshView();
        }

        public void Hide()
        {
            UnbindInventorySelection();
            selectedInputA = null;
            selectedInputB = null;
            activeInputSlot = InputSlot.A;
            SetVisible(false);
            RefreshView(false);
        }

        public void SelectInputA()
        {
            activeInputSlot = InputSlot.A;
            SetStatus("Choose material A from inventory.");
        }

        public void SelectInputB()
        {
            activeInputSlot = InputSlot.B;
            SetStatus("Choose material B from inventory.");
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
            if (synthesisService == null)
            {
                SetStatus("No synthesis service found.");
                return;
            }

            synthesisService.TrySynthesize(selectedInputA, selectedInputB);
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
            if (inventoryUI == null)
            {
                return;
            }

            inventoryUI.SetSelectionCallback(HandleInventorySelection);
            if (showInventoryWhenOpened)
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

            inventoryUI.ClearSelectionCallback();
            if (hideInventoryWhenClosed)
            {
                inventoryUI.Hide();
            }
        }

        private void HandleInventorySelection(InventoryItem item)
        {
            if (item == null)
            {
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

            SynthesisRecipeData recipe = synthesisService != null
                ? synthesisService.GetMatchedRecipe(selectedInputA, selectedInputB)
                : null;

            UpdateOutputPreview(recipe);

            bool canSynthesize = synthesisService != null && synthesisService.CanSynthesize(selectedInputA, selectedInputB);
            if (synthesizeButton != null)
            {
                synthesizeButton.interactable = canSynthesize;
            }

            if (!updateStatus)
            {
                return;
            }

            if (recipe == null)
            {
                if (selectedInputA != null && selectedInputB != null)
                {
                    SetStatus("No fixed recipe matches the current pair.");
                }

                return;
            }

            SetStatus(canSynthesize
                ? "Recipe matched. Ready to synthesize."
                : "Recipe matched, but the inventory does not have enough materials.");
        }

        private void UpdateInputView(InventoryItem item, Image iconImage, TMP_Text nameText, string emptyText)
        {
            if (iconImage != null)
            {
                iconImage.sprite = item != null && item.itemData != null ? item.itemData.icon : null;
                iconImage.enabled = iconImage.sprite != null;
            }

            if (nameText != null)
            {
                if (item == null || item.itemData == null)
                {
                    nameText.text = emptyText;
                }
                else
                {
                    nameText.text = item.itemData.itemName + " x" + item.amount;
                }
            }
        }

        private void UpdateOutputPreview(SynthesisRecipeData recipe)
        {
            ItemData outputItem = recipe != null ? recipe.outputItem : null;

            if (outputIcon != null)
            {
                outputIcon.sprite = outputItem != null ? outputItem.icon : null;
                outputIcon.enabled = outputIcon.sprite != null;
            }

            if (outputNameText != null)
            {
                outputNameText.text = outputItem != null ? outputItem.itemName : "No Output";
            }

            if (recipeDescriptionText != null)
            {
                recipeDescriptionText.text = recipe != null ? recipe.description : string.Empty;
            }
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
