using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using System;

public class OverlayController : MonoBehaviour, IPointerClickHandler
{
    // Assign your JSON file as a TextAsset via the Inspector
    public TextAsset jsonFile;

    // Filter overlay objects by source
    public int src;

    // Holds the parsed JSON overlay data
    private OverlayData overlayData;

    private VideoPlayer videoPlayer;
    private const int VIDEO_WIDTH = 1920;
    private const int VIDEO_HEIGHT = 1080;


    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        // Deserialize the JSON text from the TextAsset into our OverlayData object
        overlayData = JsonConvert.DeserializeObject<OverlayData>(jsonFile.text);
        videoPlayer.sendFrameReadyEvents = true;

        // Subscribe to VideoPlayer's frameReady event
        if (videoPlayer != null)
        {
            videoPlayer.frameReady += OnFrameReady;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from the event to avoid memory leaks
        if (videoPlayer != null)
        {
            videoPlayer.frameReady -= OnFrameReady;
        }
    }

    // Called whenever a new video frame is ready
    void OnFrameReady(VideoPlayer source, long frameIdx)
    {
        DrawFrameOverlay((int)frameIdx);
    }

    // Draws overlay boxes for the given frame index onto the current RectTransform area
    public void DrawFrameOverlay(int frameIndex)
    {
        // Remove previous overlay objects (if any)
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Locate the frame data matching the current frame index
        FrameData currentFrame = overlayData.frames.Find(frame => frame.fr == frameIndex);
        if (currentFrame == null) return; // No overlay data for this frame

        // Get the dimensions of the current UI area (assumes this component is on a UI element)
        RectTransform parentRect = GetComponent<RectTransform>();
        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;

        // Process each frame object
        foreach (var obj in currentFrame.obj)
        {
            // Ensure bbox exists, has exactly 4 values, and matches the source filter
            if (obj.bbox == null || obj.bbox.Count < 4 || obj.src != src)
                continue;

            // Calculate half width and half height for shifting the bbox to its center
            float halfWidth = (obj.bbox[2] - obj.bbox[0]) / 2f;
            float halfHeight = (obj.bbox[3] - obj.bbox[1]) / 2f;
            
            // Normalize bbox values: original values are in pixels on a VIDEO_WIDTHxVIDEO_HEIGHT video
            float normLowX = (obj.bbox[0] + halfWidth) / VIDEO_WIDTH;
            float normLowY = (obj.bbox[1] + halfHeight) / VIDEO_HEIGHT;
            float normHighX = (obj.bbox[2] + halfWidth) / VIDEO_WIDTH;
            float normHighY = (obj.bbox[3] + halfHeight) / VIDEO_HEIGHT;

            // Compute normalized width and height
            float normW = normHighX - normLowX;
            float normH = normHighY - normLowY;

            // Convert normalized coordinates to local pixel coordinates based on parent's dimensions
            float w = normW * parentWidth;
            float h = normH * parentHeight;
            float x = normLowX * parentWidth;
            // Flip the y coordinate because UI y=0 is usually at the bottom.
            float y = (1 - normLowY) * parentHeight;

            // Create a new GameObject to represent the overlay box
            GameObject overlay = new GameObject("OverlayBox", typeof(RectTransform));
            overlay.transform.SetParent(transform, false);

            // Set up RectTransform so the position is relative to the parent's lower left
            RectTransform rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);

            // Add an Image component to visualize the bounding box overlay.
            Image img = overlay.AddComponent<Image>();
            img.color = new Color(1, 0, 0, 0.3f); // semi-transparent red
        }
    }

    // Called when the UI element (with the RectTransform) is clicked.
    // This method converts the clicked screen position into local normalized coordinates (0-1) and prints them.
    public void OnPointerClick(PointerEventData eventData)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint))
        {
            // Convert the local point to normalized coordinates.
            // Note: rectTransform.rect may have negative x/y if the pivot is not at (0,0).
            Rect rect = rectTransform.rect;
            float normX = (localPoint.x - rect.x) / rect.width;
            float normY = 1 - (localPoint.y - rect.y) / rect.height;
            Vector2 videoPoint = new(normX * VIDEO_WIDTH, normY * VIDEO_HEIGHT);

            SingletonManager.Instance.Get<TrackingManager>().AddTrackEntry(videoPoint,src);
            Debug.Log("Clicked normalized local position: " + videoPoint);
        }
        else
        {
            Debug.Log("Unable to convert click position to local coordinates.");
        }
    }
}

// Classes to match the JSON structure
public class OverlayData
{
    public List<FrameData> frames;
}

public class FrameData
{
    public int fr;              // Frame index
    public List<FrameObject> obj;  // List of objects in this frame
}

public class FrameObject
{
    public int src;
    // Bounding box: [lowx, lowy, highx, highy]
    public List<float> bbox;
}
