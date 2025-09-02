using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;

namespace VRGreyboxing
{
    /**
     * Persistent id to identify objects in scenes
     */
    [ExecuteAlways]
    public class PersistentID : MonoBehaviour
    {
        public string uniqueId;


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(uniqueId) && IsGuid(uniqueId)) return;

            uniqueId = Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
        }

        private bool IsGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }
#endif
    }
}