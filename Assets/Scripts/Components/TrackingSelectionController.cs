using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TrackingSelectionController : MonoBehaviour, IDebuggable
{

    private Transform cursorTransform;
    private OverlayControllersManager overlayControllersManager;
    private int lastIndexPosition = -1;
    public void DebugUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {

            lastIndexPosition = Input.GetKey(KeyCode.LeftControl) ? Mathf.Max(0, lastIndexPosition - 1) : (lastIndexPosition + 1);
            var newParent = GetYoloOverlayFromAll(lastIndexPosition);
            if (newParent == null)
            {

                lastIndexPosition = 0;
                newParent = GetYoloOverlayFromAll(lastIndexPosition);

            }




            cursorTransform.gameObject.SetActive(true);
            cursorTransform.SetParent(newParent);
            cursorTransform.localPosition = Vector3.zero;



        }
    }

    void Start()
    {
        overlayControllersManager = SingletonManager.Instance.Get<OverlayControllersManager>();
        cursorTransform = Instantiate(SingletonManager.Instance.Get<AssetManager>().GetAsset(AssetName.TRACKING_SELECTION_CURSOR)).transform;
        cursorTransform.gameObject.SetActive(false);
    }

    Transform GetYoloOverlayFromAll(int index)
    {
        Transform result = null;
        int remaining = index; // Use a local variable to track the remaining index.

        overlayControllersManager.ForEachOverlayController(overlayController =>
        {
            // Get all children with tag "OverlayBox" using LINQ.
            var overlayBoxes = overlayController.transform
                                 .GetComponentsInChildren<Transform>()
                                 .Where(child => child.CompareTag("OverlayBox"))
                                 .ToList();

            if (overlayBoxes.Count <= remaining)
            {
                remaining -= overlayBoxes.Count;
            }
            else if (result == null)
            {
                if (remaining < overlayBoxes.Count)
                {
                    result = overlayBoxes[remaining];
                    Debug.Log("Result set from " + overlayController.transform.name + " sibling no: " + remaining);
                }


            }
        });

        return result;
    }

}
