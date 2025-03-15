using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BottomPanelController : MonoBehaviour
{
    // Reference to the prefab for bottom panel items (assign in the Inspector)
    public GameObject BottomPanelItemPrefab;

    void Awake()
    {
        SingletonManager.Instance.Register<BottomPanelController>(this);
    }

    void OnDestroy()
    {
        SingletonManager.Instance.Unregister<BottomPanelController>(this);
    }

    void Start()
    {
        // For testing, populate with integers 1 to 23
        int count = 2;
        int[] testValues = new int[count];
        for (int i = 0; i < count; i++)
        {
            testValues[i] = i + 1;
        }
        PopulateItems(testValues);
    }

    /// <summary>
    /// Removes all children and instantiates new items based on the given int array.
    /// Each new item is expected to have a BottomPanelItem component in its children.
    /// </summary>
    /// <param name="items">Array of integer values to populate items.</param>
    public void PopulateItems(int[] items)
    {
        // Remove all current child objects
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

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
}
