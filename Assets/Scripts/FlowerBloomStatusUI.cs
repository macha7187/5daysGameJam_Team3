using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Bloom Rock Puzzle/Flower Bloom Status UI")]
public class FlowerBloomStatusUI : MonoBehaviour
{
    [SerializeField] private BloomPuzzleLevel level;
    [SerializeField] private Vector2 iconSize = new Vector2(58f, 58f);
    [SerializeField] private float horizontalPadding = 22f;
    [SerializeField] private float itemSpacing = 14f;
    [SerializeField] private float fontSize = 54f;
    [SerializeField] private float rowSpacing = 12f;
    [SerializeField] private bool hideWhenNoFlowers = false;
    [SerializeField] private bool playPopOnIncrease = true;
    [SerializeField] private float popScale = 1.08f;
    [SerializeField] private float popDuration = 0.14f;

    private readonly Dictionary<string, StatusRow> rowsByKey = new Dictionary<string, StatusRow>();
    private readonly Dictionary<string, int> previousBloomingCounts = new Dictionary<string, int>();
    private readonly List<string> activeKeys = new List<string>();
    private StatusRow templateRow;
    private CanvasGroup transitionCanvasGroup;

    private void Awake()
    {
        EnsureTemplateRow();
    }

    private void OnEnable()
    {
        ResolveLevel();
        Subscribe();
        RefreshFromLevel();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        iconSize.x = Mathf.Max(1f, iconSize.x);
        iconSize.y = Mathf.Max(1f, iconSize.y);
        horizontalPadding = Mathf.Max(0f, horizontalPadding);
        itemSpacing = Mathf.Max(0f, itemSpacing);
        fontSize = Mathf.Max(1f, fontSize);
        rowSpacing = Mathf.Max(0f, rowSpacing);
        popScale = Mathf.Max(1f, popScale);
        popDuration = Mathf.Max(0.01f, popDuration);
    }

    public void SetLevel(BloomPuzzleLevel newLevel)
    {
        if (level == newLevel)
        {
            RefreshFromLevel();
            return;
        }

        Unsubscribe();
        level = newLevel;
        Subscribe();
        RefreshFromLevel();
    }

    public void RefreshFromLevel()
    {
        RefreshRows();
    }

    public void SetTransitionHidden(bool hidden)
    {
        EnsureTransitionCanvasGroup();
        transitionCanvasGroup.alpha = hidden ? 0f : 1f;
        transitionCanvasGroup.blocksRaycasts = !hidden;
        transitionCanvasGroup.interactable = !hidden;

        foreach (StatusRow row in rowsByKey.Values)
        {
            row.SetHidden(hidden);
        }
    }

    private void ResolveLevel()
    {
        if (level == null)
        {
            level = FindObjectOfType<BloomPuzzleLevel>();
        }
    }

    private void Subscribe()
    {
        if (level != null)
        {
            level.FlowerBloomCountChanged += RefreshRows;
        }
    }

    private void Unsubscribe()
    {
        if (level != null)
        {
            level.FlowerBloomCountChanged -= RefreshRows;
        }
    }

    private void RefreshRows(int bloomingCount, int totalCount)
    {
        RefreshRows();
    }

    private void RefreshRows()
    {
        EnsureTemplateRow();

        List<FlowerGroup> groups = CollectFlowerGroups();
        activeKeys.Clear();

        bool hasFlowers = groups.Count > 0;
        gameObject.SetActive(!hideWhenNoFlowers || hasFlowers);

        for (int i = 0; i < groups.Count; i++)
        {
            FlowerGroup group = groups[i];
            StatusRow row = GetOrCreateRow(group.Key, i);
            row.Root.gameObject.SetActive(true);
            row.SetContent(group.Icon, $"{group.BloomingCount}/{group.TotalCount}", fontSize);
            LayoutRow(row, i);

            if (playPopOnIncrease
                && previousBloomingCounts.TryGetValue(group.Key, out int previousCount)
                && group.BloomingCount > previousCount)
            {
                row.PlayPop(popScale, popDuration);
            }

            previousBloomingCounts[group.Key] = group.BloomingCount;
            activeKeys.Add(group.Key);
        }

        foreach (KeyValuePair<string, StatusRow> rowPair in rowsByKey)
        {
            if (!activeKeys.Contains(rowPair.Key))
            {
                rowPair.Value.Root.gameObject.SetActive(false);
            }
        }
    }

    private List<FlowerGroup> CollectFlowerGroups()
    {
        Dictionary<string, FlowerGroup> groupsByKey = new Dictionary<string, FlowerGroup>();
        foreach (FlowerTile flower in FindObjectsOfType<FlowerTile>())
        {
            string key = GetFlowerGroupKey(flower);
            if (!groupsByKey.TryGetValue(key, out FlowerGroup group))
            {
                group = new FlowerGroup(key, GetFlowerKindOrder(flower), GetFlowerRendererSprite(flower));
            }

            group.TotalCount++;
            if (flower.IsBlooming)
            {
                group.BloomingCount++;
                Sprite bloomingSprite = GetFlowerRendererSprite(flower);
                if (bloomingSprite != null)
                {
                    group.Icon = bloomingSprite;
                }
            }
            else if (group.Icon == null)
            {
                group.Icon = GetFlowerRendererSprite(flower);
            }

            groupsByKey[key] = group;
        }

        List<FlowerGroup> groups = new List<FlowerGroup>(groupsByKey.Values);
        groups.Sort((first, second) =>
        {
            int orderComparison = first.Order.CompareTo(second.Order);
            return orderComparison != 0 ? orderComparison : string.CompareOrdinal(first.Key, second.Key);
        });
        return groups;
    }

    private static string GetFlowerGroupKey(FlowerTile flower)
    {
        if (flower == null)
        {
            return "Unknown";
        }

        if (flower.Kind != FlowerTile.FlowerKind.Other)
        {
            return flower.Kind.ToString();
        }

        Sprite sprite = GetFlowerRendererSprite(flower);
        return sprite != null ? sprite.name : flower.gameObject.name;
    }

    private static int GetFlowerKindOrder(FlowerTile flower)
    {
        if (flower == null)
        {
            return 100;
        }

        switch (flower.Kind)
        {
            case FlowerTile.FlowerKind.Normal:
                return 0;
            case FlowerTile.FlowerKind.Hydrangea:
                return 1;
            case FlowerTile.FlowerKind.Lotus:
                return 2;
            default:
                return 10;
        }
    }

    private StatusRow GetOrCreateRow(string key, int index)
    {
        if (rowsByKey.TryGetValue(key, out StatusRow row))
        {
            return row;
        }

        row = index == 0 ? templateRow : CreateAdditionalRow(key);
        rowsByKey[key] = row;
        return row;
    }

    private void EnsureTemplateRow()
    {
        if (templateRow != null)
        {
            return;
        }

        RectTransform root = transform as RectTransform;
        if (root == null)
        {
            return;
        }

        Image background = GetComponent<Image>();
        templateRow = new StatusRow(this, root, background, EnsureIcon(root), EnsureText(root));
    }

    private StatusRow CreateAdditionalRow(string key)
    {
        GameObject rowObject = new GameObject($"Flower Status Row - {key}");
        rowObject.transform.SetParent(transform.parent, false);

        RectTransform rowRoot = rowObject.AddComponent<RectTransform>();
        Image rowBackground = rowObject.AddComponent<Image>();
        Image templateBackground = templateRow.Background;
        if (templateBackground != null)
        {
            rowBackground.sprite = templateBackground.sprite;
            rowBackground.color = templateBackground.color;
            rowBackground.type = templateBackground.type;
            rowBackground.preserveAspect = templateBackground.preserveAspect;
            rowBackground.raycastTarget = false;
        }

        Image icon = CreateIcon(rowRoot);
        TMP_Text text = CreateText(rowRoot);
        return new StatusRow(this, rowRoot, rowBackground, icon, text);
    }

    private Image EnsureIcon(RectTransform rowRoot)
    {
        Transform existing = rowRoot.Find("Flower Icon");
        if (existing != null && existing.TryGetComponent(out Image foundImage))
        {
            return foundImage;
        }

        return CreateIcon(rowRoot);
    }

    private TMP_Text EnsureText(RectTransform rowRoot)
    {
        Transform existing = rowRoot.Find("Flower Count Text");
        if (existing != null && existing.TryGetComponent(out TMP_Text foundText))
        {
            return foundText;
        }

        return CreateText(rowRoot);
    }

    private static Image CreateIcon(RectTransform rowRoot)
    {
        GameObject iconObject = new GameObject("Flower Icon");
        iconObject.transform.SetParent(rowRoot, false);
        Image icon = iconObject.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        return icon;
    }

    private TMP_Text CreateText(RectTransform rowRoot)
    {
        GameObject textObject = new GameObject("Flower Count Text");
        textObject.transform.SetParent(rowRoot, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableAutoSizing = true;
        text.fontSizeMax = fontSize;
        text.fontSizeMin = Mathf.Min(24f, fontSize);
        text.fontSize = fontSize;
        text.color = Color.black;
        return text;
    }

    private void LayoutRow(StatusRow row, int rowIndex)
    {
        RectTransform templateRoot = templateRow.Root;
        RectTransform rowRoot = row.Root;
        Vector2 rowSize = templateRoot.rect.size;

        if (rowIndex == 0)
        {
            rowRoot.anchoredPosition = templateRoot.anchoredPosition;
        }
        else
        {
            rowRoot.anchorMin = templateRoot.anchorMin;
            rowRoot.anchorMax = templateRoot.anchorMax;
            rowRoot.pivot = templateRoot.pivot;
            rowRoot.sizeDelta = templateRoot.sizeDelta;
            rowRoot.localScale = Vector3.one;
            rowRoot.anchoredPosition = templateRoot.anchoredPosition + new Vector2(0f, -(rowSize.y + rowSpacing) * rowIndex);
        }

        float fittedIconSize = Mathf.Min(iconSize.x, iconSize.y, Mathf.Max(24f, rowSize.y - horizontalPadding * 2f));
        float textX = horizontalPadding + fittedIconSize + itemSpacing;
        float textWidth = Mathf.Max(48f, rowSize.x - textX - horizontalPadding);
        float textHeight = Mathf.Max(fittedIconSize, fontSize + 10f);

        RectTransform iconRect = row.Icon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(horizontalPadding, 0f);
        iconRect.sizeDelta = new Vector2(fittedIconSize, fittedIconSize);
        iconRect.localScale = Vector3.one;

        RectTransform textRect = row.Text.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0.5f);
        textRect.anchorMax = new Vector2(0f, 0.5f);
        textRect.pivot = new Vector2(0f, 0.5f);
        textRect.anchoredPosition = new Vector2(textX, 0f);
        textRect.sizeDelta = new Vector2(textWidth, textHeight);
        textRect.localScale = Vector3.one;
        row.Text.fontSizeMax = fontSize;
        row.Text.fontSizeMin = Mathf.Min(24f, fontSize);
    }

    private void EnsureTransitionCanvasGroup()
    {
        if (transitionCanvasGroup == null)
        {
            transitionCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (transitionCanvasGroup == null)
        {
            transitionCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private static Sprite GetFlowerRendererSprite(FlowerTile flower)
    {
        if (flower == null)
        {
            return null;
        }

        SpriteRenderer spriteRenderer = flower.GetComponent<SpriteRenderer>();
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    private struct FlowerGroup
    {
        public readonly string Key;
        public readonly int Order;
        public int BloomingCount;
        public int TotalCount;
        public Sprite Icon;

        public FlowerGroup(string key, int order, Sprite icon)
        {
            Key = key;
            Order = order;
            Icon = icon;
            BloomingCount = 0;
            TotalCount = 0;
        }
    }

    private class StatusRow
    {
        public readonly RectTransform Root;
        public readonly Image Background;
        public readonly Image Icon;
        public readonly TMP_Text Text;

        private readonly FlowerBloomStatusUI owner;
        private Vector3 initialScale = Vector3.one;
        private Coroutine popRoutine;
        private CanvasGroup canvasGroup;

        public StatusRow(FlowerBloomStatusUI owner, RectTransform root, Image background, Image icon, TMP_Text text)
        {
            this.owner = owner;
            Root = root;
            Background = background;
            Icon = icon;
            Text = text;
            initialScale = root != null ? root.localScale : Vector3.one;
        }

        public void SetContent(Sprite iconSprite, string countText, float fontSize)
        {
            if (Icon != null)
            {
                Icon.sprite = iconSprite;
                Icon.enabled = iconSprite != null;
                Icon.color = Color.white;
            }

            if (Text != null)
            {
                Text.text = countText;
                Text.fontSizeMax = fontSize;
                Text.fontSize = fontSize;
            }
        }

        public void PlayPop(float popScale, float popDuration)
        {
            if (Root == null || !Root.gameObject.activeInHierarchy)
            {
                return;
            }

            if (popRoutine != null)
            {
                if (owner != null)
                {
                    owner.StopCoroutine(popRoutine);
                }
                popRoutine = null;
            }

            if (owner != null)
            {
                popRoutine = owner.StartCoroutine(PlayPopRoutine(popScale, popDuration));
            }
        }

        public void SetHidden(bool hidden)
        {
            if (Root == null)
            {
                return;
            }

            if (canvasGroup == null)
            {
                canvasGroup = Root.GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = Root.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = hidden ? 0f : 1f;
            canvasGroup.blocksRaycasts = !hidden;
            canvasGroup.interactable = !hidden;
        }

        private IEnumerator PlayPopRoutine(float popScale, float popDuration)
        {
            float halfDuration = popDuration * 0.5f;
            float elapsed = 0f;
            Vector3 peakScale = initialScale * popScale;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                Root.localScale = Vector3.Lerp(initialScale, peakScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                Root.localScale = Vector3.Lerp(peakScale, initialScale, t);
                yield return null;
            }

            Root.localScale = initialScale;
            popRoutine = null;
        }
    }
}
