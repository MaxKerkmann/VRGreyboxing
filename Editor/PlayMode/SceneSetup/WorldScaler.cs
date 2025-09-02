using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRGreyboxing
{
    public class WorldScaler : MonoBehaviour
    {
        [Range(0.1f, 5f)] public float scale = 1f;

        private float _lastScale = 1f;
        public bool destroyed;

        void Update()
        {
            if (Mathf.Abs(scale - _lastScale) > 0.001f)
            {
                ApplyScale();
                _lastScale = scale;
            }
        }

        public void ApplyScale()
        {
            Scene scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                root.transform.SetParent(transform);
            }
            transform.localScale = Vector3.one * scale;
        }


        public void SetScale(float newScale)
        {
            scale = newScale;
            ApplyScale();
        }

        private void OnDestroy()
        {
            destroyed = true;
        }
    }
}