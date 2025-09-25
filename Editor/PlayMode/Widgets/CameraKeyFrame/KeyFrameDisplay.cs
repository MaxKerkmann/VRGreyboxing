using System;
using UnityEngine;

namespace VRGreyboxing
{
    /**
     * Connect camera keyframes visually
     */
    public class KeyFrameDisplay : MonoBehaviour
    {
        public int keyFrameIndex;
        public GameObject prevKeyFrameDisplay;
        public LineRenderer lineRenderer;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position);
        }

        private void Update()
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, prevKeyFrameDisplay != null ? prevKeyFrameDisplay.transform.position : lineRenderer.GetPosition(1));
        }
    }
}
