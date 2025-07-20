using System.Collections.Generic;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;

namespace VRGreyboxing
{
    public class EditorDataSO : ScriptableObject
    {
        [Header("Universal Data")]
        public Object prefabSaveDirectory;
        public List<GameObject> availablePrefabs;
        public bool setupTags;
        public List<string> tags;

        [Header("Editor Data")] 
        public GameObject editorBasePrefab;
        public bool usingBuildScenesOnly;
        public bool usingGreyboxingEditor;
        public Material createdObjectMaterial;
        public Material drawingMaterial;
        
        [Header("PlayMode Data")]
        public GameObject sceneCleanUpPrefab;
        public string lastOpenScene;
        public List<string> sceneNames;
        public List<ObjectBaseState> objectStates;
        public int zoomMode;
        public int rotationMode;
        
        public float minimumZoom;
        public float maximumZoom;
        public float zoomStep;
        public float startingSize;

    }
}
