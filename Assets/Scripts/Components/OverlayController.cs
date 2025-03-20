using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using TMPro;
using Newtonsoft.Json;
using System;


public class OverlayController : MonoBehaviour, IPointerClickHandler
{

    public int src;

    private VideoPlayer videoPlayer;
    private OverlayPropertiesManager overlayPropertiesManager;

    private const int VIDEO_WIDTH = 1920;
    private const int VIDEO_HEIGHT = 1080;

    // Internal fields to hold the pause condition.
    private long targetFrame = -1;

    /// <summary>
    /// Gets the current frame from the video player.
    /// Setting this property assigns the provided value as the target frame
    /// and enables pausing when the current frame reaches or exceeds it.
    /// </summary>
    public long CurrentFrame
    {
        get { return targetFrame; }
    }


    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();

        videoPlayer.sendFrameReadyEvents = true;
        if (videoPlayer != null)
        {
            videoPlayer.frameReady += OnFrameReady;
        }
        // For testing: fill byteTrackData with sample tracking data.
        // byteTrackData = new FrameTrackingData[]
        // {
        //     new FrameTrackingData
        //     {
        //         fr = 0,
        //         obj = new TrackObject[]
        //         {
        //             new TrackObject { id = 5, cls_id = 1, c = new float[]{960, 540}, src = 0 },
        //             new TrackObject { id = 6, cls_id = 2, c = new float[]{480, 270}, src = 0 },
        //             new TrackObject { id = 7, cls_id = 1, c = new float[]{1000, 550}, src = 1 },
        //             new TrackObject { id = 8, cls_id = 2, c = new float[]{500, 300}, src = 1 }
        //         }
        //     },
        // };
    }


    void Start()
    {
        overlayPropertiesManager = SingletonManager.Instance.Get<OverlayPropertiesManager>();
    }

    public void SetCurrentFrame(long targetFrame, bool withSeek = true)
    {

        if (!videoPlayer.isPlaying)
            videoPlayer.Play();

        if (withSeek)
            videoPlayer.frame = targetFrame;
        else
        {
            if (videoPlayer.frame > targetFrame)
            {
                videoPlayer.frame = (long)SingletonManager.Instance.Get<VideoControlSlider>().minValue;
                Debug.LogWarning($"{transform.name}:SetCurrentFrame without seek is called and player frame is greater than target frame, video player frame is set to beginning");
            }
        }

        this.targetFrame = targetFrame;

    }
    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.frameReady -= OnFrameReady;
        }
    }

    void OnFrameReady(VideoPlayer source, long frameIdx)
    {
        int currentFrame = (int)frameIdx;
        Draw(currentFrame);

        // Check if the pause condition is met.
        if (currentFrame >= targetFrame)
        {
            videoPlayer.Pause();
            //videoPlayer.frame = pauseTargetFrame;
            //pauseWhenReached = false;
            //Debug.Log("OverlayController paused at frame: " + currentFrame);
        }

        else
        {
            videoPlayer.Play();
            //Debug.Log("OverlayController played at frame: " + currentFrame + " with target: " + targetFrame);
        }
    }

    public void Draw(int frameIndex)
    {
        
        DrawFrameOverlay(frameIndex);    // YOLO overlay drawing logic (unchanged)
        DrawTrackingOverlay(frameIndex);   // New tracking overlay drawing
    }

    public void Redraw() 
    {
        Draw((int)CurrentFrame);
    }
    /// <summary>
    /// Draws YOLO overlay boxes for the given frame index onto the current RectTransform area.
    /// (Existing logic using YoloData)
    /// </summary>
    public void DrawFrameOverlay(int frameIndex)
    {
        // Remove previous YOLO overlay objects (if any)
        foreach (Transform child in transform)
        {
            // Optional: you may want to tag or parent these differently if you want to preserve tracking overlays.
            Destroy(child.gameObject);
        }

        // Locate the frame data matching the current frame index
        FrameData currentFrame = SingletonManager.Instance.Get<TrackingManager>().YoloData.frames.Find(frame => frame.fr == frameIndex);
        if (currentFrame == null) return; // No overlay data for this frame

        // Get the dimensions of the current UI area (assumes this component is on a UI element)
        RectTransform parentRect = GetComponent<RectTransform>();
        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;

        // Process each frame object from YOLO data
        foreach (var obj in currentFrame.obj)
        {
            if (obj.bbox == null || obj.bbox.Count < 4 || obj.src != src)
                continue;

            float halfWidth = (obj.bbox[2] - obj.bbox[0]) / 2f;
            float halfHeight = (obj.bbox[3] - obj.bbox[1]) / 2f;
            float normLowX = (obj.bbox[0] + halfWidth) / VIDEO_WIDTH;
            float normLowY = (obj.bbox[1] + halfHeight) / VIDEO_HEIGHT;
            float normHighX = (obj.bbox[2] + halfWidth) / VIDEO_WIDTH;
            float normHighY = (obj.bbox[3] + halfHeight) / VIDEO_HEIGHT;
            float normW = normHighX - normLowX;
            float normH = normHighY - normLowY;
            float w = normW * parentWidth;
            float h = normH * parentHeight;
            float x = normLowX * parentWidth;
            float y = (1 - normLowY) * parentHeight;

            GameObject overlay = new GameObject("OverlayBox", typeof(RectTransform));
            overlay.transform.SetParent(transform, false);
            RectTransform rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
            Image img = overlay.AddComponent<Image>();
            img.color = new Color(1, 0, 0, 0.3f);
        }
    }

    /// <summary>
    /// Draws small tracking circles (with id text) for the given frame index based on byteTrackData.
    /// This overlay is drawn in addition to the YOLO overlay.
    /// </summary>
    public void DrawTrackingOverlay(int frameIndex)
    {
        // Get the dimensions of the current UI area.
        RectTransform parentRect = GetComponent<RectTransform>();
        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;

        var byteTrackData = SingletonManager.Instance.Get<TrackingManager>().ByteTrackData;
        // Find tracking data for the current frame.
        FrameTrackingData currentTrackingData = null;
        bool found = false;
        if (byteTrackData != null)
        {
            foreach (var ft in byteTrackData)
            {
                if (ft.fr == frameIndex)
                {
                    currentTrackingData = ft;
                    found = true;
                    break;
                }
            }
        }
        if (!found)
        {
            Debug.LogWarning($"{transform.name}::OverlayController Frame tracking data not found at frame {frameIndex}");
            return;
        }


        var trackingElementPrefab = SingletonManager.Instance.Get<AssetManager>().GetAsset(AssetName.TRACKING_ELEMENT);
        // For each track object, instantiate a circle prefab and update its text with the id.
        foreach (var trackObj in currentTrackingData.obj)
        {
            if (trackObj.src != src)
                continue;

            var props = overlayPropertiesManager.OverlayElementPropertiesById(trackObj.id);
            if (props == null || !props.DrawTrackingOverlay)
                continue;

            // Assume trackObj.c holds [x, y] in VIDEO_WIDTH x VIDEO_HEIGHT coordinate space.
            float videoX = trackObj.c[0];
            float videoY = trackObj.c[1];
            float normX = videoX / VIDEO_WIDTH;
            float normY = videoY / VIDEO_HEIGHT;
            float localX = normX * parentWidth;
            float localY = (1 - normY) * parentHeight; // UI y=0 is usually at bottom

            GameObject circle = Instantiate(trackingElementPrefab, transform, false);

            RectTransform circleRect = circle.GetComponent<RectTransform>();
            circleRect.anchorMin = new Vector2(0, 0);
            circleRect.anchorMax = new Vector2(0, 0);
            circleRect.anchoredPosition = new Vector2(localX, localY);
            // Optionally, adjust the circle size:
            // circleRect.sizeDelta = new Vector2(30, 30);

            // Update the TextMeshPro component to display the track id.
            TMP_Text idText = circle.GetComponentInChildren<TMP_Text>();
            if (idText != null)
            {
                idText.text = trackObj.id.ToString();
            }
            else
            {
                Debug.LogWarning("No TMP_Text component found in the circle prefab.");
            }
        }
    }

    // Called when the UI element (with the RectTransform) is clicked.
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
            Rect rect = rectTransform.rect;
            float normX = (localPoint.x - rect.x) / rect.width;
            float normY = 1 - (localPoint.y - rect.y) / rect.height;
            Vector2 videoPoint = new Vector2(normX * VIDEO_WIDTH, normY * VIDEO_HEIGHT);
            SingletonManager.Instance.Get<TrackingManager>().AddTrackEntry(videoPoint, src);
            Debug.Log("Clicked video coordinate: " + videoPoint);
        }
        else
        {
            Debug.Log("Unable to convert click position to local coordinates.");
        }

        //Draw((int)CurrentFrame);
    }

}
