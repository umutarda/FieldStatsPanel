public struct UpdateResult
{
    public long lost_frame_id;
    public FrameTrackingData[] tracks;
    public int[] lost_ids;
}

public struct FrameTrackingData
{
    public int fr;
    public TrackObject[] obj;
}

public struct TrackObject
{
    public int id;
    public int cls_id;
    public float[] c;
    public int src;
}
