using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;
using Unity.VisualScripting;
using UnityEngine.Video;

public class TrackingManager : MonoBehaviour
{
    // List to hold all track entries.
    private List<TrackEntry> trackEntries = new List<TrackEntry>();

    // Private array of VideoPlayers that will be synchronized.
    private VideoPlayer[] videoPlayers;

    // Flag indicating if a request is ready.
    public bool IsReady => isReady;
    private bool isReady;

    // Holds the last prepared request.
    private TrackRequest requestData;

    private TrackingManagerRpc trackingManagerRpc;

    void Awake()
    {
        SingletonManager.Instance.Register<TrackingManager>(this);
    }

    void Start()
    {
        trackingManagerRpc = new(this);
        // Automatically find all VideoPlayer components in the scene.
        videoPlayers = FindObjectsOfType<VideoPlayer>();
        Invoke("RequestReady", 1.0f);


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
        // Create a new track entry.
        TrackEntry newEntry = new TrackEntry
        {
            id = 0, // placeholder id.
            c = new float[] { location.x, location.y },
            src = source
        };

        // Add the new entry to the list.
        trackEntries.Add(newEntry);

        // For testing, serialize the entry to JSON and print to console.
        string jsonEntry = JsonConvert.SerializeObject(newEntry);
        Debug.Log("Added track entry: " + jsonEntry);
    }

    /// <summary>
    /// Synchronizes all found VideoPlayers:
    /// - Finds the lowest current frame among them.
    /// - Pauses all VideoPlayers and sets their frame to the lowest frame.
    /// Returns the synchronized (lowest) frame.
    /// </summary>
    public long GetSynchronizedCurrentFrame()
    {
        if (videoPlayers == null || videoPlayers.Length == 0)
        {
            Debug.LogWarning("No video players found for synchronization.");
            return 0;
        }

        // Find the lowest frame among all VideoPlayers.
        long lowestFrame = long.MaxValue;
        foreach (var vp in videoPlayers)
        {
            if (vp != null)
            {
                lowestFrame = Math.Min(lowestFrame, vp.frame);
            }
        }

        // Pause all VideoPlayers and set their frame to the lowest found.
        foreach (var vp in videoPlayers)
        {
            if (vp != null)
            {
                vp.Pause();
                vp.frame = lowestFrame;
            }
        }
        Debug.Log("Synchronized video players to frame: " + lowestFrame);

        return lowestFrame;
    }

    /// <summary>
    /// Prepares a request by synchronizing video players to get the current frame,
    /// then creates a TrackRequest struct that contains the frame id and all track entries.
    /// Sets isReady to true.
    /// </summary>
    public void RequestReady()
    {
        long currentFrame = GetSynchronizedCurrentFrame();
        requestData = new TrackRequest
        {
            frame_id = currentFrame,
            coords = new List<TrackEntry>(trackEntries)
        };
        isReady = true;
        Debug.Log("Request ready: " + JsonConvert.SerializeObject(requestData));
    }

    /// <summary>
    /// Returns the prepared TrackRequest struct and resets the isReady flag.
    /// </summary>
    public TrackRequest GetRequest()
    {
        isReady = false;
        Debug.Log("Request retrieved: " + JsonConvert.SerializeObject(requestData));
        return requestData;
    }

    public void OnReceive(UpdateResult updateResult)
    {
        string jsonResult = JsonConvert.SerializeObject(updateResult, Formatting.Indented);
        Debug.Log(jsonResult);
    }
}


