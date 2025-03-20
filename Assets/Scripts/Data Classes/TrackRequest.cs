using System.Collections.Generic;


/// <summary>
/// Struct representing a track request.
/// Example JSON format: {"frame_id":7200, "coords": [{"id":5, "c":[x,y], "src":0}, ...] }
/// </summary>
public class TrackRequest
{
    public long frame_id;
    public List<TrackEntry> coords;
}

/// <summary>
/// Data class representing a track entry.
/// Example JSON format: {"id":0, "c":[x,y], "src":source}
/// </summary>
public class TrackEntry
{
    public int id;
    public float[] c;
    public int src;
}

