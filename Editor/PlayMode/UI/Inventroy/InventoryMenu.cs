using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
namespace VRGreyboxing
{
    /**
     * Inventory display
     */
    public class InventoryMenu : MonoBehaviour
    {
        private List<PrefabPreviewTarget> _pendingPreviews = new();
        public GameObject inventoryOptionButtonPrefab;
        public int previewImageCheckAttempts;

        
        [System.Serializable]
        public struct PrefabPreviewTarget
        {
            public GameObject prefab;
            public Image renderer;
            public int retryCount;
        }
        
        /**
         * Fill inventory menu with all prefabs in the editor data
         */
        public void FillInventoryMenu()
        {
            _pendingPreviews = new List<PrefabPreviewTarget>();
            GameObject content = gameObject.GetComponentInChildren<GridLayoutGroup>().gameObject;

            for (int i = 0;i<PlayModeManager.Instance.editorDataSO.availablePrefabs.Count;i++)
            {
                var availablePrefab = PlayModeManager.Instance.editorDataSO.availablePrefabs[i];
                GameObject prefabButton = Instantiate(inventoryOptionButtonPrefab, content.transform);
                prefabButton.AddComponent<PrefabSpawnButton>();
                prefabButton.GetComponent<PrefabSpawnButton>().prefab = availablePrefab;
                prefabButton.name = availablePrefab.name;
                prefabButton.GetComponentInChildren<Text>().text = availablePrefab.name;
                prefabButton.GetComponentInChildren<Text>().gameObject.transform.localPosition = new Vector3(
                    prefabButton.GetComponentInChildren<Text>().gameObject.transform.localPosition.x, -50,
                    prefabButton.GetComponentInChildren<Text>().gameObject.transform.localPosition.z);
                Texture2D preview = AssetPreview.GetAssetPreview(availablePrefab); 
                if (preview != null)
                {
                    ApplyPreviewSprite(prefabButton.GetComponentInChildren<Image>(), preview);
                }
                else
                {
                    PrefabPreviewTarget prefabTarget = new PrefabPreviewTarget
                    {
                        prefab = availablePrefab,
                        renderer = prefabButton.GetComponentInChildren<Image>()
                    };
                    _pendingPreviews.Add(prefabTarget);
                }
                prefabButton.transform.localScale = Vector3.one;

            }
            
            if (_pendingPreviews.Count > 0)
            {
                EditorApplication.update += CheckPreviewStatus;
            }
        }
        
        /**
         * Check if preview image of prefab is loaded until configured retry count is reached
         */
        private void CheckPreviewStatus()
        {
            for (int i = _pendingPreviews.Count - 1; i >= 0; i--)
            {
                var target = _pendingPreviews[i];

                Texture2D preview = AssetPreview.GetAssetPreview(target.prefab);
                if (preview != null)
                {
                    ApplyPreviewSprite(target.renderer, preview);
                    _pendingPreviews.RemoveAt(i);
                }        
                else
                {
                    target.retryCount++;
                    _pendingPreviews[i] = target;

                    if (target.retryCount > previewImageCheckAttempts)
                    {
                        target.renderer.enabled = true;
                        target.renderer.gameObject.transform.localScale = Vector3.one;
                        _pendingPreviews.RemoveAt(i);
                    }
                }
            }

            if (_pendingPreviews.Count == 0)
            {
                EditorApplication.update -= CheckPreviewStatus;
            }
        }
        
        /**
         * Set preview image of prefab in menu
         */
        private void ApplyPreviewSprite(Image image, Texture2D texture)
        {
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 2);
            image.sprite = sprite;
            image.GetComponent<RectTransform>().localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            EditorApplication.update -= CheckPreviewStatus;
        }
    }
}
#endif