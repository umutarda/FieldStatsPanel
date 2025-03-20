using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VideoControlSlider : Slider
{

    protected override void Awake()
    {
        base.Awake();
        if (SingletonManager.Instance != null)
            SingletonManager.Instance.Register<VideoControlSlider>(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (SingletonManager.Instance != null)
            SingletonManager.Instance.Unregister<VideoControlSlider>(this);
    }



}
