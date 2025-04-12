using System.Collections.Generic;
using UnityEngine;

public enum AssetName
{
    BOTTOM_PANEL_ITEM,
    TRACKING_ELEMENT,
    TRACKING_SELECTION_CURSOR
}

[System.Serializable]
public class AssetMapping
{
    public AssetName assetName;
    public GameObject prefab;
}

public class AssetManager : MonoBehaviour
{
    // List to be populated in the Inspector with asset mappings.
    [SerializeField]
    private List<AssetMapping> assetMappings = new List<AssetMapping>();

    // Dictionary built at runtime for fast lookup.
    private Dictionary<AssetName, GameObject> assetDictionary;

    void Awake()
    {
        BuildDictionary();
        SingletonManager.Instance.Register<AssetManager>(this);
    }

    void OnDestroy()
    {
        SingletonManager.Instance.Unregister<AssetManager>(this);
    }

    private void BuildDictionary()
    {
        assetDictionary = new Dictionary<AssetName, GameObject>();
        foreach (var mapping in assetMappings)
        {
            if (!assetDictionary.ContainsKey(mapping.assetName))
            {
                assetDictionary.Add(mapping.assetName, mapping.prefab);
            }
            else
            {
                Debug.LogWarning("Duplicate asset mapping for: " + mapping.assetName);
            }
        }
    }

    /// <summary>
    /// Retrieves the prefab associated with the provided AssetName enum.
    /// </summary>
    /// <param name="assetName">The enum value representing the asset.</param>
    /// <returns>The corresponding prefab, or null if not found.</returns>
    public GameObject GetAsset(AssetName assetName)
    {
        if (assetDictionary.TryGetValue(assetName, out GameObject asset))
        {
            return asset;
        }
        Debug.LogWarning("Asset not found for: " + assetName);
        return null;
    }
}
