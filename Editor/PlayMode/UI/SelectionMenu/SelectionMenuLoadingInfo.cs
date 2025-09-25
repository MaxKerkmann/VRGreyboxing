using UnityEngine;
using UnityEngine.UI;

namespace VRGreyboxing
{
    
    /**
     * Display progress bar to open selection menu
     */
    public class SelectionMenuLoadingInfo : MonoBehaviour
    {
        public Image loadingCircle;

        public void DisplayLoadingProgress(float currentTime, float maxTime)
        {
            loadingCircle.fillAmount = 1 - currentTime / maxTime;
        }
    }
}

