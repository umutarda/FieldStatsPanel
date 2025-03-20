using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BottomPanelController : MonoBehaviour
{

    private int effectiveChildCount;
    void Awake()
    {
        SingletonManager.Instance.Register<BottomPanelController>(this);
    }

    void OnDestroy()
    {
        SingletonManager.Instance.Unregister<BottomPanelController>(this);
    }



    /// <summary>
    /// Removes all children and instantiates new items based on the given int array.
    /// Each new item is expected to have a BottomPanelItem component in its children.
    /// </summary>
    /// <param name="items">Array of integer values to populate items.</param>
    public void PopulateIds(int[] items)
    {
        effectiveChildCount = items.Length;
        // Remove all current child objects
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        var BottomPanelItemPrefab = SingletonManager.Instance.Get<AssetManager>().GetAsset(AssetName.BOTTOM_PANEL_ITEM);
        // Create a new item for each integer in the array
        foreach (int value in items)
        {
            // Instantiate the prefab as a child of this GameObject
            GameObject newItem = Instantiate(BottomPanelItemPrefab, transform);
            // Attempt to get the BottomPanelItem component from the new item or its children
            BottomPanelItem itemComponent = newItem.GetComponentInChildren<BottomPanelItem>();
            if (itemComponent != null)
            {
                // Set the item's value to the current integer
                itemComponent.SetValue(value);
            }
            else
            {
                Debug.LogWarning("BottomPanelItem component not found in the instantiated prefab.");
            }
        }
    }

    public BottomPanelItem SeekHeadItem()
    {
        if (effectiveChildCount > 0)
        {
            // Get the first child and try to retrieve a BottomPanelItem component.
            BottomPanelItem headItem = transform.GetChild(0).GetComponentInChildren<BottomPanelItem>();
            return headItem;
        }
        return null;
    }

    public BottomPanelItem PopHeadItem()
    {
        var headItem = SeekHeadItem();
        if (headItem != null)
        {
            effectiveChildCount--;
            headItem.transform.SetParent(null);
        }

        return headItem;
    }
}
