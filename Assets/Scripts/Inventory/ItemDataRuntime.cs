using UnityEngine;

namespace Game.Inventory
{
    [System.Serializable]
    public class ItemDataRuntime
    {
        public string id;
        public string name;
        public string icon;
        public string description;
        public bool stackable;
        public int maxStack;

        private Sprite cachedIcon;

        public string itemId => id;
        public string itemName => name;
        public string iconPath => icon;

        public Sprite LoadIcon()
        {
            if (cachedIcon != null)
            {
                return cachedIcon;
            }

            if (string.IsNullOrWhiteSpace(icon))
            {
                return null;
            }

            cachedIcon = Resources.Load<Sprite>(icon);
            if (cachedIcon == null)
            {
                Debug.LogWarning("Icon not found in Resources: " + icon);
            }

            return cachedIcon;
        }
    }
}