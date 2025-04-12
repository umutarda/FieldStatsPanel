using System;

[System.Serializable]
public class UpdateResult
{
    public long lost_frame_id;
    public FrameTrackingData[] tracks;
    public int[] lost_ids;
}
[System.Serializable]
public class FrameTrackingData
{
    public int fr;
    public TrackObject[] obj;
    public FrameTrackingData() 
    {

    }
    public FrameTrackingData(FrameTrackingData ftd)
    {
        this.fr = ftd.fr;
        this.obj = new TrackObject[ftd.obj.Length];
        Array.Copy(ftd.obj,this.obj,ftd.obj.Length);
    }
}

[System.Serializable]
public class TrackObject
{
    public int id;
    public int cls_id;
    public float[] c;
    public int src;
}
