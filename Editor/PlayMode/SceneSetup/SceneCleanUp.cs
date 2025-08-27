using System;
using UnityEngine;

namespace VRGreyboxing
{
    [DefaultExecutionOrder(-10000)]
    public class SceneCleanUp : MonoBehaviour
    {
        private void Awake()
        {
            PlayModeManager.Instance.StripComponentsInCurrentScene();
            PlayModeManager.Instance.PlaceWorldScaler();
        }
    }
}