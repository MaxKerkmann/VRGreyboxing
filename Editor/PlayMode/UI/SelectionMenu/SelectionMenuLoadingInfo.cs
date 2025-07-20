using UnityEngine;
using UnityEngine.UI;

namespace VRGreyboxing
{
    
    public class SelectionMenuLoadingInfo : MonoBehaviour
    {
        public Image loadingCircle;

        public void DisplayLoadingProgress(float currentTime, float maxTime)
        {
            loadingCircle.fillAmount = 1 - currentTime / maxTime;
        }
    }
}

