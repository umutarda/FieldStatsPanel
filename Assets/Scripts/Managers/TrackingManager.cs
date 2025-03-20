using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;
using UnityEngine.Video;
using System.Linq;

public class TrackingManager : MonoBehaviour, IDebuggable
{
    [SerializeField] TextAsset yoloJsonFile;

    public YoloData YoloData { get; private set; }
    public FrameTrackingData[] ByteTrackData { get; private set; }
    public bool IsReady { get; private set; }

    private TrackRequest requestData;

    private OverlayControllersManager overlayControllersManager;
    private VideoControlSlider videoControlSlider;
    private BottomPanelController bottomPanelController;
    private TrackingManagerRpc trackingManagerRpc;
    private OverlayPropertiesManager overlayPropertiesManager;

    private const int MAX_PLAYER_COUNT = 23;
    void Awake()
    {
        YoloData = JsonConvert.DeserializeObject<YoloData>(yoloJsonFile.text);
        SingletonManager.Instance.Register<TrackingManager>(this);
        ByteTrackData = new FrameTrackingData[0];
    }

    void Start()
    {
        trackingManagerRpc = new(this);
        overlayControllersManager = SingletonManager.Instance.Get<OverlayControllersManager>();
        bottomPanelController = SingletonManager.Instance.Get<BottomPanelController>();
        videoControlSlider = SingletonManager.Instance.Get<VideoControlSlider>();
        overlayPropertiesManager = SingletonManager.Instance.Get<OverlayPropertiesManager>();

        videoControlSlider.minValue = videoControlSlider.maxValue = 7200;

        videoControlSlider.onValueChanged.AddListener((val) =>
        {
            GoToAndStop((long)val);
        });

        GoToAndStop(7200);


        int[] startIds = new int[MAX_PLAYER_COUNT];
        for (int i = 0; i < MAX_PLAYER_COUNT; i++)
        {
            startIds[i] = i + 1;
        }
        bottomPanelController.PopulateIds(startIds);
        overlayPropertiesManager.UpdatePropertiesById(startIds);

    }


    void OnDestroy()
    {
        SingletonManager.Instance.Unregister<TrackingManager>(this);
    }


    /// <summary>
    /// Adds a new track entry using the given location and source.
    /// The entry is added with a placeholder id of 0.
    /// </summary>
    /// <param name="location">The Vector2 location (x, y) to track.</param>
    /// <param name="source">The source identifier for this entry.</param>
    public void AddTrackEntry(Vector2 location, int source)
    {
        
        var currentHead = bottomPanelController.PopHeadItem();
        if (currentHead == null)
        {
            Debug.LogWarning("TrackingManager::AddTrackEntry: BottomPanel has no child at its head");
            return;
        }

        // Create a new track entry.
        TrackEntry newEntry = new TrackEntry
        {
            id = currentHead.Value, // placeholder id.
            c = new float[] { location.x, location.y },
            src = source
        };

        Destroy(currentHead.gameObject);

        // Add the new entry to the list.
        //trackEntries.Add(newEntry);

        // For testing, serialize the entry to JSON and print to console.
        string jsonEntry = JsonConvert.SerializeObject(newEntry);
        Debug.Log("Added track entry: " + jsonEntry);

        AddToFrameTrackingData(newEntry,(int)videoControlSlider.maxValue);

        currentHead = bottomPanelController.SeekHeadItem();
        if (currentHead == null)
        {
            RequestReady();
        }
    }


    public void RequestReady()
    {
        long currentFrame = (long)videoControlSlider.maxValue;

        // Find the tracking data for the current frame.
        FrameTrackingData frameData = Array.Find<FrameTrackingData>(ByteTrackData,data => data.fr == currentFrame);
        List<TrackEntry> convertedEntries = new List<TrackEntry>();

        if (frameData != null && frameData.obj != null)
        {
            // Convert each TrackObject to a TrackEntry.
            foreach (var to in frameData.obj)
            {
                TrackEntry entry = new TrackEntry
                {
                    id = to.id,
                    c = to.c,
                    src = to.src
                };
                convertedEntries.Add(entry);
            }
        }

        requestData = new TrackRequest
        {
            frame_id = currentFrame,
            coords = convertedEntries
        };
        IsReady = true;

        Debug.Log("Request ready: " + JsonConvert.SerializeObject(requestData));
    }


    /// <summary>
    /// Returns the prepared TrackRequest struct and resets the isReady flag.
    /// </summary>
    public TrackRequest GetRequest()
    {
        IsReady = false;
        Debug.Log("Request retrieved: " + JsonConvert.SerializeObject(requestData));
        return requestData;
    }

    public void OnReceive(UpdateResult updateResult)
    {
        string jsonResult = JsonConvert.SerializeObject(updateResult, Formatting.Indented);
        Debug.Log(jsonResult);
        ByteTrackData = updateResult.tracks;

        videoControlSlider.minValue = videoControlSlider.maxValue;
        videoControlSlider.maxValue = updateResult.lost_frame_id;
        videoControlSlider.value = videoControlSlider.maxValue;

        bottomPanelController.PopulateIds(updateResult.lost_ids);
        overlayPropertiesManager.UpdateLostIds(updateResult.lost_ids);
        //GoToAndStop(updateResult.lost_frame_id,false);
    }

    /// <summary>
    /// Instructs each OverlayController to pause when it reaches the specified target frame.
    /// This method does not directly pause video playback.
    /// </summary>
    /// <param name="targetFrame">The frame to which overlay controllers should pause.</param>
    public void GoToAndStop(long targetFrame, bool withSeek = true)
    {
        if (overlayControllersManager.IsEmpty())
        {
            Debug.LogWarning("No overlay controllers available.");
            return;
        }

        overlayControllersManager.ForEachOverlayController(oc => oc.SetCurrentFrame(targetFrame, withSeek));


        Debug.Log("Set pause condition for overlay controllers at target frame: " + targetFrame);
    }

    /// <summary>
    /// Adds or updates frame tracking data for the given frame index.
    /// It converts the current track entries into TrackObjects and stores them.
    /// </summary>
    /// <param name="frameIndex">The frame index to update.</param>
    public void AddToFrameTrackingData(TrackEntry entry,int frameIndex)
    {
        // Find existing frame tracking data for the specified frame.
        FrameTrackingData existingData =  Array.Find<FrameTrackingData>(ByteTrackData,data => data.fr == frameIndex);

        if (existingData != null)
        {
            // Convert the existing array to a list for easier updates.
            List<TrackObject> currentTrackObjects = new List<TrackObject>(existingData.obj);

            // Process each track entry.
            //foreach (var entry in trackEntries)
            {
                bool found = false;
                // Look for an existing track object with the same id.
                for (int i = 0; i < currentTrackObjects.Count; i++)
                {
                    if (currentTrackObjects[i].id == entry.id)
                    {
                        // Update the coordinate (c) while keeping other properties intact.
                        currentTrackObjects[i].c = entry.c;
                        found = true;
                        break;
                    }
                }
                // If not found, create a new track object.
                if (!found)
                {
                    TrackObject newObj = new TrackObject
                    {
                        id = entry.id,
                        cls_id = 0, // Default value; modify if needed.
                        c = entry.c,
                        src = entry.src
                    };
                    currentTrackObjects.Add(newObj);
                }
            }
            // Update the existing frame data.
            existingData.obj = currentTrackObjects.ToArray();
        }
        else
        {
            // If no tracking data exists for the given frame, create a new record.
            List<TrackObject> trackObjects = new List<TrackObject>();
            //foreach (var entry in trackEntries)
            {
                TrackObject to = new TrackObject
                {
                    id = entry.id,
                    cls_id = 0, // Default value; modify as needed.
                    c = entry.c,
                    src = entry.src
                };
                trackObjects.Add(to);
            }
            FrameTrackingData newData = new FrameTrackingData
            {
                fr = frameIndex,
                obj = trackObjects.ToArray()
            };
            var list = ByteTrackData.ToList();
            list.Add(newData);
            ByteTrackData = list.ToArray();
        }
        Debug.Log("Updated frame tracking data for frame " + frameIndex);
        overlayControllersManager.RedrawAll();
    }


    public void DebugUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //AddToFrameTrackingData((int)videoControlSlider.maxValue);
            RequestReady();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            GoToAndStop((long)videoControlSlider.maxValue, false);
        }

        if(Input.GetKeyDown(KeyCode.Return)) 
        {
            Destroy(bottomPanelController.PopHeadItem());
        }
    }



}


