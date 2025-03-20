// Classes to match the JSON structure
using System.Collections.Generic;

public class YoloData
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
