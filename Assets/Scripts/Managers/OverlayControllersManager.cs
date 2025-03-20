using System;
using UnityEngine;

public class OverlayControllersManager : MonoBehaviour
{
    private OverlayController[] overlayControllers;

    void Awake()
    {
        // Gather all OverlayController components in the scene.
        overlayControllers = FindObjectsOfType<OverlayController>();
        SingletonManager.Instance.Register<OverlayControllersManager>(this);
    }

    void OnDestroy()
    {
        SingletonManager.Instance.Unregister<OverlayControllersManager>(this);
    }

    public bool IsEmpty() => overlayControllers.Length == 0;
    /// <summary>
    /// Executes the provided action on each OverlayController found in the scene.
    /// </summary>
    /// <param name="action">Delegate to execute for each OverlayController.</param>
    public void ForEachOverlayController(Action<OverlayController> action)
    {

        foreach (OverlayController controller in overlayControllers)
        {
            action(controller);
        }
    }

    public void RedrawAll() => ForEachOverlayController(oc =>oc.Redraw());

}
