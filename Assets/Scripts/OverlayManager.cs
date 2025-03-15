using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Newtonsoft.Json;

public class OverlayManager : MonoBehaviour
{
    // Assign your JSON file as a TextAsset via the Inspector
    public TextAsset jsonFile;

    // Filter overlay objects by source
    public int src;

    // Holds the parsed JSON overlay data
    private OverlayData overlayData;

    private VideoPlayer videoPlayer;

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
        //Debug.Log("OnFrameReady(VideoPlayer source, long frameIdx)");
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


            float W = (obj.bbox[2] - obj.bbox[0])/2;
            float H = (obj.bbox[3] - obj.bbox[1])/2;
            // Normalize bbox values: original values are in pixels on a 1920x1080 video
            float normLowX = (obj.bbox[0] + W) / 1920f;
            float normLowY = (obj.bbox[1] + H) / 1080f;
            float normHighX = (obj.bbox[2] + W) / 1920f;
            float normHighY = (obj.bbox[3] + H) / 1080f;

            // Compute normalized width and height
            float normW = normHighX - normLowX;
            float normH = normHighY - normLowY;

            // Convert normalized coordinates to local pixel coordinates based on parent's dimensions
            float w = normW * parentWidth;
            float h = normH * parentHeight;
            float x = (normLowX) * parentWidth;
            float y = (1 - (normLowY )) * parentHeight; //- h * .5f


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
