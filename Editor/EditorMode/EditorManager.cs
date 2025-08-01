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
    [InitializeOnLoad]
    public static class EditorManager
    {

        private static List<string> _scenePaths;
        
        private static readonly HashSet<Type> AllowedPrefabTypes = new HashSet<Type>
        {
            typeof(Transform),
            typeof(MeshRenderer),
            typeof(MeshFilter)
        };
        
        public static EditorDataSO editorDataSo;
        

        static EditorManager()
        {
            editorDataSo = AssetDatabase.LoadAssetAtPath<EditorDataSO>("Packages/com.bcatstudio.vrgreyboxing/EditorData.asset");
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
        
        public static void StartGreyboxing()
        {
            if (EditorApplication.isPlaying) return;

            if (!editorDataSo.setupTags)
            {
                foreach (var tag in editorDataSo.tags)
                {
                    SerializedObject tagManager =
                        new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                    SerializedProperty tagsProp = tagManager.FindProperty("tags");
                    bool exsists = false;
                    // First check if the tag already exists
                    for (int i = 0; i < tagsProp.arraySize; i++)
                    {
                        SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                        if (t.stringValue.Equals(tag)) exsists = true; // Tag already exists
                    }

                    if (!exsists)
                    {
                        // Otherwise, add new tag
                        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;

                        tagManager.ApplyModifiedProperties();
                    }
                }
                editorDataSo.setupTags = true;
            }
            
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            editorDataSo.lastOpenScene = SceneManager.GetActiveScene().path;
            editorDataSo.usingGreyboxingEditor = true;
            editorDataSo.objectStates = new List<ObjectBaseState>();
            editorDataSo.availablePrefabs = GetAvailablePrefabs();
            editorDataSo.sceneNames = GetAllScenes();
            
            PrepareScenes(false);
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
            EditorApplication.EnterPlaymode();
        }

        private static List<GameObject> GetAvailablePrefabs()
        {
            List<GameObject> result = new List<GameObject>(2);
            string[] temp = AssetDatabase.GetAllAssetPaths();
            List<string> names = new List<string>();
            foreach ( string s in temp ) {
                if ( s.Contains( ".prefab" ) ) names.Add( s );
            }
            
            foreach (var prefab in names)
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
                if ( go == null || go.CompareTag("VRG_WidgetCube") || go.CompareTag("VRG")) continue;
                
                if (go.GetComponentsInChildren<Component>().Where(c => AllowedPrefabTypes.Contains(c.GetType())).ToList().Count >= AllowedPrefabTypes.Count)
                {
                    if(!result.Contains(go))
                        result.Add(go);
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
        private static List<string> GetAllScenes()
        {
            List<string> scenes = new List<string>();

            // Get build scenes
            List<string> buildScenePaths = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    buildScenePaths.Add(scene.path);
            }

            // Get all scene files in Assets
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

        private static void OnPlayModeChanged(PlayModeStateChange newState)
        {
            if (newState == PlayModeStateChange.EnteredEditMode && editorDataSo.usingGreyboxingEditor)
            {
                ApplySceneChanges();
                PrepareScenes(true);
                EditorSceneManager.OpenScene(editorDataSo.lastOpenScene);
                editorDataSo.usingGreyboxingEditor = false;
            }
        }

        private static void ApplySceneChanges()
        {
            _scenePaths = AssetDatabase.FindAssets("t:Scene").ToList();
            List<Scene> scenes = GetScenesFromGUIDs(_scenePaths,editorDataSo.usingGreyboxingEditor);
            
            
            foreach (Scene scene in scenes)
            {
                SceneManager.LoadScene(scene.path,LoadSceneMode.Single);
                List<ObjectBaseState> RemovedKeys = new List<ObjectBaseState>();
                foreach (var objectState in editorDataSo.objectStates)
                {
                    bool found = false;
                    if (objectState.disabled)
                    {
                        found = true;
                    }
                    else if (objectState is SpawnedObject spawnedObject)
                    {
                        found = true;
                        SpawnObject(spawnedObject,scene);
                    }else if (objectState is CreatedObject createdObject)
                    {
                        found = true;
                        CreateObject(createdObject,scene);
                    }
                    else if (objectState is MarkerObject markerObject)
                    {
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
                
            }
        }

        private static void SpawnObject(SpawnedObject spawnedObject, Scene scene)
        {
            if (spawnedObject.prefabIndex == -1) return;
            Transform obj = ((GameObject)PrefabUtility.InstantiatePrefab(editorDataSo.availablePrefabs[spawnedObject.prefabIndex],scene)).transform;
            obj.SetPositionAndRotation(spawnedObject.Position, spawnedObject.Rotation);
            obj.localScale = spawnedObject.Scale;
            if (spawnedObject.alteredPositions.Count > 0)
            {
                ProBuilderMesh pbm = obj.GetComponent<ProBuilderMesh>();
                pbm.positions = spawnedObject.alteredPositions.ToArray();
                pbm.ToMesh();
                pbm.Refresh();
            }
        }

        private static void CreateObject(CreatedObject createdObject, Scene scene)
        {
            GameObject go = new GameObject("CreatedObject");
            SceneManager.MoveGameObjectToScene(go,scene);
            go.transform.SetPositionAndRotation(createdObject.Position, createdObject.Rotation);
            go.transform.localScale = createdObject.Scale;
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
                GameObject obj = Object.FindObjectsOfType<PersistentID>().FirstOrDefault(id => id.uniqueId == createdObject.newParentID)?.gameObject;
                if (obj != null) go.transform.SetParent(obj.transform);
            }
        }

        private static void DrawObject(MarkerObject markerObject, Scene scene)
        {
            GameObject go = new GameObject("DrawContainer");
            go.tag = "VRG_Mark";
            SceneManager.MoveGameObjectToScene(go,scene);
            go.transform.SetPositionAndRotation(markerObject.Position, markerObject.Rotation);
            go.transform.localScale = markerObject.Scale;
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
                lineRenderer.SetPositions(markerObject.markPoints[i].ToArray());
                lineRenderer.sharedMaterial = editorDataSo.drawingMaterial;
                lineRenderer.startColor = lineRenderer.endColor = markerObject.colors[i];
                lineRenderer.startWidth = lineRenderer.endWidth = markerObject.lineWidths[i];
                
                BoxCollider boxCollider = drawing.AddComponent<BoxCollider>();
                boxCollider.center = markerObject.colliderCenters[i];
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

        private static void ApplyChangesRecursively(GameObject root)
        {
            
            PersistentID persistentID = root.GetComponent<PersistentID>();
            if(persistentID == null) return;

            foreach (var key in editorDataSo.objectStates.Where(state => state is SpawnedObject))
            {
                SpawnedObject duplication = key as SpawnedObject;
                if (duplication != null && duplication.basePersistentID == persistentID.uniqueId)
                {
                    GameObject obj = Object.Instantiate(root);
                    SceneManager.MoveGameObjectToScene(obj,root.scene);
                    Transform objTrans = obj.transform;
                    obj.name = "Duplicate of " + root.name;
                    objTrans.SetPositionAndRotation(duplication.Position, duplication.Rotation);
                    objTrans.localScale = duplication.Scale;
                    if (duplication.alteredPositions.Count > 0)
                    {
                        obj.GetComponent<ProBuilderMesh>().positions = duplication.alteredPositions.ToArray();
                    }
                }
            }

            if (persistentID != null && editorDataSo.objectStates.FirstOrDefault(state => state.persisentID == persistentID.uniqueId) != null)
            {
                AlteredObject alteredObject = editorDataSo.objectStates.First(state => state.persisentID == persistentID.uniqueId) as AlteredObject;
                if (alteredObject != null)
                {
                    if (alteredObject.Deleted)
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
                    root.transform.SetPositionAndRotation(alteredObject.Position, alteredObject.Rotation);
                    root.transform.localScale = alteredObject.Scale;
                }
            }
            editorDataSo.objectStates.Remove(editorDataSo.objectStates.FirstOrDefault(state => state.persisentID == persistentID.uniqueId));
            
            foreach (Transform child in root.transform)
            {
                ApplyChangesRecursively(child.gameObject);
            }
        }

        private static void PrepareScenes(bool remove)
        {
            _scenePaths = AssetDatabase.FindAssets("t:Scene").ToList();
            List<Scene> scenes = GetScenesFromGUIDs(_scenePaths,editorDataSo.usingGreyboxingEditor);
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
        
    }
}
#endif