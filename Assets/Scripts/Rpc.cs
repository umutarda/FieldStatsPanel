using UnityEngine;
using AustinHarris.JsonRpc;

class Rpc : JsonRpcService
{
    [JsonRpcMethod]
    void Say(string message)
    {
        Debug.Log($"You sent {message}");
    }
}