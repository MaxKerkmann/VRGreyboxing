using UnityEngine;

namespace VRGreyboxing
{
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
