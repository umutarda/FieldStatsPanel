using System.Collections.Generic;


/// <summary>
/// Struct representing a track request.
/// Example JSON format: {"frame_id":7200, "coords": [{"id":5, "c":[x,y], "src":0}, ...] }
/// </summary>
public struct TrackRequest
{
    public long frame_id;
    public List<TrackEntry> coords;
}