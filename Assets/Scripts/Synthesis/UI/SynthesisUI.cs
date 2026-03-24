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


        /// <summary>
        /// 初始化合成服务、绑定按钮事件，并设置界面的初始显示状态。
        /// </summary>
        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            ResolveInventoryToggle();
            SetVisible(false);
            RefreshView(false);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            Debug.Log("SynthesisUI initialized");

            ResolveSynthesisService();
            BindButtons();
        }

        /// <summary>
        /// 在对象销毁时移除事件绑定并清理背包选择回调。
        /// </summary>
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

        private void OnDisable()
        {
            CleanupVisibleState();
        }



        /// <summary>
        /// 打开合成界面并绑定背包选择逻辑。
        /// </summary>
        public void Show()
        {
            EnsureInitialized();
            ResolveInventoryToggle();

            RequestUIMode();
            OpenInventoryIfNeeded();

            SetVisible(true);
            BindInventorySelection();
            activeInputSlot = selectedInputA == null ? InputSlot.A : InputSlot.B;
            SetStatus("Select two fish to synthesize.");
            RefreshView();
        }

        /// <summary>
        /// 关闭合成界面并清空当前选择状态。
        /// </summary>
        public void Hide()
        {
            CleanupVisibleState();
        }

        /// <summary>
        /// 将当前待选择的输入槽切换为材料 A。
        /// </summary>
        public void SelectInputA()
        {
            activeInputSlot = InputSlot.A;
            SetStatus("Choose material A from inventory.");
        }

        /// <summary>
        /// 将当前待选择的输入槽切换为材料 B。
        /// </summary>
        public void SelectInputB()
        {
            activeInputSlot = InputSlot.B;
            SetStatus("Choose material B from inventory.");
        }

        /// <summary>
        /// 清空材料 A 的当前选择并刷新界面。
        /// </summary>
        public void ClearInputA()
        {
            selectedInputA = null;
            if (activeInputSlot == InputSlot.None)
            {
                activeInputSlot = InputSlot.A;
            }

            RefreshView();
        }

        /// <summary>
        /// 清空材料 B 的当前选择并刷新界面。
        /// </summary>
        public void ClearInputB()
        {
            selectedInputB = null;
            if (activeInputSlot == InputSlot.None)
            {
                activeInputSlot = InputSlot.B;
            }

            RefreshView();
        }

        /// <summary>
        /// 调用合成服务执行当前选中材料的合成操作。
        /// </summary>
        public void TrySynthesize()
        {
            if (synthesisService == null)
            {
                SetStatus("No synthesis service found.");
                return;
            }

            synthesisService.TrySynthesize(selectedInputA, selectedInputB);
        }

        /// <summary>
        /// 解析并获取场景中的合成服务实例，同时绑定合成完成事件。
        /// </summary>
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

        /// <summary>
        /// 绑定背包选择回调，并在需要时显示背包界面。
        /// </summary>
        private void BindInventorySelection()
        {
            if (inventoryUI == null)
            {
                return;
            }

            inventoryUI.SetSelectionCallback(HandleInventorySelection);
            if (showInventoryWhenOpened && inventoryToggle == null)
            {
                inventoryUI.Show();
            }
        }

        /// <summary>
        /// 解绑背包选择回调，并在需要时隐藏背包界面。
        /// </summary>
        private void UnbindInventorySelection()
        {
            if (inventoryUI == null)
            {
                return;
            }

            inventoryUI.ClearSelectionCallback();
            if (hideInventoryWhenClosed && inventoryToggle == null)
            {
                inventoryUI.Hide();
            }
        }

        /// <summary>
        /// 处理玩家从背包中选中的物品，并填入当前激活的输入槽。
        /// </summary>
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

        /// <summary>
        /// 处理合成完成后的结果显示与输入槽重置逻辑。
        /// </summary>
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

        /// <summary>
        /// 刷新输入槽、产物预览、按钮状态和提示文本。
        /// </summary>
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

        /// <summary>
        /// 刷新单个输入槽的图标和名称显示。
        /// </summary>
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

        /// <summary>
        /// 根据当前匹配到的配方刷新合成结果预览区域。
        /// </summary>
        private void UpdateOutputPreview(SynthesisRecipeData recipe)
        {
            ItemDataRuntime outputItem = recipe != null ? recipe.outputItem : null;

            if (outputIcon != null)
            {
                Sprite icon = outputItem != null ? outputItem.LoadIcon() : null;
                outputIcon.sprite = icon;
                outputIcon.enabled = icon != null;
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

        /// <summary>
        /// 根据背包物品获取对应的运行时物品数据。
        /// </summary>
        private ItemDataRuntime GetItemDataRuntime(InventoryItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemId))
            {
                return null;
            }

            return ItemDatabaseRuntime.FindById(item.itemId);
        }

        /// <summary>
        /// 更新界面底部的状态提示文本。
        /// </summary>
        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        /// <summary>
        /// 控制合成界面本体的显示与隐藏。
        /// </summary>
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

        /// <summary>
        /// 绑定所有按钮的点击事件。
        /// </summary>
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

        /// <summary>
        /// 解绑所有按钮的点击事件，防止重复注册。
        /// </summary>
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
