using System.Collections.Generic;
using Unity.XR.CoreUtils.Collections;
using UnityEditor;
using UnityEngine;

namespace VRGreyboxing
{
    /**
     * Persistent data between edit and play mode
     */
    public class EditorDataSO : ScriptableObject
    {
        [Header("Universal Data")]
        public Object prefabSaveDirectory;
        public List<DefaultAsset> prefabSourceDirectories;
        public DefaultAsset defaultPrefabFolder;
        public List<GameObject> availablePrefabs;
        public bool setupTags;
        public List<string> tags;

        [Header("Editor Data")] 
        public GameObject editorBasePrefab;
        public bool usingBuildScenesOnly;
        public EditorBuildSettingsScene[] originalBuildScenes;
        public bool usingGreyboxingEditor;
        public Material createdObjectMaterial;
        public Material drawingMaterial;
        
        [Header("PlayMode Data")]
        public GameObject sceneCleanUpPrefab;
        public string lastOpenScene;
        public List<string> sceneNames;
        public List<ObjectBaseState> objectStates;
        
        [Header("Usage Modes")]
        public ZoomMode zoomMode;
        public RotationMode rotationMode;
        public bool restrictToStickMovement;
        public bool restrictToTeleport;
        public bool enableStickLeaning;
        public bool enableTeleportRotationLeaning;
        public bool oneHandGrabRotation;
        public bool performLateTeleport;
        public float widgetScaleSize;
        
        [Header("Navigation Settings")]
        public float minimumZoom;
        public float maximumZoom;
        public float zoomStep;
        public float startingSize;
        public float rotationSpeed;
        public float movementSpeed;
        public float leaningSpeed;
        public float zoomMoveDistancePerStep;
        public float maxTeleportDistance;
        public float freeRotationCenterDistance;
        public float teleportationRotationDistance;
        public float zoomMenuTime;

        [Header("Usage Times")] 
        public int restrictedRotation;
        public int unrestrictedRotation;
        public int teleportRotation;
        public int gestureZoom;
        public int menuZoom;
        public int redo;
        public int undo;
    }

    public enum RotationMode
    {
        Restricted = 0,
        Unrestricted = 1,
        Teleport = 2
    }

    public enum ZoomMode
    {
        Gesture = 0,
        Menu = 1
    }
}
