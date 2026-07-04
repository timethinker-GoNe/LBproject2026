using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FarmingEngine
{

    /// <summary>
    /// Main Inventory bar that list all items in your inventory
    /// </summary>

    public class InventoryPanel : ItemSlotPanel
    {
        private const string QuickbarLayoutKey = "quickbar";
        private const string BackgroundFrameName = "BackgroundFrame";
        private const string SlotContainerName = "SlotContainer";
        private const string SlotFrameName = "RuntimeSlotFrame";

        private static List<InventoryPanel> panel_list = new List<InventoryPanel>();

        protected override void Awake()
        {
            EnsureQuickbarStructure();
            base.Awake();
            panel_list.Add(this);
            unfocus_when_out = true;

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].onPressKey += OnPressShortcut;
            }

            Hide(true);
        }

        private void EnsureQuickbarStructure()
        {
            if (layoutKey != QuickbarLayoutKey || slots == null || slots.Length == 0)
                return;

            RectTransform root = transform as RectTransform;
            if (root == null)
                return;

            RectTransform background = GetOrCreateChildRect(BackgroundFrameName);
            StretchToParent(background);
            background.SetAsFirstSibling();

            Image backgroundImage = background.GetComponent<Image>();
            if (backgroundImage == null)
                backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.raycastTarget = false;

            RectTransform slotContainer = GetOrCreateChildRect(SlotContainerName);
            StretchToParent(slotContainer);
            slotContainer.SetAsLastSibling();

            foreach (UISlot slot in slots)
            {
                if (slot == null)
                    continue;

                if (slot.transform.parent != slotContainer)
                    slot.transform.SetParent(slotContainer, false);

                NormalizeQuickbarSlotVisuals(slot);
            }

            ApplyQuickbarSprites(backgroundImage);
        }

        private RectTransform GetOrCreateChildRect(string childName)
        {
            Transform child = transform.Find(childName);
            if (child != null)
            {
                RectTransform childRect = child as RectTransform;
                if (childRect != null)
                    return childRect;
            }

            GameObject go = new GameObject(childName, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            return rt;
        }

        private static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private void ApplyQuickbarSprites(Image backgroundImage)
        {
            string bgSpritePath, slotSpritePath;
            if (!UILayoutConfig.TryGetPanelSprites(QuickbarLayoutKey, out bgSpritePath, out slotSpritePath))
                return;

            Sprite bgSprite = LoadStreamingSprite(bgSpritePath);
            if (bgSprite != null)
            {
                backgroundImage.sprite = bgSprite;
                backgroundImage.type = Image.Type.Simple;
                backgroundImage.color = Color.white;
            }

            Sprite slotSprite = LoadStreamingSprite(slotSpritePath);
            if (slotSprite == null)
                return;

            foreach (UISlot slot in slots)
            {
                Image slotImage = FindSlotBackgroundImage(slot);
                if (slotImage == null)
                    continue;

                slotImage.sprite = slotSprite;
                slotImage.type = Image.Type.Simple;
                slotImage.color = Color.white;
            }
        }

        private static Image FindSlotBackgroundImage(UISlot slot)
        {
            if (slot == null)
                return null;

            Image frame = FindChildImage(slot.transform, SlotFrameName);
            if (frame != null)
                return frame;

            Image back = FindChildImage(slot.transform, "Back");
            if (back != null)
                return back;

            Image bg = FindChildImage(slot.transform, "BG");
            if (bg != null)
                return bg;

            Button button = slot.GetComponent<Button>();
            if (button != null && button.targetGraphic is Image buttonImage)
                return buttonImage;

            return null;
        }

        private static Image FindChildImage(Transform root, string childName)
        {
            foreach (Transform child in root)
            {
                if (child.name == childName)
                {
                    Image image = child.GetComponent<Image>();
                    if (image != null)
                        return image;
                }

                Image nested = FindChildImage(child, childName);
                if (nested != null)
                    return nested;
            }
            return null;
        }

        private static void NormalizeQuickbarSlotVisuals(UISlot slot)
        {
            Image frame = GetOrCreateSlotChildImage(slot.transform, SlotFrameName);
            StretchToParent(frame.transform as RectTransform);
            frame.raycastTarget = false;
            frame.transform.SetAsFirstSibling();

            SetChildImagesEnabled(slot.transform, "Back", false);
            SetChildImagesEnabled(slot.transform, "BG", false);
            CenterChildImage(slot.transform, "Icon", 0.68f);
        }

        private static Image GetOrCreateSlotChildImage(Transform root, string childName)
        {
            Transform child = root.Find(childName);
            if (child != null)
            {
                Image image = child.GetComponent<Image>();
                if (image != null)
                    return image;
            }

            GameObject go = new GameObject(childName, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(root, false);
            return go.GetComponent<Image>();
        }

        private static void SetChildImagesEnabled(Transform root, string childName, bool enabled)
        {
            foreach (Transform child in root)
            {
                if (child.name == childName)
                {
                    Image image = child.GetComponent<Image>();
                    if (image != null)
                        image.enabled = enabled;
                }

                SetChildImagesEnabled(child, childName, enabled);
            }
        }

        private static void CenterChildImage(Transform root, string childName, float sizeRatio)
        {
            Image image = FindChildImage(root, childName);
            if (image == null)
                return;

            RectTransform rt = image.transform as RectTransform;
            if (rt == null)
                return;

            rt.SetParent(root, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one * Mathf.Clamp01(sizeRatio);
            image.raycastTarget = false;
            image.preserveAspect = true;
        }

        private static Sprite LoadStreamingSprite(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;

            string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            string filePath = Path.Combine(Application.streamingAssetsPath, normalized);
            if (!File.Exists(filePath) && !Path.HasExtension(filePath))
                filePath += ".png";
            if (!File.Exists(filePath))
                return null;

            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2);
            if (!texture.LoadImage(bytes))
                return null;

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f, 100f);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            panel_list.Remove(this);
        }

        protected override void Start()
        {
            base.Start();
        }

        public override void InitPanel()
        {
            base.InitPanel();

            if (!IsInventorySet())
            {
                PlayerCharacter player = GetPlayer();
                if (player != null)
                {
                    bool has_inventory = PlayerData.Get().HasInventory(player.player_id);
                    if (has_inventory)
                    {
                        SetInventory(InventoryType.Inventory, player.InventoryData.uid, player.InventoryData.size);
                        SetPlayer(player);
                        Show(true);
                    }
                }
            }
        }

        private void OnPressShortcut(UISlot slot)
        {
            CancelSelection();
            PressSlot(slot.index);
        }

        public static InventoryPanel Get(int player_id=0)
        {
            foreach (InventoryPanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer();
                if (player != null && player.player_id == player_id)
                    return panel;
            }
            return null;
        }

        public static new List<InventoryPanel> GetAll()
        {
            return panel_list;
        }
    }

}
