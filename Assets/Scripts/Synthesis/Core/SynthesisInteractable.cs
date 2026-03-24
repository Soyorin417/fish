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

        private void Start()
        {
            UpdatePrompt();
        }

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
