using AustinHarris.JsonRpc;
using Newtonsoft.Json.Linq;

public class TrackingManagerRpc : JsonRpcService
{
    private TrackingManager trackingManager;

    // Constructor that accepts a TrackingManager instance.
    public TrackingManagerRpc(TrackingManager trackingManager)
    {
        this.trackingManager = trackingManager;
    }

    [JsonRpcMethod]
    public bool IsReady()
    {
        return trackingManager.IsReady;
    }

    [JsonRpcMethod]
    public TrackRequest GetRequest()
    {
        return trackingManager.GetRequest();
    }

    [JsonRpcMethod]
    public void OnReceive(JObject updateResult)
    {
        // Convert the JObject to an UpdateResult object.
        UpdateResult _updateResult = updateResult.ToObject<UpdateResult>();
        trackingManager.OnReceive(_updateResult);
    }
}
