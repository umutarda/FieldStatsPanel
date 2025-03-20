using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DebugManager : MonoBehaviour
{
    private List<IDebuggable> debuggableComponents;

    void Awake()
    {
        // Find all components implementing IDebuggable in the scene.
        debuggableComponents = FindObjectsOfType<MonoBehaviour>()
                                  .OfType<IDebuggable>()
                                  .ToList();
    }

    void Update()
    {
        // Call DebugUpdate() on every debuggable component.
        foreach (IDebuggable debuggable in debuggableComponents)
        {
            debuggable.DebugUpdate();
        }
    }
}
