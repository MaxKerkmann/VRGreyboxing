using UnityEngine;

namespace VRGreyboxing
{
    /**
     * Trigger scene setup before any other logic in scene
     */
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