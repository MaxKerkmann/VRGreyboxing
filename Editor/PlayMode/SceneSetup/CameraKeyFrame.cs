using UnityEngine;

namespace VRGreyboxing
{
    /**
     * Keyframe component to save camera movement data
     */
    [System.Serializable]
    public class CameraKeyFrame
    {
        public CameraKeyFrame prevKeyFrame;
        public Vector3 cameraPosition;
        public Quaternion cameraRotation;
        public float cameraMoveTime;
        public float cameraRotateTime;
    }
}
