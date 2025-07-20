using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
namespace VRGreyboxing
{
    public class InventoryMenu : MonoBehaviour
    {
        private List<PrefabPreviewTarget> _pendingPreviews = new();
        public GameObject inventoryOptionButtonPrefab;

        
        [System.Serializable]
        public struct PrefabPreviewTarget
        {
            public GameObject prefab;
            public SpriteRenderer renderer;
            public int retryCount;
        }
        
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
                Texture2D preview = AssetPreview.GetAssetPreview(availablePrefab); 
                if (preview != null)
                {
                    ApplyPreviewSprite(prefabButton.GetComponentInChildren<SpriteRenderer>(), preview);
                }
                else
                {
                    PrefabPreviewTarget prefabTarget = new PrefabPreviewTarget
                    {
                        prefab = availablePrefab,
                        renderer = prefabButton.GetComponentInChildren<SpriteRenderer>()
                    };
                    _pendingPreviews.Add(prefabTarget);
                }

            }
            
            if (_pendingPreviews.Count > 0)
            {
                EditorApplication.update += CheckPreviewStatus;
            }
        }
        
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

                    if (target.retryCount > 5)
                    {
                        target.renderer.gameObject.GetComponent<SpriteRenderer>().enabled = true;
                        target.renderer.gameObject.transform.localScale = Vector3.one * 60;
                        _pendingPreviews.RemoveAt(i);
                    }
                }
            }

            if (_pendingPreviews.Count == 0)
            {
                EditorApplication.update -= CheckPreviewStatus;
            }
        }
        private void ApplyPreviewSprite(SpriteRenderer srenderer, Texture2D texture)
        {
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 2);
            srenderer.sprite = sprite;
        }

        private void OnDestroy()
        {
            EditorApplication.update -= CheckPreviewStatus;
        }
    }
}
#endif