using Game.Synthesis.UI;
using TMPro;
using UnityEngine;

namespace Game.Synthesis.Core
{
    public class SynthesisInteractable : MonoBehaviour
    {
        [SerializeField] private SynthesisUI synthesisUI;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private string interactorTag = "Player";

        [Header("Prompt")]
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private string promptMessage = "Press E to open synthesis";

        private int interactorCount;

        /// <summary>
        /// 初始化交互提示的显示状态。
        /// </summary>
        private void Start()
        {
            UpdatePrompt();
        }

        /// <summary>
        /// 持续检测交互按键，并在满足条件时打开合成界面。
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(interactKey))
            {
                Debug.Log($"按下交互键, interactorCount={interactorCount}, synthesisUI={(synthesisUI != null)}, visible={(synthesisUI != null ? synthesisUI.IsVisible : false)}");
            }

            UpdatePrompt();

            if (interactorCount <= 0 || synthesisUI == null || synthesisUI.IsVisible)
            {
                return;
            }

            if (Input.GetKeyDown(interactKey))
            {
                Debug.Log("准备打开合成UI");
                synthesisUI.Show();
                UpdatePrompt();
            }
        }

        /// <summary>
        /// 当交互对象进入触发范围时，记录可交互状态并刷新提示。
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("进入触发器: " + other.name + " tag=" + other.tag);

            if (!IsValidInteractor(other))
            {
                Debug.Log("不是合法交互者");
                return;
            }

            interactorCount++;
            Debug.Log("合法交互者进入, interactorCount=" + interactorCount);
            UpdatePrompt();
        }

        /// <summary>
        /// 当交互对象离开触发范围时，取消可交互状态并刷新提示。
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            Debug.Log("离开触发器: " + other.name + " tag=" + other.tag);

            if (!IsValidInteractor(other))
            {
                return;
            }

            interactorCount = Mathf.Max(0, interactorCount - 1);
            Debug.Log("合法交互者离开, interactorCount=" + interactorCount);
            UpdatePrompt();
        }

        /// <summary>
        /// 判断进入触发器的对象是否为允许交互的目标。
        /// </summary>
        private bool IsValidInteractor(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(interactorTag))
            {
                return true;
            }

            return other.CompareTag(interactorTag);
        }

        /// <summary>
        /// 根据当前交互状态刷新提示文本和提示物体的显示状态。
        /// </summary>
        private void UpdatePrompt()
        {
            bool shouldShow = interactorCount > 0 && synthesisUI != null && !synthesisUI.IsVisible;

            if (promptRoot != null)
            {
                promptRoot.SetActive(shouldShow);
            }

            if (promptText != null)
            {
                promptText.text = promptMessage;
            }
        }
    }
}