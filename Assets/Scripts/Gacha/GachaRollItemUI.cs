using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gacha
{
    public class GachaRollItemUI : MonoBehaviour
    {
        [SerializeField] private Image bg;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image rareFrame;
        [SerializeField] private GameObject highlightRoot;

        private RectTransform cachedRectTransform;

        private void Awake()
        {
            ValidateBindings();
        }

        public RectTransform RectTransform
        {
            get
            {
                if (cachedRectTransform == null)
                {
                    cachedRectTransform = GetComponent<RectTransform>();
                }

                return cachedRectTransform;
            }
        }

        public bool ValidateBindings()
        {
            bool valid = true;

            valid &= ResolveImageReference(ref bg, "Bg");
            valid &= ResolveImageReference(ref icon, "Icon");
            valid &= ResolveTextReference(ref nameText, "NameText");
            valid &= ResolveImageReference(ref rareFrame, "RareFrame");
            valid &= ResolveOptionalObjectReference(ref highlightRoot, "HighlightRoot");

            return valid;
        }

        public void SetData(string fishId, string fishName, int rarityLevel, Sprite fishIcon)
        {
            if (!ValidateBindings())
            {
                Debug.LogError("GachaRollItemUI.SetData aborted because bindings are invalid on " + name);
                return;
            }

            icon.sprite = fishIcon;
            icon.enabled = fishIcon != null;

            nameText.text = string.IsNullOrWhiteSpace(fishName) ? fishId : fishName;

            Color rarityColor = GetRarityColor(rarityLevel);

            rareFrame.color = rarityColor;

            if (bg != null)
            {
                Color bgColor = rarityColor;
                bgColor.a = 0.22f;
                bg.color = bgColor;
            }

            SetHighlighted(false);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (!ValidateBindings())
            {
                Debug.LogError("GachaRollItemUI.SetHighlighted aborted because bindings are invalid on " + name);
                return;
            }

            if (highlightRoot != null)
            {
                highlightRoot.SetActive(highlighted);
            }

            if (bg != null)
            {
                bg.transform.localScale = highlighted ? Vector3.one * 1.08f : Vector3.one;
            }
        }

        private bool ResolveImageReference(ref Image target, string objectName)
        {
            if (target != null && target.transform.IsChildOf(transform))
            {
                return true;
            }

            Transform child = FindChildByName(transform, objectName);
            if (child == null)
            {
                Debug.LogError("GachaRollItemUI could not find child Image '" + objectName + "' under " + name);
                target = null;
                return false;
            }

            Image resolved = child.GetComponent<Image>();
            if (resolved == null)
            {
                Debug.LogError("GachaRollItemUI child '" + objectName + "' is missing Image under " + name);
                target = null;
                return false;
            }

            if (target != resolved)
            {
                Debug.LogWarning("GachaRollItemUI auto-corrected Image reference '" + objectName + "' on " + name);
            }

            target = resolved;
            return true;
        }

        private bool ResolveTextReference(ref TMP_Text target, string objectName)
        {
            if (target != null && target.transform.IsChildOf(transform))
            {
                return true;
            }

            Transform child = FindChildByName(transform, objectName);
            if (child == null)
            {
                Debug.LogError("GachaRollItemUI could not find child TMP_Text '" + objectName + "' under " + name);
                target = null;
                return false;
            }

            TMP_Text resolved = child.GetComponent<TMP_Text>();
            if (resolved == null)
            {
                Debug.LogError("GachaRollItemUI child '" + objectName + "' is missing TMP_Text under " + name);
                target = null;
                return false;
            }

            if (target != resolved)
            {
                Debug.LogWarning("GachaRollItemUI auto-corrected TMP_Text reference '" + objectName + "' on " + name);
            }

            target = resolved;
            return true;
        }

        private bool ResolveOptionalObjectReference(ref GameObject target, string objectName)
        {
            if (target != null)
            {
                if (target.transform.IsChildOf(transform))
                {
                    return true;
                }

                Debug.LogError("GachaRollItemUI optional reference '" + objectName + "' points outside clone hierarchy on " + name);
                target = null;
            }

            Transform child = FindChildByName(transform, objectName);
            if (child != null)
            {
                target = child.gameObject;
            }

            return true;
        }

        private static Transform FindChildByName(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == objectName)
                {
                    return child;
                }

                Transform nested = FindChildByName(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static Color GetRarityColor(int rarityLevel)
        {
            switch (rarityLevel)
            {
                case 4:
                    return new Color(1f, 0.55f, 0.18f, 1f);
                case 3:
                    return new Color(0.74f, 0.30f, 1f, 1f);
                case 2:
                    return new Color(0.20f, 0.78f, 1f, 1f);
                default:
                    return new Color(0.55f, 0.85f, 0.55f, 1f);
            }
        }
    }
}
