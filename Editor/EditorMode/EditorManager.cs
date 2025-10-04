#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.ProBuilder;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.SceneManagement;
using EditorUtility = UnityEditor.EditorUtility;
using LightType = UnityEngine.LightType;
using Object = UnityEngine.Object;

namespace VRGreyboxing
{
    /**
     * Manager class holding main logic for edit mode processes
     */
    [InitializeOnLoad]
    public static class EditorManager
    {

        private static List<string> _sceneIDs;
        private static GameObject cam;
        
        private static readonly HashSet<Type> AllowedPrefabTypes = new HashSet<Type>
        {
            typeof(Transform),
            typeof(MeshRenderer),
            typeof(MeshFilter)
        };
        
        public static EditorDataSO editorDataSo;
        
        private static float startTime;
        private static float currentRuntime;
        private static float moveTime;
        private static float rotateTime;
        private static Vector3 originalPosition;
        private static Quaternion originalRotation;
        private static float originalDistance;
        private static List<CameraKeyFrame> keyframes;
        private static int keyframeIndex;
        private static Vector3 camStartPos;
        private static Quaternion camStartRot;
        

        static EditorManager()
        {
            editorDataSo = AssetDatabase.LoadAssetAtPath<EditorDataSO>("Packages/com.bcatstudio.vrgreyboxing/EditorData.asset");
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
        
        /**
         * Start greyboxing process
         */
        public static void StartGreyboxing()
        {
            if (EditorApplication.isPlaying) return;

            //Setup tags
            if (!editorDataSo.setupTags)
            {
                foreach (var tag in editorDataSo.tags)
                {
                    SerializedObject tagManager =
                        new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                    SerializedProperty tagsProp = tagManager.FindProperty("tags");
                    bool exsists = false;
                    for (int i = 0; i < tagsProp.arraySize; i++)
                    {
                        SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                        if (t.stringValue.Equals(tag)) exsists = true;
                    }

                    if (!exsists)
                    {
                        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                        tagManager.ApplyModifiedProperties();
                    }
                }
                editorDataSo.setupTags = true;
            }
            
            //Initialize data
            editorDataSo.lastOpenScene = SceneManager.GetActiveScene().path;
            editorDataSo.usingGreyboxingEditor = true;
            editorDataSo.objectStates = new List<ObjectBaseState>();
            editorDataSo.availablePrefabs = GetAvailablePrefabs();
            editorDataSo.sceneNames = GetAllScenes();
            
            PrepareScenes(false);
            
            //Create initial hub scene
            Scene baseScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            Transform obj = ((GameObject)PrefabUtility.InstantiatePrefab(editorDataSo.editorBasePrefab)).transform;
            SceneManager.MoveGameObjectToScene(obj.gameObject,baseScene);
            
            GameObject light = new GameObject("Light");
            SceneManager.MoveGameObjectToScene(light,baseScene);
            Light l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            light.transform.Rotate(new Vector3(90, 0, 0));
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.localScale = new Vector3(20, 20, 20);
            SceneManager.MoveGameObjectToScene(plane,baseScene);

            
            obj.position = Vector3.zero;
            ResetUsageTimes();
            
            EditorApplication.EnterPlaymode();
        }

        private static void ResetUsageTimes()
        {
            editorDataSo.restrictedRotation = 0;
            editorDataSo.unrestrictedRotation = 0;
            editorDataSo.teleportRotation = 0;
            editorDataSo.gestureZoom = 0;
            editorDataSo.menuZoom = 0;
            editorDataSo.undo = 0;
            editorDataSo.redo = 0;
        }

        /**
         * Get all prefabs in project file or just those in configured folders
         */
        private static List<GameObject> GetAvailablePrefabs()
        {
            List<GameObject> result = new List<GameObject>(2);
            List<string> temp = new List<string>();
            if (editorDataSo.prefabSourceDirectories.Count == 0)
            {
                temp = AssetDatabase.GetAllAssetPaths().ToList();
            }
            else
            {
                List<DefaultAsset> directories = editorDataSo.prefabSourceDirectories.ToList();
                directories.Add(editorDataSo.defaultPrefabFolder);
                foreach (var folder in directories)
                {
                    List<string> guids = AssetDatabase.FindAssets("", new[] { AssetDatabase.GetAssetPath(folder) }).ToList();
                    foreach (string guid in guids)
                    {
                        temp.Add(AssetDatabase.GUIDToAssetPath(guid));
                    }
                }
                
            }
            List<string> names = new List<string>();
            foreach ( string s in temp ) {
                if ( s.Contains( ".prefab" ) ) names.Add( s );
            }
            
            foreach (var prefab in names)
            {
                try
                {
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
                    if (go == null || go.CompareTag("VRG_WidgetCube") || go.CompareTag("VRG")) continue;

                    if (go.GetComponentsInChildren<Component>().Where(c => AllowedPrefabTypes.Contains(c.GetType()))
                            .ToList().Count >= AllowedPrefabTypes.Count)
                    {
                        if (!result.Contains(go))
                            result.Add(go);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Skipping faulty prefab: "+e.Message);
                }
            }
            result.Sort((a, b) =>
            {
                bool aHasTag = a.CompareTag("VRG_SpawnableObject");
                bool bHasTag = b.CompareTag("VRG_SpawnableObject");

                if (aHasTag == bHasTag) return 0;   
                return aHasTag ? -1 : 1;           
            });
            
            GameObject cameraFigure = result.Find(go => go.CompareTag("VRG_CameraFigure"));
            if (cameraFigure != null)
            {
                result.Remove(cameraFigure);
                int insertIndex = result.FindLastIndex(go => go.CompareTag("VRG_SpawnableObject")) + 1;
                result.Insert(insertIndex, cameraFigure);
            }
            return result;
        }
        
        /**
         * Find all scene names in project
         */
        private static List<string> GetAllScenes()
        {
            List<string> scenes = new List<string>();

            List<string> buildScenePaths = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    buildScenePaths.Add(scene.path);
            }

            string[] allScenePaths = Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories);

            foreach (string path in allScenePaths)
            {
                string convertpath = path.Replace(@"\","/");
                bool isInBuild = buildScenePaths.Contains(convertpath);

                if (!editorDataSo.usingBuildScenesOnly || isInBuild)
                {
                    string sceneName = Path.GetFileNameWithoutExtension(convertpath);
                    scenes.Add(sceneName);
                }
            }

            return scenes;
        }

        /**
         * When play mode ends reset scenes and apply saved changes
         **/
        private static void OnPlayModeChanged(PlayModeStateChange newState)
        {
            if (newState == PlayModeStateChange.EnteredEditMode && editorDataSo.usingGreyboxingEditor)
            {
                ApplySceneChanges();
                PrepareScenes(true);
                Scene scene = EditorSceneManager.OpenScene(editorDataSo.lastOpenScene);
                editorDataSo.usingGreyboxingEditor = false;
            }

            if (newState == PlayModeStateChange.ExitingPlayMode && editorDataSo.usingGreyboxingEditor)
            {
                PlayModeManager.Instance.ResetWorldScale();
            }
        }
        
        /**
         * Apply all saves changes from greyboxing phase in vr to scenes in edit mode
         **/
        private static void ApplySceneChanges()
        {
            _sceneIDs = AssetDatabase.FindAssets("t:Scene").ToList();
            List<Scene> scenes = GetScenesFromGUIDs(_sceneIDs,editorDataSo.usingGreyboxingEditor);
            
            
            foreach (Scene scene in scenes)
            {
                SceneManager.LoadScene(scene.path,LoadSceneMode.Single);
                List<ObjectBaseState> RemovedKeys = new List<ObjectBaseState>();
                foreach (var objectState in editorDataSo.objectStates)
                {
                    bool found = false;
                    if (objectState.disabled || objectState.deleted)
                    {
                        found = true;
                    }
                    else if (objectState is SpawnedObject spawnedObject)
                    {
                        if(spawnedObject.originalScenePath != scene.path) continue;
                        found = true;
                        SpawnObject(spawnedObject,scene);
                    }else if (objectState is CreatedObject createdObject)
                    {
                        if(createdObject.originalScenePath != scene.path) continue;
                        found = true;
                        CreateObject(createdObject,scene);
                    }
                    else if (objectState is MarkerObject markerObject)
                    {
                        if(markerObject.originalScenePath != scene.path) continue;
                        found = true;
                        DrawObject(markerObject,scene);
                    }
                    
                    if(found)
                        RemovedKeys.Add(objectState);
                }
                
                foreach (GameObject obj in scene.GetRootGameObjects())
                {
                    ApplyChangesRecursively(obj);   
                }
                
                foreach (var removedKey in RemovedKeys)
                {
                    editorDataSo.objectStates.Remove(removedKey);
                }
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        /**
         * Spawn object from available prefabs by saved index
         */
        private static void SpawnObject(SpawnedObject spawnedObject, Scene scene)
        {
            if (spawnedObject.prefabIndex == -1) return;
            Transform obj = ((GameObject)PrefabUtility.InstantiatePrefab(editorDataSo.availablePrefabs[spawnedObject.prefabIndex],scene)).transform;
            PersistentID persistentID = obj.gameObject.AddComponent<PersistentID>();
            persistentID.uniqueId = spawnedObject.persisentID;
            obj.SetPositionAndRotation(spawnedObject.position, spawnedObject.rotation);
            obj.localScale = spawnedObject.scale;
            if (obj.GetComponentInChildren<ProBuilderMesh>() != null)
            {
                var pbm = obj.GetComponentInChildren<ProBuilderMesh>();
                pbm.MakeUnique();
                pbm.ToMesh();
                pbm.Refresh();
            }
            if (spawnedObject.alteredPositions.Count > 0)
            {
                ProBuilderMesh pbm = obj.GetComponent<ProBuilderMesh>();
                pbm.positions = spawnedObject.alteredPositions.ToArray();
                pbm.ToMesh();
                pbm.Refresh();
            }
            if (spawnedObject.keyFrames != null)
            {
                obj.gameObject.GetComponent<CameraFigure>().keyFrames = spawnedObject.keyFrames;
                EditorUtility.SetDirty( obj.gameObject.GetComponent<CameraFigure>());
            }
        }

        /**
         * Create costum object by saved edge points
         */
        private static void CreateObject(CreatedObject createdObject, Scene scene)
        {
            GameObject go = new GameObject("CreatedObject");
            PersistentID persistentID = go.AddComponent<PersistentID>();
            persistentID.uniqueId = createdObject.persisentID;
            SceneManager.MoveGameObjectToScene(go,scene);
            go.transform.SetPositionAndRotation(createdObject.position, createdObject.rotation);
            go.transform.localScale = createdObject.scale;
            MeshCollider col = go.AddComponent<MeshCollider>();
            col.convex = true;
            ProBuilderMesh pbm = go.AddComponent<ProBuilderMesh>();
            pbm.CreateShapeFromPolygon(createdObject.basePositions,1f,false);
            Vector3 center = Vector3.zero;
            foreach (Vector3 pos in createdObject.alteredPositions)
            {
                center += pos;
            }
            center /= createdObject.alteredPositions.Count;
            for (int i = 0; i < createdObject.alteredPositions.Count; i++)
            {
                createdObject.alteredPositions[i] -= center;
            }
            pbm.positions = createdObject.alteredPositions.ToArray();
            go.transform.position += center;
            go.GetComponent<MeshRenderer>().material = editorDataSo.createdObjectMaterial;
            if (createdObject.flippedVertices)
            {
                foreach (var face in pbm.faces)
                {
                    face.Reverse();
                }
            }
            pbm.ToMesh();
            pbm.Refresh();
            pbm.Optimize();
            if (createdObject.newParentID != "")
            {
                GameObject obj = Object.FindObjectsByType<PersistentID>(FindObjectsSortMode.None).FirstOrDefault(id => id.uniqueId == createdObject.newParentID)?.gameObject;
                if (obj != null) go.transform.SetParent(obj.transform);
            }

            go.transform.localRotation = Quaternion.identity;
        }

        /**
         * Recreate drawing with saved positions from the line renderer
         */
        private static void DrawObject(MarkerObject markerObject, Scene scene)
        {
            GameObject go = new GameObject("DrawContainer");
            PersistentID persistentID = go.AddComponent<PersistentID>();
            persistentID.uniqueId = markerObject.persisentID;
            go.tag = "VRG_Mark";
            SceneManager.MoveGameObjectToScene(go,scene);
            go.transform.SetPositionAndRotation(markerObject.position, markerObject.rotation);
            go.transform.localScale = markerObject.scale;
            BoxCollider containerCollider = go.AddComponent<BoxCollider>();
            List<BoxCollider> drawingColliders = new List<BoxCollider>();
            for(int i = 0; i < markerObject.markPoints.Count; i++)
            {
                GameObject drawing = new GameObject("Drawing " + i+1);
                drawing.tag = "VRG_Mark";
                drawing.transform.parent = go.transform;
                
                LineRenderer lineRenderer = drawing.AddComponent<LineRenderer>();
                lineRenderer.useWorldSpace = false;
                lineRenderer.positionCount = markerObject.markPoints[i].Count;
                lineRenderer.SetPositions(markerObject.markPoints[i].Select(v => v+markerObject.drawingOffsets[i]).ToArray());
                lineRenderer.sharedMaterial = editorDataSo.drawingMaterial;
                lineRenderer.startColor = lineRenderer.endColor = markerObject.colors[i];
                lineRenderer.startWidth = lineRenderer.endWidth = markerObject.lineWidths[i];
                
                BoxCollider boxCollider = drawing.AddComponent<BoxCollider>();
                boxCollider.center = markerObject.colliderCenters[i]+markerObject.drawingOffsets[i];
                boxCollider.size = markerObject.colliderSizes[i];
                drawingColliders.Add(boxCollider);
                drawing.transform.localPosition = Vector3.zero;

            }

            Bounds combinedBounds = drawingColliders[0].bounds;
            for (int i = 1; i < drawingColliders.Count; i++)
            {
                combinedBounds.Encapsulate(drawingColliders[i].bounds);
            }
            containerCollider.center = combinedBounds.center;
            containerCollider.size = combinedBounds.size;
            containerCollider.enabled = false;
        }

        
        /**
         * Apply changes made to already existing objects in scenes
         */
        private static void ApplyChangesRecursively(GameObject root)
        {
            PersistentID persistentID = root.GetComponent<PersistentID>();
            if(persistentID == null) return;

            foreach (var key in editorDataSo.objectStates.Where(state => state is SpawnedObject && !state.disabled))
            {
                SpawnedObject duplication = key as SpawnedObject;
                if (duplication != null && duplication.basePersistentID == persistentID.uniqueId)
                {
                    GameObject obj = Object.Instantiate(root);
                    PersistentID persID = obj.gameObject.AddComponent<PersistentID>();
                    persID.uniqueId = duplication.persisentID;
                    SceneManager.MoveGameObjectToScene(obj,root.scene);
                    Transform objTrans = obj.transform;
                    obj.name = "Duplicate of " + root.name;
                    objTrans.SetPositionAndRotation(duplication.position, duplication.rotation);
                    objTrans.localScale = duplication.scale;
                    if (obj.GetComponentInChildren<ProBuilderMesh>() != null)
                    {
                        var pbm = obj.GetComponentInChildren<ProBuilderMesh>();
                        pbm.MakeUnique();
                        pbm.ToMesh();
                        pbm.Refresh();
                    }
                    if (duplication.alteredPositions.Count > 0)
                    {
                        obj.GetComponent<ProBuilderMesh>().positions = duplication.alteredPositions.ToArray();
                    }
                    if (duplication.keyFrames != null)
                    {
                        obj.gameObject.GetComponent<CameraFigure>().keyFrames = duplication.keyFrames;
                    }
                }
            }

            if (persistentID != null && editorDataSo.objectStates.FirstOrDefault(state => state.persisentID == persistentID.uniqueId) != null)
            {
                AlteredObject alteredObject = editorDataSo.objectStates.First(state => state.persisentID == persistentID.uniqueId) as AlteredObject;
                if (alteredObject != null)
                {
                    if (alteredObject.deleted)
                    {
                        editorDataSo.objectStates.Remove(editorDataSo.objectStates.FirstOrDefault(state => state.persisentID == persistentID.uniqueId));
                        Object.DestroyImmediate(root.gameObject);
                        return;
                    }

                    if (alteredObject.alteredPositions.Count > 0)
                    {
                        ProBuilderMesh pbm = root.gameObject.GetComponent<ProBuilderMesh>();
                        pbm.positions = alteredObject.alteredPositions.ToArray();
                        pbm.ToMesh();
                        pbm.Refresh();
                    }
                    root.transform.SetPositionAndRotation(alteredObject.position, alteredObject.rotation);
                    root.transform.localScale = alteredObject.scale;
                    if (alteredObject.keyFrames != null)
                    {
                        root.gameObject.GetComponent<CameraFigure>().keyFrames = alteredObject.keyFrames;
                        EditorUtility.SetDirty( root.gameObject.GetComponent<CameraFigure>());
                    }
                }
            }
            editorDataSo.objectStates.Remove(editorDataSo.objectStates.FirstOrDefault(state => state.persisentID == persistentID.uniqueId));
            
            foreach (Transform child in root.transform)
            {
                ApplyChangesRecursively(child.gameObject);
            }
        }

        /**
         * Iterate through scenes and prepare them for play- or edit-mode
         */
        private static void PrepareScenes(bool remove)
        {
            _sceneIDs = AssetDatabase.FindAssets("t:Scene").ToList();
            
            if (!editorDataSo.usingBuildScenesOnly)
            {
                if (remove)
                {
                    EditorApplication.delayCall += () => EditorBuildSettings.scenes = editorDataSo.originalBuildScenes;
                }
                else
                {
                    editorDataSo.originalBuildScenes = EditorBuildSettings.scenes;
                    var allScenes = _sceneIDs.Select(scene => new EditorBuildSettingsScene(AssetDatabase.GUIDToAssetPath(scene), true)).ToArray();
                    EditorBuildSettings.scenes = allScenes;
                }
            }
            
            List<Scene> scenes = GetScenesFromGUIDs(_sceneIDs,editorDataSo.usingGreyboxingEditor);
            foreach (Scene scene in scenes)
            {

                if (remove)
                    RemoveSceneCleanUp(scene);
                else
                    PrefabUtility.InstantiatePrefab(editorDataSo.sceneCleanUpPrefab, scene);

                foreach (GameObject obj in scene.GetRootGameObjects())
                {
                    if (remove)
                        RemoveIDsRecursive(obj.transform);
                    else
                        AssignIDsRecursive(obj.transform);
                }


                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (remove)
                {
                  EditorSceneManager.CloseScene(scene, true); 
                } 
            }
        }

        private static List<Scene> GetScenesFromGUIDs(List<string> guids, bool useOnlyBuildScenes)
        {
            List<Scene> scenes = new List<Scene>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (useOnlyBuildScenes)
                {
                    int index = SceneUtility.GetBuildIndexByScenePath(path);
                    if (index < 0)
                    {
                        continue;
                    }
                }
                UnityEditor.PackageManager.PackageInfo packageInfo =
                    UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
                if (packageInfo != null)
                {
                    if (packageInfo.source != PackageSource.Embedded ||
                        packageInfo.source != PackageSource.Local) continue;
                }
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if(scene.isLoaded)
                    scenes.Add(scene);
            }
            return scenes;
        }

        /**
         * Add persistent id to all objects in scenes
         */
        private static void AssignIDsRecursive(Transform root)
        {
            var id = root.GetComponent<PersistentID>();
            if (id == null)
            {
                id = root.gameObject.AddComponent<PersistentID>();
            }

            if (string.IsNullOrEmpty(id.uniqueId))
            {
                id.uniqueId = Guid.NewGuid().ToString();
                EditorUtility.SetDirty(id);
            }

            if(root.CompareTag("VRG_Mark")) return;
            
            foreach (Transform child in root)
            {
                AssignIDsRecursive(child);
            }
        }

        /**
         * Remove persistent id of objects after returning to edit mode
         */
        private static void RemoveIDsRecursive(Transform root)
        {
            var id = root.GetComponent<PersistentID>();
            if (id != null)
            {
                Object.DestroyImmediate(id);
            }

            foreach (Transform child in root)
            {
                RemoveIDsRecursive(child);
            }
        }

        private static void RemoveSceneCleanUp(Scene scene)
        {
            List<GameObject> gos = scene.GetRootGameObjects().ToList().Where(go => go.GetComponent<SceneCleanUp>() != null)
                .ToList();
            foreach (var go in gos)
            {
                Object.DestroyImmediate(go);
            }
        }
        
        /**
         * Initialize camera movement for keyframes from camera figure
         */
        public static void StartCameraMovement(CameraFigure cameraFigure)
        {
            if(cameraFigure.keyFrames.Count == 0) return;
            startTime = (float)EditorApplication.timeSinceStartup;
            EditorApplication.update += Update;
            keyframes = cameraFigure.keyFrames;
            keyframeIndex = 0;
            camStartPos = cameraFigure.transform.position;
            camStartRot = cameraFigure.transform.rotation;
                
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;
                
            originalPosition = sceneView.pivot;
            originalRotation = sceneView.rotation;
            originalDistance = sceneView.size;
            moveTime = keyframes[keyframeIndex].cameraMoveTime;
            rotateTime = keyframes[keyframeIndex].cameraRotateTime;
            currentRuntime = moveTime > rotateTime ? moveTime : rotateTime;
        }
        
        private static void Update()
        {
            float currentTime = (float)EditorApplication.timeSinceStartup;
            float elapsedTime = currentTime - startTime;

            if (elapsedTime > currentRuntime)
            {
                if (keyframeIndex == keyframes.Count - 1)
                {
                    EditorApplication.update -= Update;
                    SceneView sceneView = SceneView.lastActiveSceneView;
                    sceneView.LookAt(originalPosition, originalRotation, originalDistance, false, false);
                    return;
                }

                camStartPos = keyframes[keyframeIndex].cameraPosition;
                camStartRot = keyframes[keyframeIndex].cameraRotation;
                keyframeIndex++;
                moveTime = keyframes[keyframeIndex].cameraMoveTime;
                rotateTime = keyframes[keyframeIndex].cameraRotateTime;
                startTime = (float)EditorApplication.timeSinceStartup;
                currentRuntime = moveTime > rotateTime ? moveTime : rotateTime;
            }
            
            UpdateCameraMovement(elapsedTime);
        }

        /**
         * Interpolate camera between each camera figure keyframe
         */
        private static void UpdateCameraMovement(float elapsedTime)
        {
            Vector3 cameraPosition = Vector3.Lerp(camStartPos,keyframes[keyframeIndex].cameraPosition,elapsedTime/currentRuntime);
            Quaternion cameraRotation = Quaternion.Slerp(camStartRot,keyframes[keyframeIndex].cameraRotation,elapsedTime/currentRuntime);

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;

            sceneView.LookAt(cameraPosition, cameraRotation, 0, false, true);

            sceneView.Repaint();
        }
        
    }
}
#endif