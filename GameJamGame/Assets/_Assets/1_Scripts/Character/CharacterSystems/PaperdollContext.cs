
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class PaperDollContext
{
    public enum BodyPart { Head, Body }
    public SortingGroup SortingGroup;
    public Transform GraphicsRoot;
    // Reference for head, body, left and right arm, legs, hat, backpack and held item
    public SpriteRenderer Head;
    public SpriteRenderer Body;

    private int[] sortOrderOriginalCache;

    private int sortGroupOrderCache;

    public void CacheDefaultSortOrder()
    {
        sortGroupOrderCache = SortingGroup.sortingOrder;

        var allBodypartEnums = (System.Enum.GetValues(typeof(BodyPart)) as BodyPart[]);
        if (sortOrderOriginalCache == null) sortOrderOriginalCache = new int[allBodypartEnums.Length];

        foreach (var bodyPart in allBodypartEnums)
        {
            var sr = GetSpriteRenderer(bodyPart);
            if (sr != null)
            {
                sortOrderOriginalCache[(int)bodyPart] = sr.sortingOrder;
            }
        }
    }

    public void ResetToDefaultSortOrder()
    {
        SortingGroup.sortingOrder = sortGroupOrderCache;

        var allBodypartEnums = (System.Enum.GetValues(typeof(BodyPart)) as BodyPart[]);
        if (sortOrderOriginalCache == null || sortOrderOriginalCache.Length != allBodypartEnums.Length)
        {
            Debug.LogWarning("Default sort order cache is not set or invalid. Call CacheDefaultSortOrder() first.");
            return;
        }
        foreach (var bodyPart in allBodypartEnums)
        {
            var sr = GetSpriteRenderer(bodyPart);
            if (sr != null)
            {
                sr.sortingOrder = sortOrderOriginalCache[(int)bodyPart];
            }
        }
    }

    public void SetAllSortOrder(int sortOrder)
    {
        SortingGroup.sortingOrder = sortOrder;
    }

    public Transform GetBodyPart(BodyPart bodyPart)
    {
        switch (bodyPart)
        {
            case BodyPart.Head:
                return Head != null ? Head.transform : null;
            case BodyPart.Body:
                return Body != null ? Body.transform : null;
            default:
                Debug.LogError("Invalid body part specified: " + bodyPart);
                return null;
        }
    }

    public void SetSprite(BodyPart bodyPart, Sprite newSprite)
    {
        var spriteRenderer = GetSpriteRenderer(bodyPart);
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = newSprite;
        }
        else
        {
            Debug.LogError("SpriteRenderer not found for body part: " + bodyPart);
        }
    }

    public void SetColor(BodyPart bodyPart, Color color)
    {
        var spriteRenderer = GetSpriteRenderer(bodyPart);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
        else
        {
            Debug.LogError("SpriteRenderer not found for body part: " + bodyPart);
        }
    }

    public SpriteRenderer GetSpriteRenderer(BodyPart bodyPart)
    {
        switch (bodyPart)
        {
            case BodyPart.Head:
                return Head;
            case BodyPart.Body:
                return Body;
            default:
                Debug.LogError("Invalid body part specified: " + bodyPart);
                return null;
        }
    }
}