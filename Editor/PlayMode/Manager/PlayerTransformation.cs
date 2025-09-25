using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.ProBuilder;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using Math = System.Math;


#if UNITY_EDITOR

namespace VRGreyboxing
{
    public enum TransWidgetTransformType
    {
        RotationX,
        RotationY,
        RotationZ,
        MoveForward,
        MoveBackward,
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown,
        ScaleForward,
        ScaleBackward,
        ScaleLeft,
        ScaleRight,
        ScaleUp,
        ScaleDown,
        ScaleCorner
    }
    
    /**
     * Logic to apply transform changes to objects in the scene
     */
    public class PlayerTransformation : MonoBehaviour
    {
        public XRInteractionManager xrInteractionManager;
        public GameObject transWidgetPrefab;
        public GameObject keyFrameDisplayPrefab;
        
        [HideInInspector]
        public TransWidgetEditPoint currentEditPoint;
        [HideInInspector]
        public GameObject selectedObject;
        private bool _justSelected;
        private GameObject _currentTransWidget;
        private Handedness _selectTransformHandedness;
        public GameObject currentSelectedTransWidgetPoint;
        public Quaternion currentStartRotation;
        
        private GameObject _leftController;
        private GameObject _rightController;

        public GameObject leftgrabbedObject;
        public GameObject rightgrabbedObject;

        public GameObject currentCameraFigure;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Vector3 _originalScale;
        private float _originalHeight;

        public bool usingCameraFigure;
        
        private List<KeyFrameDisplay> _keyFrameDisplays;


        private void Start()
        {
            _leftController = ActionManager.Instance.leftController;
            _rightController = ActionManager.Instance.rightController;
            _keyFrameDisplays = new List<KeyFrameDisplay>();
        }
        


        /**
         * Grab interactable object with one or two hands using the XR Interaction Manager
         */
        public void PerformGrab(Handedness handedness,XRGrabInteractable interactable)
        {
            if (interactable.gameObject.GetComponentInChildren<MeshCollider>())
            {
                interactable.gameObject.GetComponentInChildren<MeshCollider>().convex = true;
            }
            
            GameObject usedController = handedness == Handedness.Right ? _rightController : _leftController;

            if (handedness == Handedness.Right)
            {
                rightgrabbedObject = interactable.transform.gameObject;
            }
            else
            {
                leftgrabbedObject = interactable.transform.gameObject;
            }


            if (leftgrabbedObject == rightgrabbedObject)
            {
                if (handedness == Handedness.Right)
                {

                        interactable.trackRotation = true;
                        
                        xrInteractionManager.SelectEnter(_leftController.GetComponentInChildren<IXRSelectInteractor>(),
                            interactable);
                        xrInteractionManager.SelectEnter(_rightController.GetComponentInChildren<IXRSelectInteractor>(),
                            interactable);
                }
                else
                {
                    interactable.trackRotation = true;

                    xrInteractionManager.SelectEnter(_rightController.GetComponentInChildren<IXRSelectInteractor>(),interactable);
                    xrInteractionManager.SelectEnter(_leftController.GetComponentInChildren<IXRSelectInteractor>(),interactable);
                }
                    
            }
            else
            {
                interactable.trackRotation = true;
                xrInteractionManager.SelectEnter(usedController.GetComponentInChildren<IXRSelectInteractor>(),
                    interactable);
            }
        }
        
        /**
         * End grabbing of object and reset variables
         */
        public void EndGrab(Handedness handedness)
        {

            var usedController = handedness == Handedness.Right ? _rightController : _leftController;
            var grabInteractable = handedness == Handedness.Right ? rightgrabbedObject.GetComponent<XRGrabInteractable>() : leftgrabbedObject.GetComponent<XRGrabInteractable>();
            var interactor = usedController.GetComponentInChildren<IXRSelectInteractor>();
            if (grabInteractable != null)
            {
                grabInteractable.trackRotation = true;
                xrInteractionManager.SelectExit(interactor, grabInteractable);
                xrInteractionManager.CancelInteractableFocus(grabInteractable);
            }
            else
            {
                xrInteractionManager.SelectExit(interactor, interactor.firstInteractableSelected);
                
            }
            if (handedness == Handedness.Right)
            {
                rightgrabbedObject = null;
            }
            else
            {
                leftgrabbedObject = null;
            }

        }

        /**
        * Select currently hovered object.
        * If it´s a transform point make the user grab, otherwise display the transform point widget around it.
        */
        public void SelectObject(Handedness handedness,GameObject obj)
        {
            var constrainGrabTransformer = obj.GetComponent<ConstrainGrabTransformer>();
            if (constrainGrabTransformer != null)
            {
                TransWidgetEditPoint transWidgetEditPoint = constrainGrabTransformer.transWidgetEditPoint;
                if (transWidgetEditPoint != null && !_justSelected)
                {
                    currentSelectedTransWidgetPoint = transWidgetEditPoint.gameObject;
                    _selectTransformHandedness = handedness;
                    GameObject usedController = handedness == Handedness.Right ? _rightController : _leftController;
                    var interactor = usedController.GetComponentInChildren<IXRSelectInteractor>();
                    var interactable = constrainGrabTransformer.gameObject.GetComponent<XRGrabInteractable>();
                    xrInteractionManager.SelectEnter(interactor, interactable);
                    return;
                }
            }
            currentSelectedTransWidgetPoint = null;

            if (_justSelected) return;

            if (obj == selectedObject)
            {
                DeselectObject();
                return;
            }

            if (obj.GetComponentInChildren<KeyFrameDisplay>() != null)
            {
                ActionManager.Instance.DisplayKeyframeEditMenu(obj.transform.position, obj.GetComponentInChildren<KeyFrameDisplay>().keyFrameIndex);
                return;
            }
            
            if (selectedObject != null)
                DeselectObject();
            
            if(obj.CompareTag("VRG_Mark"))
                selectedObject = obj.transform.parent.gameObject;
            else
                selectedObject = obj;

            if (selectedObject.GetComponent<CameraFigure>() != null && _keyFrameDisplays.Count == 0)
            {
                DisplayCameraKeyframes();
            }
            else if(_keyFrameDisplays.Count > 0)
            {
                RemoveKeyFrameDisplays();
            }
            
            _selectTransformHandedness = Handedness.None;
            _justSelected = true;
            ApplyTransWidgetToSelectedObject();
        }

        /**
        *  If the current selected object is a transform point, end the grab of the object and display the transform widget again
        */
        public void EndSelection(GameObject obj)
        {
            if (_selectTransformHandedness != Handedness.None)
            {
                var grabInteractable = obj.GetComponent<ConstrainGrabTransformer>();

                GameObject usedController = _selectTransformHandedness == Handedness.Left
                    ? _leftController
                    : _rightController;
                var interactor = usedController.GetComponentInChildren<IXRSelectInteractor>();
                var interactable = grabInteractable.gameObject.GetComponent<XRGrabInteractable>();
                xrInteractionManager.SelectExit(interactor, interactable);
                _selectTransformHandedness = Handedness.None;
                currentSelectedTransWidgetPoint = null;
                ApplyTransWidgetToSelectedObject();
            }
            else
            {
                _justSelected = false;
            }
        }

        /**
        * Create a new transform widget for the selected object, destroy the old one
        */
        public void ApplyTransWidgetToSelectedObject()
        {
            GameObject transWidget = Instantiate(transWidgetPrefab, selectedObject.transform.position,Quaternion.identity);
            if(_currentTransWidget != null)
                Destroy(_currentTransWidget);
            _currentTransWidget = transWidget;
            SetCurrentTransWidgetPositions();
        }
        
        /**
         * Disable all parts of the current transform widget except the one being grabbed
         */
        public void DisableTransWidget(bool exceptCurrentTransWidgetPoint)
        {
            foreach (var editPoint in _currentTransWidget.GetComponentsInChildren<TransWidgetEditPoint>())
            {
                if (exceptCurrentTransWidgetPoint && editPoint == currentEditPoint) continue;
                editPoint.gameObject.SetActive(false);
            }
        }
        
        /**
        * Place the transform widget points the vertices, edges and faces of the selected object
        */
        public void SetCurrentTransWidgetPositions()
        {
            Quaternion originalRotation = selectedObject.transform.rotation;
            selectedObject.transform.rotation = _currentTransWidget.transform.rotation;
            bool enabledCollider = false;
            if (selectedObject.GetComponentInChildren<Collider>().enabled == false)
            {
                selectedObject.GetComponentInChildren<Collider>().enabled = true;
                enabledCollider = true;
            }
            
            Bounds objBounds = selectedObject.GetComponentInChildren<Renderer>() != null ? selectedObject.GetComponentInChildren<Renderer>().bounds : selectedObject.GetComponentInChildren<Collider>().bounds;
            for (int i = 0; i < selectedObject.transform.childCount; i++)
            {
                if(selectedObject.transform.GetChild(i).GetComponentInChildren<Renderer>() != null || selectedObject.transform.GetChild(i).GetComponentInChildren<Collider>() != null)
                    objBounds.Encapsulate(selectedObject.transform.GetChild(i).GetComponentInChildren<Renderer>() != null ? selectedObject.transform.GetChild(i).GetComponentInChildren<Renderer>().bounds : selectedObject.transform.GetChild(i).GetComponentInChildren<Collider>().bounds);
            }

            Vector3 rotationWidgetPointCounter = Vector3.zero;
            int cornerScaleCounter = 0;
            Transform t = _currentTransWidget.transform;
            Vector3 e = objBounds.extents;
            float objSizeFactor = (objBounds.size.x+ objBounds.size.y+ objBounds.size.z)/3;
            foreach (var editPoint in _currentTransWidget.GetComponentsInChildren<TransWidgetEditPoint>())
            {
                editPoint.playerTransformation = this;
                editPoint.transform.localScale = Vector3.one * (objSizeFactor*PlayModeManager.Instance.editorDataSO.widgetScaleSize);
                if(editPoint == currentEditPoint) continue;
                switch (editPoint.transWidgetTransformType)
                {
                    case TransWidgetTransformType.MoveUp:
                        editPoint.transform.position += t.up * e.y;
                        break;
                    case TransWidgetTransformType.MoveDown:
                        editPoint.transform.position -= t.up * e.y;
                        break;
                    case TransWidgetTransformType.MoveForward:
                        editPoint.transform.position += t.forward * e.z;
                        break;
                    case TransWidgetTransformType.MoveBackward:
                        editPoint.transform.position -= t.forward * e.z;
                        break;
                    case TransWidgetTransformType.MoveLeft:
                        editPoint.transform.position -= t.right * e.x;
                        break;
                    case TransWidgetTransformType.MoveRight:
                        editPoint.transform.position += t.right * e.x;
                        break;
                    case TransWidgetTransformType.ScaleUp:
                        editPoint.transform.position += t.up * e.y;
                        editPoint.transform.up = t.up;
                        break;
                    case TransWidgetTransformType.ScaleDown:
                        editPoint.transform.position -= t.up * e.y;
                        editPoint.transform.up = -t.up;
                        break;
                    case TransWidgetTransformType.ScaleForward:
                        editPoint.transform.position += t.forward * e.z;
                        editPoint.transform.up = t.forward;
                        break;
                    case TransWidgetTransformType.ScaleBackward:
                        editPoint.transform.position -= t.forward * e.z;
                        editPoint.transform.up = -t.forward;
                        break;
                    case TransWidgetTransformType.ScaleLeft:
                        editPoint.transform.position -= t.right * e.x;
                        editPoint.transform.up = -t.right;
                        break;
                    case TransWidgetTransformType.ScaleRight:
                        editPoint.transform.position += t.right * e.x;
                        editPoint.transform.up = t.right;
                        break;
                    case TransWidgetTransformType.RotationY:
                        switch (rotationWidgetPointCounter.y)
                        {
                            case 0:
                                editPoint.transform.position += t.right * e.x + t.forward * e.z;
                                rotationWidgetPointCounter.y++;
                                break;
                            case 1:
                                editPoint.transform.position += t.right * e.x - t.forward * e.z;
                                rotationWidgetPointCounter.y++;
                                break;
                            case 2:
                                editPoint.transform.position -= t.right * e.x + t.forward * e.z;
                                rotationWidgetPointCounter.y++;
                                break;
                            case 3:
                                editPoint.transform.position -= t.right * e.x - t.forward * e.z;
                                rotationWidgetPointCounter.y++;
                                break;
                        }
                        break;
                    case TransWidgetTransformType.RotationZ:
                        switch (rotationWidgetPointCounter.z)
                        {
                            case 0:
                                editPoint.transform.position += t.right * e.x + t.up * e.y;
                                rotationWidgetPointCounter.z++;
                                break;
                            case 1:
                                editPoint.transform.position += t.right * e.x - t.up * e.y;
                                rotationWidgetPointCounter.z++;
                                break;
                            case 2:
                                editPoint.transform.position -= t.right * e.x + t.up * e.y;
                                rotationWidgetPointCounter.z++;
                                break;
                            case 3:
                                editPoint.transform.position -= t.right * e.x - t.up * e.y;
                                rotationWidgetPointCounter.z++;
                                break;
                        }
                        break;
                    case TransWidgetTransformType.RotationX:
                        switch (rotationWidgetPointCounter.x)
                        {
                            case 0:
                                editPoint.transform.position += t.forward * e.z + t.up * e.y;
                                rotationWidgetPointCounter.x++;
                                break;
                            case 1:
                                editPoint.transform.position += t.forward * e.z - t.up * e.y;
                                rotationWidgetPointCounter.x++;
                                break;
                            case 2:
                                editPoint.transform.position -= t.forward * e.z + t.up * e.y;
                                rotationWidgetPointCounter.x++;
                                break;
                            case 3:
                                editPoint.transform.position -= t.forward * e.z - t.up * e.y;
                                rotationWidgetPointCounter.x++;
                                break;
                        }
                        break;
                    case TransWidgetTransformType.ScaleCorner:
                        switch (cornerScaleCounter)
                        {
                            case 0:
                                editPoint.transform.position -= t.right * e.x + t.up * e.y+t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                            case 1:
                                editPoint.transform.position += t.right * e.x + t.up * e.y+t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                            case 2:
                                editPoint.transform.position += t.right * e.x + t.up * e.y-t.forward * e.z;
                                cornerScaleCounter++;   
                                break;
                            case 3:
                                editPoint.transform.position -= t.right * e.x - t.up * e.y+t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                            case 4:
                                editPoint.transform.position += t.right * e.x - t.up * e.y+t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                            case 5:
                                editPoint.transform.position -= t.right * e.x - t.up * e.y-t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                            case 6:
                                editPoint.transform.position += t.right * e.x - t.up * e.y-t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                            case 7:
                                editPoint.transform.position -= t.right * e.x + t.up * e.y-t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                        }
                        break;
                }
            }
            selectedObject.transform.rotation = _currentTransWidget.transform.rotation = originalRotation;
            if(enabledCollider)
                selectedObject.GetComponentInChildren<Collider>().enabled = false;

        }
        
        /**
        * Remove current transform widget and reset selected object variable
        */
        public void DeselectObject()
        {
            Destroy(_currentTransWidget);
            RemoveKeyFrameDisplays();
            selectedObject = null;
        }
        
        /**
         * Move or rotate the selected objects depending on which type of transform point has been grabbed
         */
        public void TransformSelectedObject(TransWidgetTransformType transWidgetPoint,Vector3 transformation, Quaternion deltaRotation)
        {
            switch (transWidgetPoint)
            {
                case TransWidgetTransformType.MoveUp:
                case TransWidgetTransformType.MoveDown:
                case TransWidgetTransformType.MoveLeft:
                case TransWidgetTransformType.MoveRight:
                case TransWidgetTransformType.MoveForward:
                case TransWidgetTransformType.MoveBackward:
                    selectedObject.transform.position = transformation;
                    break;
                case TransWidgetTransformType.RotationY:
                case TransWidgetTransformType.RotationZ:
                case TransWidgetTransformType.RotationX:
                    selectedObject.transform.rotation = deltaRotation * currentStartRotation;
                    break;
            }
        }
        
        /**
         * Scale the selected object depending on the position of the grabbed transform point either just on one axis or all at the same time
         */
        public void ScaleSelectedObjectFromCorner(Vector3 fixedCorner, Vector3 transWidgetPoint, Vector3 cornerPoint, GameObject scaleObject)
        {
            var oldVector = fixedCorner - cornerPoint;
            var rotatedOldVector = Quaternion.Inverse(selectedObject.transform.rotation) * oldVector;
            if (Math.Abs(rotatedOldVector.x) <= 0.01)
                rotatedOldVector.x = 0;
            if (Math.Abs(rotatedOldVector.y) <= 0.01)
                rotatedOldVector.y = 0;
            if (Math.Abs(rotatedOldVector.z) <= 0.01)
                rotatedOldVector.z = 0;
            

            float oldDiagonal = Vector3.Distance(fixedCorner,cornerPoint);
            float newDiagonal = Vector3.Distance(fixedCorner, transWidgetPoint);
            float ratio = newDiagonal / oldDiagonal;

            if (rotatedOldVector.x == 0 || rotatedOldVector.y == 0 || rotatedOldVector.z == 0)
            {
                if (rotatedOldVector.x != 0)
                    scaleObject.transform.localScale = new Vector3(ratio, 1, 1);
                if (rotatedOldVector.y != 0)
                    scaleObject.transform.localScale = new Vector3(1, ratio, 1);
                if (rotatedOldVector.z != 0)
                    scaleObject.transform.localScale = new Vector3(1, 1, ratio);
            }
            else
            {
                scaleObject.transform.localScale = new Vector3(ratio,ratio,ratio);
            }
        }
        
        /**
         *  Spawn selected prefab from the inventory in front of the player and grab the object the controller used to select it in the menu
         */
        public GameObject CreatePrefabFromMenu(GameObject prefab,int prefabIndex,Transform buttonTransform)
        {
            GameObject go = Instantiate(prefab,buttonTransform.position,Quaternion.identity);
            go.transform.up = Vector3.up;
            go.transform.parent = PlayModeManager.Instance.currentWorldScaler.transform;
            go.transform.localScale = prefab.transform.localScale;

            if (go.GetComponentInChildren<Collider>() == null)
            {
                MeshCollider mcollider = go.AddComponent<MeshCollider>();
                mcollider.convex = true;
                mcollider.sharedMesh = go.GetComponentInChildren<MeshFilter>() != null ? go.GetComponentInChildren<MeshFilter>().sharedMesh : go.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            }
            
            go.AddComponent<TwoHandGrabTransformer>();
            IdHolderInformation idHolderInformation = go.AddComponent<IdHolderInformation>();
            idHolderInformation.prefabIndex = prefabIndex;
            XRGrabInteractable interactable = go.GetComponent<XRGrabInteractable>();
            if(interactable == null)
                 interactable = go.AddComponent<XRGrabInteractable>();
            ActionManager.Instance.SetupInteractable(interactable);
            
            return go;
        }

        /**
         * Delete the selected object and save the action
         */
        public void DeleteSelectedObject()
        {
            PlayModeManager.Instance.RegisterObjectChange(selectedObject,false,-1,false,true);
            Destroy(_currentTransWidget); 
            selectedObject.SetActive(false);
            selectedObject = null;
            ActionManager.Instance.CloseSelectionMenu();
        }

        /**
         * Create a copy of the selected object and grab it with the controller used to select the option
         */
        public GameObject DuplicateSelectedObject()
        {
            ActionManager.Instance.CloseSelectionMenu();
            GameObject go = Instantiate(selectedObject,selectedObject.transform.position,Quaternion.identity);
            if (go.GetComponentInChildren<ProBuilderMesh>() != null)
            {
                var pbm = go.GetComponentInChildren<ProBuilderMesh>();
                pbm.MakeUnique();
                pbm.ToMesh();
                pbm.Refresh();
            }
            ObjectBaseState baseState = PlayModeManager.Instance.GetObjectTypeForPersistentID(go.GetComponent<PersistentID>());
            if (baseState is CreatedObject)
            {
                CreatedObject createdState = baseState as CreatedObject;
                Destroy(go.GetComponent<PersistentID>());
                PlayModeManager.Instance.RegisterObjectChange(go,false,-1,true,false,"","",createdState.basePositions,createdState.flippedVertices);
                return go;
            }
            Destroy(go.GetComponent<PersistentID>());
            IdHolderInformation idHolderInformation = go.AddComponent<IdHolderInformation>();
            if (selectedObject.GetComponent<IdHolderInformation>() != null)
            {
                idHolderInformation.prefabIndex = selectedObject.GetComponent<IdHolderInformation>().prefabIndex;
            }
            idHolderInformation.iDHolder = selectedObject.transform;
            return go;
        }
        

        /**
         * Save the selected object as prefab in the project files
         */
        public async void SaveSelectedObject()
        {
            ActionManager.Instance.CloseSelectionMenu();
            string assetPath = AssetDatabase.GetAssetPath(PlayModeManager.Instance.editorDataSO.prefabSaveDirectory);
            if (assetPath.Split("/").Last().Contains("."))
            {
                assetPath = assetPath.Substring(0, assetPath.Length - (assetPath.Split("/").Last().Length+1));
            }

            assetPath += "/";
            string assetName = await ActionManager.Instance.DisplayInputMenu("Prefab name...");
            string prefTag = selectedObject.tag;
            string persID = selectedObject.GetComponent<PersistentID>().uniqueId;
            bool spawnTag = selectedObject.CompareTag("VRG_SpawnableObject");
            if(spawnTag)
                selectedObject.tag = "Untagged";
            DestroyImmediate(selectedObject.GetComponent<PersistentID>());
            DestroyImmediate(selectedObject.GetComponent<XRGrabInteractable>());
            DestroyImmediate(selectedObject.GetComponent<TwoHandGrabTransformer>());
            DestroyImmediate(selectedObject.GetComponent<XRGeneralGrabTransformer>());

            selectedObject.name = assetName;
            assetPath = assetPath + assetName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(selectedObject,assetPath, out var success);
            if (success)
            {
                Debug.Log("Successfully saved selected object");
                PlayModeManager.Instance.editorDataSO.availablePrefabs.Add(selectedObject);
            }

            PersistentID persistentID = selectedObject.AddComponent<PersistentID>();
            persistentID.uniqueId = persID;
            selectedObject.AddComponent<XRGrabInteractable>();
            selectedObject.AddComponent<TwoHandGrabTransformer>();
            selectedObject.AddComponent<XRGeneralGrabTransformer>();
            selectedObject.tag = spawnTag ? "VRG_SpawnableObject" : prefTag;
            

        }

        /**
         * Set the player in the place of the selected camera figure and adjust the size of the player to match the dimensions of the figure
         * Save the original transform of the player
         */
        public void EnterCameraFigure()
        {
            GameObject xrorigin = ActionManager.Instance.xROrigin;
            xrorigin.layer = LayerMask.NameToLayer("Default");
            _originalPosition = xrorigin.transform.position;
            _originalRotation = xrorigin.transform.rotation;
            _originalScale = xrorigin.transform.localScale;
            _originalHeight = xrorigin.transform.GetComponentInChildren<Camera>().transform.position.y;

            foreach (var camFigure in FindObjectsByType<CameraFigure>(FindObjectsSortMode.None))
            {
                camFigure.gameObject.SetActive(false);
            }
            usingCameraFigure = true;
            
            xrorigin.transform.position = currentCameraFigure.transform.position;
            xrorigin.transform.forward = currentCameraFigure.transform.forward;
            xrorigin.transform.localScale = currentCameraFigure.transform.localScale;
            xrorigin.transform.GetComponentInChildren<Camera>().transform.forward = xrorigin.transform.forward;
            xrorigin.transform.GetComponentInChildren<Camera>().transform.parent.position -= new Vector3(0, xrorigin.transform.GetComponentInChildren<Camera>().transform.position.y-currentCameraFigure.transform.GetChild(0).transform.position.y, 0);

            
            ActionManager.Instance.leaningPossible = false;
            ActionManager.Instance.cameraFigureMovement = 1;
            
            ActionManager.Instance.DisplayCameraOverlay("CameraFigure",-1);
            foreach (var comp in xrorigin.GetComponentsInChildren<GrabMoveProvider>())
            {
                comp.enabled = false;
            }
            CharacterController characterController = xrorigin.GetComponent<CharacterController>();
            characterController.radius = currentCameraFigure.GetComponent<CapsuleCollider>().radius;
            characterController.height = currentCameraFigure.GetComponent<CapsuleCollider>().height;
            DeselectObject();
            ActionManager.Instance.CloseSelectionMenu();
        }

        /**
         * Reset the player position and scale to the saved transform before entering the camera figure
         */
        public void ExitCameraFigure()
        {
            ActionManager.Instance.CloseConfirmMenu();
            foreach (var camFigure in FindObjectsByType<CameraFigure>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                camFigure.gameObject.SetActive(true);
            }

            usingCameraFigure = false;

            GameObject xrorigin = ActionManager.Instance.xROrigin;
            xrorigin.layer = LayerMask.NameToLayer("VRG_Player");

            xrorigin.transform.position = _originalPosition;
            xrorigin.transform.rotation = _originalRotation;
            xrorigin.transform.localScale = _originalScale;
            Transform cam = xrorigin.transform.GetComponentInChildren<Camera>().transform;
            xrorigin.transform.GetComponentInChildren<Camera>().transform.position = new Vector3(cam.position.x,_originalHeight,cam.position.z);
            xrorigin.transform.GetComponentInChildren<Camera>().transform.parent.transform.localPosition = Vector3.zero;
            xrorigin.transform.GetComponentInChildren<Camera>().transform.parent.transform.localRotation = Quaternion.Euler(0, 0, 0);
            
            
            ActionManager.Instance.leaningPossible = true;
            ActionManager.Instance.cameraFigureMovement = 0;
            
            ActionManager.Instance.HideCameraOverlay();
            foreach (var comp in xrorigin.GetComponentsInChildren<GrabMoveProvider>(true))
            {
                comp.enabled = true;
            }
            CharacterController characterController = xrorigin.GetComponent<CharacterController>();
            characterController.radius = 0.1f;
            characterController.height = 0.1f;
            PlayModeManager.Instance.RegisterObjectChange(currentCameraFigure);
        }

        /**
         * Place a new camera keyframe at the position and rotation of the player.
         * Initialize movement and rotation time
         */
        public void PlaceCameraKeyframe()
        {
            ActionManager.Instance.CloseConfirmMenu();
            CameraFigure cameraFigure = currentCameraFigure.GetComponent<CameraFigure>();
            CameraKeyFrame cameraKeyFrame = new CameraKeyFrame()
            {
                cameraPosition = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.position,
                cameraRotation = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.rotation,
                cameraMoveTime = 2f,
                cameraRotateTime = 2f
            };
            cameraKeyFrame.prevKeyFrame = cameraFigure.keyFrames.Count > 0 ? cameraFigure.keyFrames[^1] : null;
            cameraFigure.keyFrames.Add(cameraKeyFrame);
        }

        /**
         * Remove selected camera keyframe
         */
        public void DeleteCameraKeyframe(int index)
        {
            CameraFigure cameraFigure = currentCameraFigure.GetComponent<CameraFigure>();
            RemoveKeyFrameDisplays();
            if (index == 0 || index == cameraFigure.keyFrames.Count-1)
            {
                cameraFigure.keyFrames.RemoveAt(index);
            }
            else
            {
                cameraFigure.keyFrames.RemoveAt(index);
                cameraFigure.keyFrames[index+1].prevKeyFrame = cameraFigure.keyFrames[index - 1];
            }
            DisplayCameraKeyframes();
        }

        /**
         * Display all camera keyframes of the selected camera figure in the area
         */
        public void DisplayCameraKeyframes()
        {
            CameraFigure cameraFigure = selectedObject.GetComponent<CameraFigure>();
            currentCameraFigure = selectedObject;
            _keyFrameDisplays = new List<KeyFrameDisplay>();
            foreach (var keyFrame in cameraFigure.keyFrames)
            {
                GameObject keyFrameDisplayObject = Instantiate(keyFrameDisplayPrefab,keyFrame.cameraPosition,keyFrame.cameraRotation);
                KeyFrameDisplay display = keyFrameDisplayObject.AddComponent<KeyFrameDisplay>();
                display.keyFrameIndex = cameraFigure.keyFrames.IndexOf(keyFrame);
                if (cameraFigure.keyFrames.IndexOf(keyFrame) > 0)
                {
                    display.prevKeyFrameDisplay = _keyFrameDisplays[^1].gameObject;
                }
                else
                {
                    display.lineRenderer.SetPosition(1,cameraFigure.transform.GetChild(0).position);
                }
                _keyFrameDisplays.Add(display);
            }
        }

        /**
         * Hide keyframe display from player
         **/
        public void RemoveKeyFrameDisplays()
        {
            foreach (var keyFrameDisplay in _keyFrameDisplays.ToList())
            {
                currentCameraFigure.GetComponent<CameraFigure>().keyFrames[keyFrameDisplay.keyFrameIndex].cameraPosition = keyFrameDisplay.transform.position;
                currentCameraFigure.GetComponent<CameraFigure>().keyFrames[keyFrameDisplay.keyFrameIndex].cameraRotation = keyFrameDisplay.transform.rotation;
                Destroy(keyFrameDisplay.gameObject);
            }
            _keyFrameDisplays.Clear();
        }
        
        
        
    }
}
#endif