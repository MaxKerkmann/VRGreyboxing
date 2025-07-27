using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Transformers;


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
    
    public class PlayerTransformation : MonoBehaviour
    {
        public XRInteractionManager xrInteractionManager;
        public GameObject transWidgetPrefab;
        
        [HideInInspector]
        public TransWidgetEditPoint currentEditPoint;
        [HideInInspector]
        public GameObject selectedObject;
        private bool _justSelected;
        private GameObject _currentTransWidget;
        public Vector3 currentWidgetCenter;
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

        private void Start()
        {
            _leftController = ActionManager.Instance.leftController;
            _rightController = ActionManager.Instance.rightController;
        }
        


        public void PerformGrab(Handedness handedness,XRGrabInteractable interactable)
        {
            if (interactable.gameObject.GetComponent<MeshCollider>())
            {
                interactable.gameObject.GetComponent<MeshCollider>().convex = true;
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

                        //xrInteractionManager.SelectExit(_leftController.GetComponentInChildren<IXRSelectInteractor>(),interactable);
                        interactable.trackRotation = true;
                        
                        xrInteractionManager.SelectEnter(_leftController.GetComponentInChildren<IXRSelectInteractor>(),
                            interactable);
                        xrInteractionManager.SelectEnter(_rightController.GetComponentInChildren<IXRSelectInteractor>(),
                            interactable);
                }
                else
                {
                    //xrInteractionManager.SelectExit(_rightController.GetComponentInChildren<IXRSelectInteractor>(),interactable);
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
            
            if (selectedObject != null)
                DeselectObject();
            
            if(obj.CompareTag("VRG_Mark"))
                selectedObject = obj.transform.parent.gameObject;
            else
                selectedObject = obj;
            
            _selectTransformHandedness = Handedness.None;
            _justSelected = true;
            ApplyTransWidgetToSelectedObject();
        }

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

        public void ApplyTransWidgetToSelectedObject()
        {
            GameObject transWidget = Instantiate(transWidgetPrefab, selectedObject.transform.position,Quaternion.identity);
            if(_currentTransWidget != null)
                Destroy(_currentTransWidget);
            _currentTransWidget = transWidget;
            SetCurrentTransWidgetPositions();
        }

        public void DisableTransWidget(bool exceptCurrentTransWidgetPoint)
        {
            foreach (var editPoint in _currentTransWidget.GetComponentsInChildren<TransWidgetEditPoint>())
            {
                if (exceptCurrentTransWidgetPoint && editPoint == currentEditPoint) continue;
                editPoint.gameObject.SetActive(false);
            }
        }
        
        public void SetCurrentTransWidgetPositions()
        {
            Quaternion originalRotation = selectedObject.transform.rotation;
            selectedObject.transform.rotation = _currentTransWidget.transform.rotation;
            bool enabledCollider = false;
            if (selectedObject.GetComponent<Collider>().enabled == false)
            {
                selectedObject.GetComponent<Collider>().enabled = true;
                enabledCollider = true;
            }

            Bounds objBounds = selectedObject.GetComponent<Renderer>() != null ? selectedObject.GetComponent<Renderer>().bounds : selectedObject.GetComponent<Collider>().bounds;
            foreach (var childBounds in selectedObject.GetComponentsInChildren<Renderer>())
            {
                objBounds.Encapsulate(childBounds.bounds);
            }
            foreach (var childBounds in selectedObject.GetComponentsInChildren<Collider>())
            {
                objBounds.Encapsulate(childBounds.bounds);
            }
            Vector3 rotationWidgetPointCounter = Vector3.zero;
            int cornerScaleCounter = 0;
            Transform t = _currentTransWidget.transform;
            Vector3 e = objBounds.extents;
            currentWidgetCenter = objBounds.center;
            foreach (var editPoint in _currentTransWidget.GetComponentsInChildren<TransWidgetEditPoint>())
            {
                editPoint.playerTransformation = this;
                editPoint.transform.localScale *= ActionManager.Instance.GetCurrentSizeRatio();
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
                            case 0://LUF
                                editPoint.transform.position -= t.right * e.x + t.up * e.y+t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                            case 1://RUF
                                editPoint.transform.position += t.right * e.x + t.up * e.y+t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                            case 2://RUB
                                editPoint.transform.position += t.right * e.x + t.up * e.y-t.forward * e.z;
                                cornerScaleCounter++;   
                                break;
                            case 3://LLF
                                editPoint.transform.position -= t.right * e.x - t.up * e.y+t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                            case 4://RLF
                                editPoint.transform.position += t.right * e.x - t.up * e.y+t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                            case 5://LLB
                                editPoint.transform.position -= t.right * e.x - t.up * e.y-t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                            case 6://RLB
                                editPoint.transform.position += t.right * e.x - t.up * e.y-t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                            case 7://LUB
                                editPoint.transform.position -= t.right * e.x + t.up * e.y-t.forward * e.z;
                                cornerScaleCounter++;
                                break;
                        }
                        break;
                }
            }
            selectedObject.transform.rotation = _currentTransWidget.transform.rotation = originalRotation;
            if(enabledCollider)
                selectedObject.GetComponent<Collider>().enabled = false;

        }
        
        public void DeselectObject()
        {
            Destroy(_currentTransWidget); 
            selectedObject = null;
        }
        
        
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
        public void ScaleSelectedObjectFromCorner(Vector3 fixedCornerWS, Vector3 transWidgetPointWS, Vector3 cornerPointWS, GameObject scaleObject)
        {
            var oldVector = fixedCornerWS - cornerPointWS;
            var rotatedOldVector = Quaternion.Inverse(selectedObject.transform.rotation) * oldVector;
            if (Math.Abs(rotatedOldVector.x) <= 0.01)
                rotatedOldVector.x = 0;
            if (Math.Abs(rotatedOldVector.y) <= 0.01)
                rotatedOldVector.y = 0;
            if (Math.Abs(rotatedOldVector.z) <= 0.01)
                rotatedOldVector.z = 0;
            
            Debug.Log(oldVector);

            float oldDiagonal = Vector3.Distance(fixedCornerWS,cornerPointWS);
            float newDiagonal = Vector3.Distance(fixedCornerWS, transWidgetPointWS);
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
        public GameObject CreatePrefabFromMenu(GameObject prefab,Transform buttonTransform)
        {
            GameObject go = Instantiate(prefab,buttonTransform.position,Quaternion.identity);
            go.transform.up = Vector3.up;
            go.transform.localScale *= ActionManager.Instance.GetCurrentSizeRatio();
            
            go.AddComponent<TwoHandGrabTransformer>();
            XRGrabInteractable interactable = go.GetComponent<XRGrabInteractable>();
            if(interactable == null)
                 interactable = go.AddComponent<XRGrabInteractable>();
            ActionManager.Instance.SetupInteractable(interactable);
            
            return go;
        }

        public void DeleteSelectedObject()
        {
            PlayModeManager.Instance.RegisterObjectChange(selectedObject,false,-1,false,true);
            Destroy(_currentTransWidget); 
            selectedObject.SetActive(false);
            selectedObject = null;
            ActionManager.Instance.CloseSelectionMenu();
        }

        public GameObject DuplicateSelectedObject(Handedness handedness)
        {
            ActionManager.Instance.CloseSelectionMenu();
            GameObject go = Instantiate(selectedObject,selectedObject.transform.position,Quaternion.identity);
            Destroy(go.GetComponent<PersistentID>());
            return go;
        }
        

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
            if(spawnTag)
                selectedObject.tag = "VRG_SpawnableObject";

        }

        public void EnterCameraFigure()
        {
            GameObject xrorigin = ActionManager.Instance.xROrigin;
            xrorigin.layer = LayerMask.NameToLayer("Default");
            currentCameraFigure = selectedObject;
            _originalPosition = xrorigin.transform.position;
            _originalRotation = xrorigin.transform.rotation;
            _originalScale = xrorigin.transform.localScale;
            _originalHeight = xrorigin.transform.GetComponentInChildren<Camera>().transform.position.y;


            xrorigin.transform.position = currentCameraFigure.transform.position + Vector3.down;
            xrorigin.transform.forward = currentCameraFigure.transform.forward;
            xrorigin.transform.GetComponentInChildren<Camera>().transform.position = Vector3.Scale(currentCameraFigure.transform.GetChild(0).transform.position, Vector3.up);
            xrorigin.transform.localScale = currentCameraFigure.transform.localScale;
            
            ActionManager.Instance.leaningPossible = false;
            ActionManager.Instance.cameraFigureMovement = 1;
            
            xrorigin.GetComponentInChildren<TextMeshPro>(true).enabled = true;
            foreach (var comp in xrorigin.GetComponentsInChildren<GrabMoveProvider>())
            {
                comp.enabled = false;
            }
            CharacterController characterController = xrorigin.GetComponent<CharacterController>();
            characterController.radius = currentCameraFigure.GetComponent<CapsuleCollider>().radius;
            characterController.height = currentCameraFigure.GetComponent<CapsuleCollider>().height;
            DeselectObject();
            currentCameraFigure.SetActive(false);
            ActionManager.Instance.CloseSelectionMenu();
        }

        public void ExitCameraFigure()
        {
            ActionManager.Instance.CloseConfirmMenu();
            currentCameraFigure.SetActive(true);

            GameObject xrorigin = ActionManager.Instance.xROrigin;
            xrorigin.layer = LayerMask.NameToLayer("VRG_Player");

            xrorigin.transform.position = _originalPosition;
            xrorigin.transform.rotation = _originalRotation;
            xrorigin.transform.localScale = _originalScale;
            Transform cam = xrorigin.transform.GetComponentInChildren<Camera>().transform;
            xrorigin.transform.GetComponentInChildren<Camera>().transform.position = new Vector3(cam.position.x,_originalHeight,cam.position.z);
            
            ActionManager.Instance.leaningPossible = true;
            ActionManager.Instance.cameraFigureMovement = 0;
            
            xrorigin.GetComponentInChildren<TextMeshPro>(true).enabled = false;
            foreach (var comp in xrorigin.GetComponentsInChildren<GrabMoveProvider>(true))
            {
                comp.enabled = true;
            }
            CharacterController characterController = xrorigin.GetComponent<CharacterController>();
            characterController.radius = 0.1f;
            characterController.height = 0.1f;
            currentCameraFigure = null;
        }
        
    }
}
#endif