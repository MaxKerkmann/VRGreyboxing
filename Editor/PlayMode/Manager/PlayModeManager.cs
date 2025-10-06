using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEditor.ProBuilder;
using UnityEngine.ProBuilder;

namespace VRGreyboxing
{
    /**
     * Manager class to handle and save changes across different scenes
     */
    public class PlayModeManager : MonoBehaviour
    {
        public static PlayModeManager Instance;
        
        public EditorDataSO editorDataSO;

        private List<ObjectBaseState> _actionStack;
        private int _actionStackIndex;
        
        public WorldScaler currentWorldScaler;
        

        private static readonly HashSet<Type> AllowedTypes = new HashSet<Type>
        {
            typeof(Transform),
            typeof(RectTransform),
            typeof(MeshRenderer),
            typeof(SkinnedMeshRenderer),
            typeof(MeshFilter),
            typeof(BoxCollider),
            typeof(SphereCollider),
            typeof(CapsuleCollider),
            typeof(MeshCollider),
            typeof(Collider),
            typeof(Light),
            typeof(SceneCleanUp),
            typeof(PersistentID),
            typeof(CameraFigure),
            typeof(ProBuilderMesh),
            typeof(ProBuilderEditor),
            typeof(LineRenderer)
        };
        
        public void Awake()
        {
            Instance = this;
            ResetActionStack();
        }
        
        /**
         * Remove all unwanted components from objects in the scene.
         **/
        public void StripComponentsInCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                root.SetActive(false);

                StripGameObject(root);

                root.SetActive(true);
            }
        }

        /**
         * Remove components from specific gameobject
         */
        public GameObject StripGameObject(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var components = t.GetComponents<Component>()
                    .Where(c => !AllowedTypes.Contains(c.GetType())).ToList();
                    
                var dependencyMap = new Dictionary<Type, HashSet<Type>>(); 
                var componentTypes = components.Select(c => c.GetType()).ToList();

                foreach (var type in componentTypes)
                {
                    if (!dependencyMap.ContainsKey(type))
                        dependencyMap[type] = new HashSet<Type>();

                    var requires = type.GetCustomAttributes(typeof(RequireComponent), true)
                        .Cast<RequireComponent>();

                    foreach (var req in requires)
                    {
                        foreach (var requiredType in new[] { req.m_Type0, req.m_Type1, req.m_Type2 })
                        {
                            if (requiredType != null && componentTypes.Contains(requiredType))
                            {
                                dependencyMap[type].Add(requiredType);
                            }
                        }
                    }
                }

                List<Type> removalOrder = TopologicalSort(componentTypes, dependencyMap);
                if (removalOrder == null)return root;
                    
                removalOrder.Reverse();
                foreach (var type in removalOrder)
                {
                    Component comp = t.GetComponent(type);
                    if (comp != null)
                    {
                        DestroyImmediate(comp);
                    }
                }

            }
            return root;
        } 
        
        /**
         * Sort components of object in way that they can be deleted without dependency issues
         **/
        private static List<Type> TopologicalSort(List<Type> types, Dictionary<Type, HashSet<Type>> dependencies)
        {
            var result = new List<Type>();
            var visited = new HashSet<Type>();
            var temp = new HashSet<Type>();

            bool Visit(Type node)
            {
                if (temp.Contains(node)) return false;
                if (visited.Contains(node)) return true;

                temp.Add(node);

                if (dependencies.TryGetValue(node, out var deps))
                {
                    foreach (var dep in deps)
                    {
                        if (!Visit(dep)) return false;
                    }
                }

                temp.Remove(node);
                visited.Add(node);
                result.Add(node);

                return true;
            }

            foreach (var type in types)
            {
                if (!Visit(type)) return null;
            }

            return result;
        }

        /**
         * Create world scale object in scene and make it the parent of all objects
         **/
        public void PlaceWorldScaler()
        {
            GameObject worldScaler = new GameObject("WorldScaler");
            worldScaler.transform.position = Vector3.zero;
            currentWorldScaler = worldScaler.AddComponent<WorldScaler>();
            Scene scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                root.transform.SetParent(worldScaler.transform);
            }

        }

        /**
         * Reset scale of world scaler
         **/
        public void ResetWorldScale()
        {
            if(currentWorldScaler == null) return;
            currentWorldScaler.SetScale(1);
            foreach (var objectState in editorDataSO.objectStates)
            {
                if(objectState.originalScenePath != SceneManager.GetActiveScene().path) continue;
                objectState.position = objectState.gameObject.transform.position;
                objectState.scale = objectState.gameObject.transform.localScale;
            }
        }

        /**
         * Save changes of passed objects as ObjectState variances depending on passed variables
         **/
        public void RegisterObjectChange(GameObject obj,bool firstSelection = false, int prefabIndex = -1, bool objectCreation = false, bool objectDeletion = false, string basePersistentID = "",string parentPersitendID = "",List<Vector3> basePositions = null,bool flipVertices = false,List<List<Vector3>> markPoints = null,List<Vector3> drawingOffsets = null,List<Vector3> colliderCenters = null,List<Vector3> colliderSizes = null,List<Color> colors = null,List<float> lineWidths = null)
        {

            //Case: Object has not been saved before
            if (objectCreation)
            {
                var persitentID = obj.AddComponent<PersistentID>();
                var scene = SceneManager.GetActiveScene();
                List<Vector3> positions = obj.GetComponent<ProBuilderMesh>().positions.ToList();
                var createdObj = new CreatedObject(obj, persitentID.uniqueId, obj.transform.position, obj.transform.rotation, obj.transform.lossyScale,objectDeletion, basePositions, positions,flipVertices, scene.path, true);
                createdObj.newParentID = parentPersitendID;
                if (parentPersitendID == "" && editorDataSO.objectStates.Count > 0)
                {
                    for (int i = editorDataSO.objectStates.Count - 1; i >= 0; i--)
                    {
                        if (string.IsNullOrEmpty(editorDataSO.objectStates[i].newParentID))
                        {
                            if (i == editorDataSO.objectStates.Count - 1)
                            {
                                editorDataSO.objectStates.Add(createdObj);
                                break;
                            }

                            {
                                editorDataSO.objectStates.Insert(i, createdObj);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    editorDataSO.objectStates.Add(createdObj);
                }

                _actionStack.Add(createdObj);
                _actionStackIndex++;
                _actionStack[_actionStackIndex] = createdObj;
            }
            else if (prefabIndex >= 0)
            {
                var persitentID = obj.AddComponent<PersistentID>();
                var scene = SceneManager.GetActiveScene();
                List<Vector3> positions = obj.GetComponent<ProBuilderMesh>() != null ? obj.GetComponent<ProBuilderMesh>().positions.ToList() : new List<Vector3>();
                var spawnedObj = new SpawnedObject(obj,persitentID.uniqueId,obj.transform.position, obj.transform.rotation, obj.transform.localScale,objectDeletion,positions, prefabIndex,scene.path,basePersistentID)
                    {
                        justCreated = true
                    };
                if (obj.GetComponent<CameraFigure>() != null)
                {
                    spawnedObj.keyFrames = obj.GetComponent<CameraFigure>().keyFrames;
                }
                editorDataSO.objectStates.Add(spawnedObj);
                _actionStack.Add(spawnedObj);
                _actionStackIndex++;
                _actionStack[_actionStackIndex] = spawnedObj;
            }
            else if (basePersistentID != "")
            {
                var persitentID = obj.AddComponent<PersistentID>();
                var scene = SceneManager.GetActiveScene();
                List<Vector3> positions = obj.GetComponent<ProBuilderMesh>() != null ? obj.GetComponent<ProBuilderMesh>().positions.ToList() : new List<Vector3>();
                var spawnedObj = new SpawnedObject(obj,persitentID.uniqueId,obj.transform.position, obj.transform.rotation, obj.transform.lossyScale,objectDeletion,positions, -1,scene.path,basePersistentID)
                {
                    justCreated = true
                };
                editorDataSO.objectStates.Add(spawnedObj);
                _actionStack.Add(spawnedObj);
                _actionStackIndex++;
                _actionStack[_actionStackIndex] = spawnedObj;
            }
            else if (markPoints != null)
            {
                var persitentID = obj.AddComponent<PersistentID>();
                var scene = SceneManager.GetActiveScene();
                List<Vector3> positions = new List<Vector3>();
                var markObj = new MarkerObject(obj,persitentID.uniqueId,obj.transform.position, obj.transform.rotation, obj.transform.lossyScale,objectDeletion,positions,scene.path,markPoints,drawingOffsets,colliderCenters,colliderSizes,colors,lineWidths)
                {
                    justCreated = true
                };
                editorDataSO.objectStates.Add(markObj);
                _actionStack.Add(markObj);
                _actionStackIndex++;
                _actionStack[_actionStackIndex] = markObj;
            }
            //Case: object was already saved before
            else
            {
                var persistentID = obj.GetComponent<PersistentID>();
                if (persistentID == null)
                {
                    persistentID = obj.GetComponent<IdHolderInformation>().GetIDHolder().GetComponentInParent<PersistentID>();
                }
                ObjectBaseState baseState = editorDataSO.objectStates.FirstOrDefault(obs => obs.persisentID == persistentID.uniqueId);
                if (baseState != null && !firstSelection)
                {
                    if (objectDeletion)
                    {
                        for (int i = 0; i < baseState.gameObject.transform.childCount; i++)
                        {
                            if (baseState.gameObject.transform.GetChild(i).GetComponent<PersistentID>() != null)
                            {
                                var persistentChildID = baseState.gameObject.transform.GetChild(i).GetComponent<PersistentID>();
                                if (persistentChildID == null)
                                {
                                    persistentChildID = baseState.gameObject.transform.GetChild(i).GetComponent<IdHolderInformation>().GetIDHolder().GetComponentInParent<PersistentID>();
                                }
                                ObjectBaseState childBaseState = editorDataSO.objectStates.FirstOrDefault(obs => obs.persisentID == persistentChildID.uniqueId);
                                childBaseState.deleted = true;
                            }
                        }
                    }
                    
                    switch (baseState)
                    {
                        case AlteredObject:
                        {
                            List<Vector3> positions = obj.GetComponent<ProBuilderMesh>() != null ? obj.GetComponent<ProBuilderMesh>().positions.ToList() : new List<Vector3>();
                            var alteredObject = new AlteredObject(obj,baseState.persisentID, obj.transform.position, obj.transform.rotation, obj.transform.lossyScale, objectDeletion,SceneManager.GetActiveScene().path,positions)
                            {
                                prevState = baseState
                            };
                            if (obj.GetComponent<CameraFigure>() != null)
                            {
                                alteredObject.keyFrames = obj.GetComponent<CameraFigure>().keyFrames;
                            }
                            editorDataSO.objectStates[editorDataSO.objectStates.IndexOf(baseState)] = alteredObject;
                            if (baseState.untouched)
                                _actionStackIndex++;
                            if (_actionStackIndex >= _actionStack.Count)
                            {
                                _actionStack.Add(alteredObject);
                            }
                            else
                            {
                                _actionStack[_actionStackIndex] = alteredObject;
                                for (int i = _actionStackIndex + 1; i < _actionStack.Count; i++)
                                {
                                    _actionStack[i].RemoveChange();
                                }
                                _actionStack = _actionStack.Take(_actionStackIndex+1).ToList();
                            }
                            break;
                        }
                        case SpawnedObject:
                        {
                            SpawnedObject spawnedState = baseState as SpawnedObject;
                            List<Vector3> positions = obj.GetComponent<ProBuilderMesh>() != null ? obj.GetComponent<ProBuilderMesh>().positions.ToList() : new List<Vector3>();
                            var spawnedObject = new SpawnedObject(obj,baseState.persisentID, obj.transform.position, obj.transform.rotation, obj.transform.localScale,objectDeletion,positions, spawnedState.prefabIndex, SceneManager.GetActiveScene().path,spawnedState.basePersistentID)
                            {
                                prevState = baseState
                            };
                            if (obj.GetComponent<CameraFigure>() != null)
                            {
                                spawnedObject.keyFrames = obj.GetComponent<CameraFigure>().keyFrames;
                            }
                            editorDataSO.objectStates[editorDataSO.objectStates.IndexOf(baseState)] = spawnedObject;
                            if (baseState.untouched)
                                _actionStackIndex++;
                            _actionStack[_actionStackIndex] = spawnedObject;
                            break;
                        }
                        case CreatedObject:
                        {
                            CreatedObject createdState = baseState as CreatedObject;
                            var createdObject = new CreatedObject(obj, createdState.persisentID, obj.transform.position,
                                obj.transform.rotation, obj.transform.lossyScale,objectDeletion, createdState.basePositions,
                                obj.GetComponent<ProBuilderMesh>().positions.ToList(),createdState.flippedVertices, SceneManager.GetActiveScene().path, false)
                            {
                                prevState = baseState
                            };
                            editorDataSO.objectStates[editorDataSO.objectStates.IndexOf(baseState)] = createdObject;
                            if (baseState.untouched)
                                _actionStackIndex++;
                            _actionStack[_actionStackIndex] = createdObject;
                            break;
                        }
                        case MarkerObject:
                            MarkerObject markerState = baseState as MarkerObject;
                            var markerObject = new MarkerObject(obj,baseState.persisentID, obj.transform.position, obj.transform.rotation, obj.transform.lossyScale,objectDeletion, new List<Vector3>(),SceneManager.GetActiveScene().path,markerState.markPoints,drawingOffsets,markerState.colliderCenters,markerState.colliderSizes,markerState.colors,markerState.lineWidths)
                            {
                                prevState = baseState
                            };
                            editorDataSO.objectStates[editorDataSO.objectStates.IndexOf(baseState)] = markerObject;
                            if (baseState.untouched)
                                _actionStackIndex++;
                            _actionStack[_actionStackIndex] = markerObject;
                            break;
                    }
                }
                else if(baseState == null)
                {
                    List<Vector3> positions = obj.GetComponent<ProBuilderMesh>() != null ? obj.GetComponent<ProBuilderMesh>().positions.ToList() : new List<Vector3>();
                    AlteredObject alteredObject = new AlteredObject(obj, persistentID.uniqueId, obj.transform.position, obj.transform.rotation, obj.transform.localScale, objectDeletion,SceneManager.GetActiveScene().path,positions)
                    {
                        untouched = true
                    };
                    if (obj.GetComponent<CameraFigure>() != null)
                    {
                        alteredObject.keyFrames = obj.GetComponent<CameraFigure>().keyFrames;
                    }
                    editorDataSO.objectStates.Add(alteredObject);
                }
            }
        }

        public ObjectBaseState GetObjectTypeForPersistentID(PersistentID persistentID)
        {
            return editorDataSO.objectStates.FirstOrDefault(obs => obs.persisentID == persistentID.uniqueId);
        }

        /**
         * Trigger undo change of last action in action stack
         **/
        public void UndoObjectChange()
        {
            if(_actionStackIndex < 0) return;


            if (_actionStack[_actionStackIndex].prevState != null || _actionStack[_actionStackIndex].justCreated)
            {
                _actionStack[_actionStackIndex] = _actionStack[_actionStackIndex].UndoChange();
                editorDataSO.objectStates[editorDataSO.objectStates.IndexOf(editorDataSO.objectStates.FirstOrDefault(obs => obs.persisentID == _actionStack[_actionStackIndex].persisentID))] = _actionStack[_actionStackIndex];
                editorDataSO.undo++;
            }
            else
            {
                if (_actionStackIndex <= 0) return;
                
                _actionStackIndex--;
                UndoObjectChange();
            }
        }

        /**
         * Trigger redo change of last action in action stack
         **/
        public void RedoObjectChange()
        {
            if(_actionStackIndex < 0) return;
            if(_actionStack[_actionStackIndex].nextState != null || !_actionStack[_actionStackIndex].gameObject.activeSelf)
            {
                _actionStack[_actionStackIndex] = _actionStack[_actionStackIndex].RedoChange();
                editorDataSO.objectStates[editorDataSO.objectStates.IndexOf(editorDataSO.objectStates.FirstOrDefault(obs => obs.persisentID == _actionStack[_actionStackIndex].persisentID))] = _actionStack[_actionStackIndex];
                editorDataSO.redo++;
            }
            else
            {
                if(_actionStackIndex == _actionStack.Count-1) return;
                
                _actionStackIndex++;
                RedoObjectChange();
            }
        }

        private void ResetActionStack()
        {
            _actionStack = new List<ObjectBaseState>();
            _actionStackIndex = -1;
        }

        public void RefreshObjectStates()
        {
            foreach (var objectState in editorDataSO.objectStates)
            {
                if (objectState.originalScenePath == SceneManager.GetActiveScene().path)
                {
                    PersistentID id = FindObjectsByType<PersistentID>(FindObjectsSortMode.None)
                        .FirstOrDefault(persID => persID.uniqueId == objectState.persisentID);
                    if(id != null)
                        objectState.gameObject = id.gameObject;
                }
            }
        }
        
        public void SwitchToScene(string sceneName)
        {
            RefreshObjectStates();
            ResetWorldScale();
            ResetActionStack();
            SceneManager.LoadScene(sceneName);
            RefreshObjectStates();
        }
    }

}