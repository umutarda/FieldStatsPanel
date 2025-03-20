using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OverlayElementProperties
{
    public int Id;
    public bool DrawTrackingOverlay;
    public bool Lost; // Indicates whether this element is considered "lost"
}

public class OverlayPropertiesManager : MonoBehaviour, IDebuggable
{
    // An array of properties for overlay elements.
    public OverlayElementProperties[] overlayElementProperties;
    private OverlayControllersManager overlayControllersManager;

    // Internal flag to toggle the behavior for lost elements on key press L.
    private bool lostToggle = false;

    void Awake()
    {
        SingletonManager.Instance.Register<OverlayPropertiesManager>(this);
    }

    void Start()
    {
        overlayControllersManager = SingletonManager.Instance.Get<OverlayControllersManager>();
    }

    void OnDestroy()
    {
        SingletonManager.Instance.Unregister<OverlayPropertiesManager>(this);
    }

    public void DebugUpdate()
    {
        // Key press A: set all DrawTrackingOverlay to true.
        if (Input.GetKeyDown(KeyCode.A))
        {
            lostToggle = true;
            if (overlayElementProperties != null)
            {
                foreach (var prop in overlayElementProperties)
                {
                    prop.DrawTrackingOverlay = true;
                }
                Debug.Log("Set all DrawTrackingOverlay flags to true.");
            }

            overlayControllersManager.RedrawAll();
        }

        // Key press N: set all DrawTrackingOverlay to false.
        if (Input.GetKeyDown(KeyCode.N))
        {
            lostToggle = false;
            if (overlayElementProperties != null)
            {
                foreach (var prop in overlayElementProperties)
                {
                    prop.DrawTrackingOverlay = false;

                }
                Debug.Log("Set all DrawTrackingOverlay flags to false.");
            }

            overlayControllersManager.RedrawAll();
        }

        // Key press L: Toggle DrawTrackingOverlay for elements where Lost is true.
        if (Input.GetKeyDown(KeyCode.L))
        {
            lostToggle = !lostToggle; // Toggle the flag.
            if (overlayElementProperties != null)
            {
                foreach (var prop in overlayElementProperties)
                {
                    if (prop.Lost)
                    {
                        prop.DrawTrackingOverlay = lostToggle;
                    }
                }
                Debug.Log("Toggled DrawTrackingOverlay for lost elements to " + lostToggle);

            }


            overlayControllersManager.RedrawAll();

        }
    }

    /// <summary>
    /// Updates the overlayElementProperties based on the provided array of IDs.
    /// For each ID:
    /// - If an element with the same ID exists, its properties are retained.
    /// - If not, a new element is created with default values.
    /// </summary>
    /// <param name="newIds">Array of IDs to update or add.</param>
    public void UpdatePropertiesById(int[] newIds)
    {
        Dictionary<int, OverlayElementProperties> currentDict = new Dictionary<int, OverlayElementProperties>();
        if (overlayElementProperties != null)
        {
            foreach (var prop in overlayElementProperties)
            {
                if (!currentDict.ContainsKey(prop.Id))
                {
                    currentDict.Add(prop.Id, prop);
                }
            }
        }

        List<OverlayElementProperties> updatedList = new List<OverlayElementProperties>();

        foreach (int id in newIds)
        {
            if (currentDict.TryGetValue(id, out OverlayElementProperties existingProp))
            {
                updatedList.Add(existingProp);
            }
            else
            {
                OverlayElementProperties newProp = new OverlayElementProperties
                {
                    Id = id,
                    DrawTrackingOverlay = true, // default value; adjust if needed.
                    Lost = false
                };
                updatedList.Add(newProp);
            }
        }

        overlayElementProperties = updatedList.ToArray();
    }

    /// <summary>
    /// Returns the OverlayElementProperties for the given ID, or null if not found.
    /// </summary>
    /// <param name="id">The ID to search for.</param>
    public OverlayElementProperties OverlayElementPropertiesById(int id)
    {
        if (overlayElementProperties != null)
        {
            foreach (var prop in overlayElementProperties)
            {
                if (prop.Id == id)
                {
                    return prop;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Updates the Lost property for each overlay element.
    /// For each element, if its ID is contained in the lostIds array, Lost is set to true; otherwise, false.
    /// </summary>
    /// <param name="lostIds">Array of IDs that are considered lost.</param>
    public void UpdateLostIds(int[] lostIds)
    {
        if (overlayElementProperties != null)
        {
            foreach (var prop in overlayElementProperties)
            {
                bool isLost = false;
                foreach (int id in lostIds)
                {
                    if (id == prop.Id)
                    {
                        isLost = true;
                        break;
                    }
                }
                prop.Lost = isLost;
            }
            Debug.Log("Updated Lost property for overlay elements based on provided lost IDs.");
        }
    }
}
