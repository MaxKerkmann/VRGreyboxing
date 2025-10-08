using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRGreyboxing
{
    /**
     * Parent to all objects in a scene to apply global scaling
     */
    public class WorldScaler : MonoBehaviour
    {
        private static readonly int WorldScale = Shader.PropertyToID("_WorldScale");
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

        /**
         * Refresh as parent of all objects and apply localscale
         */
        private void ApplyScale()
        {
            Scene scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                root.transform.SetParent(transform);
                ApplyShaderChange(root);
            }
            transform.localScale = Vector3.one * scale;
        }

        /**
         * Update shader variable on all scaled objects to keep texture scaling
         */
        private void ApplyShaderChange(GameObject go)
        {
            if(go.GetComponent<Renderer>() != null)
                go.GetComponent<Renderer>().sharedMaterial.SetFloat(WorldScale,scale);
            for (int i = 0; i < go.transform.childCount; i++)
            {
                ApplyShaderChange(go.transform.GetChild(i).gameObject);
            }
        }


        /**
         * Set new world scale
         */
        public void SetScale(float newScale)
        {
            scale = newScale;
            ApplyScale();
        }

        /**
         * Save when destroyed in current update cycle
         */
        private void OnDestroy()
        {
            destroyed = true;
        }
    }
}