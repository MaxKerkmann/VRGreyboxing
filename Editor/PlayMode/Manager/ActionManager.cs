using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Object = UnityEngine.Object;

#if UNITY_EDITOR

namespace VRGreyboxing
{
    /**
     * Manager class containing main logic to handle player input and display ui
     */
    public class ActionManager : MonoBehaviour
    {
        
        public static ActionManager Instance;
        public GameObject xROrigin;
        
        public XRInteractionManager xrInteractionManager;
        
        //UI prefabs and instances
        public GameObject menuOptionButtonPrefab;
        
        public GameObject inventoryMenuPrefab;
        private GameObject _inventoryInstance;

        public GameObject selectionMenuPrefab;
        public Sprite selectionMenuDeletionIcon;
        public Sprite selectionMenuSaveIcon;
        public Sprite seletionMenuDuplicateIcon;
        public Sprite selectionMenuCameraIcon;
        private GameObject _selectionMenuInstance;
        public GameObject selectionMenuLoadingProgressPrefab;
        private GameObject _selectionMenuLoadingProgressInstance;
        public GameObject keyFrameMenuPrefab;
        private GameObject _keyFrameMenuInstance;
        
        public GameObject inputFieldPrefab;
        public GameObject keyboardPrefab;
        private bool _shift;
        private List<KeyboardKey> _keyCaps;
        private Image _shiftKeyImage;
        private bool _textSubmitted;
        
        public GameObject confirmMenuPrefab;
        private GameObject _confirmMenuInstance;
        public Sprite confirmConfirmIcon;
        public Sprite confirmCancelIcon;
        private int _confirmReturn;
        
        private EditMode _currentEditMode;
        public GameObject mainMenuPrefab;
        public List<Sprite> mainMenuOptionSprites;
        public List<string> mainMenuOptionNames;
        private GameObject _mainMenuInstance;
        private Handedness _mainMenuHandedness;
        private bool _mainMenuChangedMode;
        
        private GameObject _sceneMenuInstance;
        public GameObject sceneMenuPrefab;
        public GameObject sceneSelectButtonPrefab;
        private int _sceneMenuIndex;
        private int _closeIndex;

        private GameObject _drawingConfirmMenuInstance;
        private GameObject _colorPickerInstance;
        public GameObject colorPickerPrefab;
        
        public GameObject scaleMenuPrefab;
        private GameObject _scaleMenuInstance;
        
        public GameObject movementOptionsMenuPrefab;
        private GameObject _movementOptionsMenuInstance;
        
        private PlayerNavigation _playerNavigation;
        private PlayerTransformation _playerTransformation;
        private PlayerEdit _playerEdit;
        private PlayerCommunication _playerCommunication;

        private bool _performedTeleport;
        
        public bool leaningPossible;

        
        //PlayerInputs
        private VRGreyboxingInput playerInput;
        private InputAction i_grabLeft;
        private InputAction i_grabRight;
        private InputAction i_triggerLeft;
        private InputAction i_triggerRight;
        private InputAction i_grabMoveLeft;
        private InputAction i_grabMoveRight;
        private InputAction i_axLeft;
        private InputAction i_axRight;
        private InputAction i_byLeft;
        private InputAction i_byRight;
        private InputAction i_StickPressLeft;
        private InputAction i_LeftStick;
        private InputAction i_StickPressRight;
        private InputAction i_RightStick;
        
        //Input Memory
        private float _graceTimeLeft;
        private float _graceTimeRight;
        private bool _leftHandSingleInput;
        private bool _rightHandSingleInput;
        private float _leftHandSelectMenuTime;
        private float _rightHandSelectMenuTime;
        private bool _leftHandSelectInput;
        private bool _rightHandSelectInput;
        private bool _leftHandNoSelectionTarget;
        private bool _rightHandNoSelectionTarget;
        private bool _undoInput;
        private bool _redoInput;
        private float _undoTimeLeft;
        private float _redoTimeRight;
        private bool _performedUndoRedo;
        public int cameraFigureMovement;
        private float _cameraOverlayTime;

        private List<VertexEditPoint> _currentPolyShapePoints;
        private GameObject _currentPolyShape;
        private Handedness _currentPolyShapeHandedness;
        
        [HideInInspector]
        public bool twoHandGrab;
        private bool _teleportLeft;
        private bool _grabLeft;
        private bool _grabRight;
        private bool _triggerLeft;
        private bool _triggerRight;

        private bool _objectSpawnedLeft;
        private bool _objectSpawnedRight;
        private int _lastPrefabIndex;
        private string _lastDuplicationID;
        private bool _mainMenuChanged;
        private bool _inventoryChanged;
        private bool _colorPickerChanged;
        private bool _drawingConfirmMenuChanged;
        private bool _cameraExitMenuChanged;
        private bool _cameraKeyFrameMenuChanged;
        private bool _movementOptionsChanged;
        
        //Controller
        public GameObject leftController;
        public GameObject rightController;
        public XRControllerRaycaster leftControllerRaycaster;
        public XRControllerRaycaster rightControllerRaycaster;
        
        private GameObject _leftHandHoverObject;
        private GameObject _rightHandHoverObject;
        
        private GameObject _leftHandHoveredXRObject;
        private GameObject _rightHandHoveredXRObject;
        private GameObject _leftHandSelectedXRObject;
        private GameObject _rightHandSelectedXRObject;
        
        private GameObject _leftHandHoveredUI;
        private GameObject _rightHandHoveredUI;
        
        private void Awake()
        {
            Instance = this;
            _currentEditMode = EditMode.Transformation;
            playerInput = new VRGreyboxingInput();
            _playerNavigation = GetComponent<PlayerNavigation>();
            _playerTransformation = GetComponent<PlayerTransformation>();
            _playerCommunication = GetComponent<PlayerCommunication>();
            _playerEdit = GetComponent<PlayerEdit>();
            ResetSelectionMenuTime(Handedness.None);
            ResetGraceTime(Handedness.None);
            ResetUndoRedoTime(Handedness.None);
            _lastPrefabIndex = -1;
            
        }
        

        private void OnEnable()
        {
            i_grabLeft = playerInput.VRGreyboxing.GrabLeft;
            i_grabLeft.Enable();
            i_grabRight = playerInput.VRGreyboxing.GrabRight;
            i_grabRight.Enable();
            i_triggerLeft = playerInput.VRGreyboxing.TriggerLeft;
            i_triggerLeft.Enable();
            i_triggerRight = playerInput.VRGreyboxing.TriggerRight;
            i_triggerRight.Enable();
            i_grabMoveLeft = playerInput.VRGreyboxing.GrabMoveLeft;
            i_grabMoveLeft.Enable();
            i_grabMoveRight = playerInput.VRGreyboxing.GrabMoveRight;
            i_grabMoveRight.Enable();
            i_axLeft = playerInput.VRGreyboxing.AXLeft;
            i_axLeft.Enable();
            i_axRight = playerInput.VRGreyboxing.AXRight;
            i_axRight.Enable();
            i_byLeft = playerInput.VRGreyboxing.BYLeft;
            i_byLeft.Enable();
            i_byRight = playerInput.VRGreyboxing.BYRight;
            i_byRight.Enable();
            i_StickPressLeft = playerInput.VRGreyboxing.StickPressLeft;
            i_StickPressLeft.Enable();
            i_LeftStick = playerInput.VRGreyboxing.StickLeft;
            i_LeftStick.Enable();
            i_StickPressRight = playerInput.VRGreyboxing.StickPressRight;
            i_StickPressRight.Enable();
            i_RightStick = playerInput.VRGreyboxing.StickRight;
            i_RightStick.Enable();
        }
        private void OnDisable()
        {
            i_grabLeft.Disable();
            i_grabRight.Disable();
            i_triggerLeft.Disable();
            i_triggerRight.Disable();
            i_grabMoveLeft.Disable();
            i_grabMoveRight.Disable();
            i_axLeft.Disable();
            i_axRight.Disable();
            i_byLeft.Disable();
            i_byRight.Disable();
            i_RightStick.Disable();
            i_LeftStick.Disable();
            i_StickPressLeft.Disable();
            i_StickPressRight.Disable();
        }

        public void Start()
        {
            DisplaySceneSelectionMenu(new Vector3(0,1,0));
            leaningPossible = true;
        }

        /**
         * Check for player inputs according to selected edit mode from main menu
         */
        private void Update()
        {
            
            //Grace Timer
            _graceTimeLeft = _leftHandSingleInput ? _graceTimeLeft - Time.deltaTime : _graceTimeLeft;
            _graceTimeRight = _rightHandSingleInput ? _graceTimeRight - Time.deltaTime : _graceTimeRight;
            
            //Selection Menu Timer
            _leftHandSelectMenuTime = _leftHandSelectInput ? _leftHandSelectMenuTime - Time.deltaTime : _leftHandSelectMenuTime;
            _rightHandSelectMenuTime = _rightHandSelectInput ? _rightHandSelectMenuTime - Time.deltaTime : _rightHandSelectMenuTime;
            
            //Undo Redo Timer
            _undoTimeLeft = _undoInput ? _undoTimeLeft - Time.deltaTime : _undoTimeLeft;
            _redoTimeRight = _redoInput ? _redoTimeRight - Time.deltaTime : _redoTimeRight;

            if (_cameraOverlayTime <= 0 && !Mathf.Approximately(_cameraOverlayTime, -1))
            {
                HideCameraOverlay();
            }
            else
            {
                _cameraOverlayTime -= Time.deltaTime;
            }
            
            if(HandleZoomRotation()) return;

            if (PlayModeManager.Instance.editorDataSO.restrictToStickMovement)
            {
                foreach (var grabMoveProvider in xROrigin.GetComponentsInChildren<GrabMoveProvider>())
                {
                    grabMoveProvider.enabled = false;
                }
                _playerNavigation.PerformLinearMovement(i_LeftStick.ReadValue<Vector2>());
                _playerNavigation.PerformFlyingMovement(i_RightStick.ReadValue<Vector2>());
            }

            if (PlayModeManager.Instance.editorDataSO.restrictToTeleport)
            {
                foreach (var grabMoveProvider in xROrigin.GetComponentsInChildren<GrabMoveProvider>())
                {
                    grabMoveProvider.enabled = false;
                }
            }
            
            
            bool grabMove = HandleGrabMove();

            
            if(cameraFigureMovement == 0 && !PlayModeManager.Instance.editorDataSO.restrictToStickMovement)
                HandleTeleport();

            if (cameraFigureMovement == 0 && !PlayModeManager.Instance.editorDataSO.restrictToStickMovement)
            {
                foreach (var grabMoveProvider in xROrigin.GetComponentsInChildren<GrabMoveProvider>())
                {
                    grabMoveProvider.enabled = true;
                }
            }
            
            if (cameraFigureMovement>5)
                _playerNavigation.PerformLinearMovement(i_LeftStick.ReadValue<Vector2>());
            else if(leaningPossible && PlayModeManager.Instance.editorDataSO.enableStickLeaning)
                _playerNavigation.PerformLeaning(i_LeftStick.ReadValue<Vector2>());
            
            if (cameraFigureMovement > 0 && !PlayModeManager.Instance.editorDataSO.restrictToStickMovement && _playerNavigation.currentCameraKeyFrame == null) cameraFigureMovement++;

            
            if (!_playerTransformation.usingCameraFigure)
            {
                HandleMainMenu();
            }

            HandleUndoRedo();
            
            if (_currentEditMode == EditMode.Transformation)
            {
                if (!_playerTransformation.usingCameraFigure)
                {
                    if(!grabMove) HandleInventory();
                    HandleMovementOptionsMenu();
                }
                else
                {
                    HandleCameraFigureExit();
                    if(!grabMove) HandleCameraFigureKeyFrameMenu();
                }
                
                HandleTransformGrab();
                HandleTransformSelect();
            }
            if (_currentEditMode == EditMode.Edit)
            {
                _playerEdit.CheckForVertexPositions();
                HandleEditGrab();
                HandleEditSelect();
                if (_currentPolyShape != null)
                {
                    _playerEdit.ScaleCurrentPolyShape(_currentPolyShapeHandedness,_currentPolyShape,_currentPolyShapePoints);
                }
            }
            if (_currentEditMode == EditMode.Communicate)
            {
                HandleComSelect();
                HandleColorMenu();
                if(!grabMove) HandleDrawConfirmMenu();
                HandleComGrab();
            }

            if (!CheckBackButtonPress(Handedness.Left))
            {
                if(_leftHandSingleInput)
                    ResetGraceTime(Handedness.Left);
                if(_leftHandSelectInput)
                    ResetSelectionMenuTime(Handedness.Left);
            }
            if (!CheckBackButtonPress(Handedness.Right))
            {
                if(_rightHandSingleInput)
                    ResetGraceTime(Handedness.Right);
                if(_rightHandSelectInput)
                    ResetSelectionMenuTime(Handedness.Right);
            }
            
            if(_mainMenuChangedMode)
                ResetGraceTime(Handedness.None);
            
        }
        

        private void LateUpdate()
        {
            LineRenderer leftConLineRenderer = leftController.GetComponentInChildren<NearFarInteractor>().GetComponentInChildren<LineRenderer>(true);
            LineRenderer rightConLineRenderer = rightController.GetComponentInChildren<NearFarInteractor>().GetComponentInChildren<LineRenderer>(true);

            if (_currentEditMode == EditMode.Edit && _leftHandHoveredXRObject != null &&  _leftHandHoveredXRObject.GetComponent<EditWidgetEditPoint>() == null && _leftHandHoveredXRObject.GetComponent<ProBuilderMesh>() == null)
            {
                leftConLineRenderer.startColor = leftConLineRenderer.endColor = Color.red;
            }
            if (_currentEditMode == EditMode.Edit && _rightHandHoveredXRObject != null &&  _rightHandHoveredXRObject.GetComponent<EditWidgetEditPoint>() == null && _rightHandHoveredXRObject.GetComponent<ProBuilderMesh>() == null)
            {
                rightConLineRenderer.startColor = rightConLineRenderer.endColor = Color.red;
            }
        }

        #region Input

        private bool CheckBackButtonPress(Handedness handedness)
        {
            if (handedness == Handedness.Left)
            {
                if(i_triggerLeft.IsPressed() || i_grabLeft.IsPressed())
                    return true;
            }
            else
            {
                if(i_triggerRight.IsPressed() || i_grabRight.IsPressed())
                    return true;
            }
            return false;
        }
        
        private void HandleMainMenu()
        {
            if (_mainMenuInstance == null)
            {
                if ((i_StickPressLeft.IsPressed()|| i_StickPressRight.IsPressed()) && !_mainMenuChanged)
                {
                    _mainMenuChanged = true;
                    _mainMenuHandedness = i_StickPressLeft.IsPressed() ? Handedness.Left : Handedness.Right;
                    DisplayMainMenu(i_StickPressLeft.IsPressed() ? leftController : rightController);
                }
                else if (!i_StickPressLeft.IsPressed() && !i_StickPressRight.IsPressed())
                {
                    _mainMenuChanged = false;
                }
            }
            else
            {
                if ((i_StickPressLeft.IsPressed()|| i_StickPressRight.IsPressed()) && !_mainMenuChanged)
                {
                    CloseMainMenu();
                    _mainMenuHandedness = Handedness.None;
                    _mainMenuChanged = true;
                }
                else if (!i_StickPressLeft.IsPressed() && !i_StickPressRight.IsPressed())
                {
                    _mainMenuChanged = false;
                }

                if (i_triggerLeft.IsPressed() && _mainMenuHandedness == Handedness.Left)
                {
                    SelectMainMenuOption();
                }

                if (i_triggerRight.IsPressed() && _mainMenuHandedness == Handedness.Right)
                {
                    SelectMainMenuOption();
                }
            }
        }

        private void SelectMainMenuOption()
        {
            int selectedOption = _mainMenuInstance.GetComponent<RadialSelection>().currentSelectedPart;
            CloseMainMenu();
            HandleMainMenuSelection(selectedOption);
            _mainMenuHandedness = Handedness.None;
        }

        private void HandleUndoRedo()
        {
            if (i_triggerLeft.WasReleasedThisFrame() && !_undoInput)
            {
                _undoInput = true;
            }
            if (i_triggerLeft.IsPressed() && _undoInput && _undoTimeLeft > 0)
            {
                DeselectObject();
                PlayModeManager.Instance.UndoObjectChange();
                _undoInput = false;
                _performedUndoRedo = true;
                ResetUndoRedoTime(Handedness.Left);
            }
            
            if (i_triggerRight.WasReleasedThisFrame() && !_redoInput)
            {
                _redoInput = true;
            }
            if (i_triggerRight.IsPressed() && _redoInput && _redoTimeRight > 0)
            {
                DeselectObject();
                PlayModeManager.Instance.RedoObjectChange();
                _redoInput = false;
                _performedUndoRedo = true;
                ResetUndoRedoTime(Handedness.Right);
            }
            

            if (_undoTimeLeft < 0 || _performedUndoRedo)
            {
                ResetUndoRedoTime(Handedness.Left);
                _undoInput = false;
            }

            if (_redoTimeRight < 0 || _performedUndoRedo)
            {
                ResetUndoRedoTime(Handedness.Right);
                _redoInput = false;
            }
            _performedUndoRedo = false;
        }
        
        private void HandleTransformSelect()
        {
            if (i_triggerLeft.IsPressed() && !_mainMenuChangedMode)
            {
                _leftHandSingleInput = true; 
                if (_leftHandHoverObject == null && _leftHandHoveredUI == null)
                {
                    _leftHandNoSelectionTarget = true;
                }
                if (_objectSpawnedLeft)
                {
                    if (_lastPrefabIndex == -1)
                    {
                        if(_lastDuplicationID != "")
                            PlayModeManager.Instance.RegisterObjectChange(_leftHandSelectedXRObject, false, _lastPrefabIndex,false,false,_lastDuplicationID);
                        else
                            PlayModeManager.Instance.RegisterObjectChange(_leftHandSelectedXRObject);
                    }
                    else
                    {
                        PlayModeManager.Instance.RegisterObjectChange(_leftHandSelectedXRObject, false, _lastPrefabIndex);
                    }
                    _lastPrefabIndex = -1;
                    _playerTransformation.EndGrab(Handedness.Left);
                    _objectSpawnedLeft = false;
                }
                else if (_leftHandHoverObject != null && _leftHandHoverObject == _leftHandHoveredXRObject && _leftHandHoveredUI == null && !_leftHandNoSelectionTarget)
                {
                    _leftHandSelectInput = true;
                    if (_graceTimeLeft <= 0)
                    {
                        SelectObject(Handedness.Left, _leftHandHoveredXRObject);
                        if(_playerTransformation.currentSelectedTransWidgetPoint == null && _leftHandHoveredXRObject.GetComponent<PersistentID>()!=null)
                            PlayModeManager.Instance.RegisterObjectChange(_leftHandHoveredXRObject,true);
                        _triggerLeft = true;
                    }

                    if (_playerTransformation.currentSelectedTransWidgetPoint == null)
                    {
                        if (_leftHandSelectMenuTime <= 0)
                        {
                            if (_selectionMenuInstance == null && _playerTransformation.selectedObject != null)
                                DisplaySelectionMenu();
                            CloseSelectionLoadingProgress();
                        }
                        else if (_leftHandSelectMenuTime < _rightHandSelectMenuTime * 0.75)
                        {
                            DisplaySelectionLoadingProgress(_leftHandSelectMenuTime,_rightHandSelectMenuTime);

                        }
                    }
                }
            }
            else if (i_triggerRight.IsPressed() && !_mainMenuChangedMode)
            {
                _rightHandSingleInput = true;
                if (_rightHandHoverObject == null && _rightHandHoveredUI == null)
                {
                    _rightHandNoSelectionTarget = true;
                }
                if (_objectSpawnedRight)
                {
                    if (_lastPrefabIndex == -1)
                    {
                        if(_lastDuplicationID != "")
                            PlayModeManager.Instance.RegisterObjectChange(_rightHandSelectedXRObject, false, _lastPrefabIndex,false,false,_lastDuplicationID);
                        else
                            PlayModeManager.Instance.RegisterObjectChange(_rightHandSelectedXRObject);
                    }
                    else
                    {
                        PlayModeManager.Instance.RegisterObjectChange(_rightHandSelectedXRObject, false, _lastPrefabIndex);
                    }
                    _lastPrefabIndex = -1;
                    _playerTransformation.EndGrab(Handedness.Right);
                    _objectSpawnedRight = false;
                }
                else if (_rightHandHoverObject != null && _rightHandHoverObject == _rightHandHoveredXRObject && _rightHandHoveredUI == null && !_rightHandNoSelectionTarget)
                {
                    _rightHandSelectInput = true;
                    if (_graceTimeRight <= 0)
                    {
                        SelectObject(Handedness.Right, _rightHandHoveredXRObject);
                        if(_playerTransformation.currentSelectedTransWidgetPoint == null && _rightHandHoveredXRObject.GetComponent<PersistentID>()!=null)
                            PlayModeManager.Instance.RegisterObjectChange(_rightHandHoveredXRObject,true);
                        _triggerRight = true;
                    }

                    if (_playerTransformation.currentSelectedTransWidgetPoint == null)
                    {
                        if (_rightHandSelectMenuTime <= 0)
                        {
                            if (_selectionMenuInstance == null && _playerTransformation.selectedObject != null)
                                DisplaySelectionMenu();
                            CloseSelectionLoadingProgress();
                        }
                        else if (_rightHandSelectMenuTime < _leftHandSelectMenuTime * 0.75)
                        {
                            DisplaySelectionLoadingProgress(_rightHandSelectMenuTime,_leftHandSelectMenuTime);
                        }
                    }
                }
            }
            else
            {
                if (_triggerLeft)
                {
                    _triggerLeft = false;
                    if (_playerTransformation.currentSelectedTransWidgetPoint != null && _playerTransformation.selectedObject.GetComponent<PersistentID>()!=null)
                        PlayModeManager.Instance.RegisterObjectChange(_playerTransformation.selectedObject);

                    _playerTransformation.EndSelection(_leftHandHoveredXRObject);

                }
                else if (_triggerRight)
                {
                    _triggerRight = false;
                    if (_playerTransformation.currentSelectedTransWidgetPoint != null && _playerTransformation.selectedObject.GetComponent<PersistentID>()!=null)
                        PlayModeManager.Instance.RegisterObjectChange(_playerTransformation.selectedObject);

                    _playerTransformation.EndSelection(_rightHandHoveredXRObject);

                }
                
                _leftHandNoSelectionTarget = _rightHandNoSelectionTarget = false;
                _performedUndoRedo = false;
                
                
                if(!i_triggerLeft.IsPressed() && !i_triggerRight.IsPressed())
                    _mainMenuChangedMode = false; 
                
                CloseSelectionLoadingProgress();
            }
        }
        
        private void HandleEditSelect()
        {
            if (i_triggerLeft.IsPressed() && !_mainMenuChangedMode)
            {
                _leftHandSingleInput = true; 
                if (_leftHandHoverObject == null && _leftHandHoveredUI == null)
                {
                    _leftHandNoSelectionTarget = true;
                }
                if (_leftHandHoverObject != null && _leftHandHoverObject == _leftHandHoveredXRObject && _leftHandHoveredUI == null && !_leftHandNoSelectionTarget)
                {
                    if(_leftHandHoverObject.GetComponent<ProBuilderMesh>()==null) return;
                    if (_graceTimeLeft <= 0)
                    {
                        SelectObject(Handedness.Left, _leftHandHoveredXRObject);
                        if(_playerEdit.currentSelectedEditWidgetPoint == null && _leftHandHoveredXRObject.GetComponent<PersistentID>()!=null)
                            PlayModeManager.Instance.RegisterObjectChange(_leftHandHoveredXRObject,true);
                        _triggerLeft = true;
                    }
                }
            }
            else if(i_triggerRight.IsPressed() && !_mainMenuChangedMode)
            {
                _rightHandSingleInput = true;
                if (_rightHandHoverObject == null && _rightHandHoveredUI == null)
                {
                    _rightHandNoSelectionTarget = true;
                }
                if (_rightHandHoverObject != null && _rightHandHoverObject == _rightHandHoveredXRObject && _rightHandHoveredUI == null && !_rightHandNoSelectionTarget)
                {
                    if(_rightHandHoverObject.GetComponent<ProBuilderMesh>()==null) return;
                    if (_graceTimeRight <= 0)
                    {
                        SelectObject(Handedness.Right, _rightHandHoveredXRObject);
                        if(_playerEdit.currentSelectedEditWidgetPoint == null && _rightHandHoveredXRObject.GetComponent<PersistentID>()!=null)
                            PlayModeManager.Instance.RegisterObjectChange(_rightHandHoveredXRObject,true);
                        _triggerRight = true;
                    }
                }
            }
            else
            {
                if (_triggerLeft)
                {
                    _triggerLeft = false;
                    if(_playerEdit.currentSelectedEditWidgetPoint != null)
                        PlayModeManager.Instance.RegisterObjectChange(GetSelectedObject());
                    _playerEdit.EndSelection(_leftHandHoveredXRObject);
                }
                else if (_triggerRight)
                {
                    _triggerRight = false;
                    if(_playerEdit.currentSelectedEditWidgetPoint != null)
                        PlayModeManager.Instance.RegisterObjectChange(GetSelectedObject());
                    _playerEdit.EndSelection(_rightHandHoveredXRObject);
                }
                
                _leftHandNoSelectionTarget = _rightHandNoSelectionTarget = false;
                

                
                if(!i_triggerLeft.IsPressed() && !i_triggerRight.IsPressed()) 
                    _mainMenuChangedMode = false; 

            }
        }

        private void HandleEditGrab()
        {
            if (i_grabLeft.IsPressed() && !_grabLeft)
            {
                _leftHandSingleInput = true; 
                if (_graceTimeLeft <= 0)
                {
                    if (_currentPolyShape != null)
                    {
                        _playerEdit.CenterPolyshapePosition(_currentPolyShape);
                        SelectObject(Handedness.Left, _currentPolyShape);
                        string parentID = "";
                        if (_currentPolyShape.transform.parent != null)
                            parentID = _currentPolyShape.transform.parent.GetComponent<PersistentID>().uniqueId;
                        
                        PlayModeManager.Instance.RegisterObjectChange(_currentPolyShape,false,-1,true,false,"",parentID,_currentPolyShapePoints.Select(p => p.transform.position).ToList(),_playerEdit.flipVertices);
                        _currentPolyShape = null;
                        _playerEdit.flipVertices = false;
                        foreach (var vertexEdit in _currentPolyShapePoints.ToList())      
                        {
                            Destroy(vertexEdit.gameObject);
                        }
                    }
                    else
                    {
                        _currentPolyShapePoints = _playerEdit.PlaceSelectVertex(Handedness.Left);
                        _currentPolyShapeHandedness = Handedness.Left;
                    }
                    _grabLeft = true;

                }
            }else if (_grabLeft && !i_grabLeft.IsPressed())
            {
                _grabLeft = false;
            }
            
            if (i_grabRight.IsPressed() && !_grabRight)
            {
                _rightHandSingleInput = true;
                if (_graceTimeRight <= 0)
                {
                    if (_currentPolyShape != null)
                    {
                        _playerEdit.CenterPolyshapePosition(_currentPolyShape);
                        SelectObject(Handedness.Right, _currentPolyShape);
                        string parentID = "";
                        if (_currentPolyShape.transform.parent != null)
                            parentID = _currentPolyShape.transform.parent.GetComponent<PersistentID>().uniqueId;
                        PlayModeManager.Instance.RegisterObjectChange(_currentPolyShape,false,-1,true,false,"",parentID,_currentPolyShapePoints.Select(p => p.transform.position).ToList());
                        _currentPolyShape = null;
                        foreach (var vertexEdit in _currentPolyShapePoints.ToList())      
                        {
                            Destroy(vertexEdit.gameObject);
                        }
                    }
                    else
                    {
                        _currentPolyShapePoints = _playerEdit.PlaceSelectVertex(Handedness.Right);
                        _currentPolyShapeHandedness = Handedness.Right;
                    }
                    _grabRight = true;

                }
            }else if (_grabRight && !i_grabRight.IsPressed())
            {
                _grabRight = false;
            }
        }
        
        private void HandleTransformGrab()
        {
            if (i_grabLeft.IsPressed() && _leftHandHoverObject != null && _leftHandHoverObject == _leftHandHoveredXRObject)
            {
                _leftHandSingleInput = true; 
                if (_graceTimeLeft <= 0)
                {
                    if(_leftHandHoveredXRObject.CompareTag("VRG_WidgetCube")) return;
                    if(_leftHandHoveredXRObject.GetComponent<KeyFrameDisplay>() == null)
                        _playerTransformation.DeselectObject();
                    _playerTransformation.PerformGrab(Handedness.Left, _leftHandHoveredXRObject.GetComponent<XRGrabInteractable>());
                    if(_leftHandHoveredXRObject.GetComponent<PersistentID>()!=null)
                        PlayModeManager.Instance.RegisterObjectChange(_leftHandHoveredXRObject,true);
                    _grabLeft = true;
                    if (_grabRight && _rightHandHoveredXRObject == _leftHandHoveredXRObject)
                    {
                        twoHandGrab = true;
                    }
                }
            }else if (_grabLeft && !i_grabLeft.IsPressed())
            {
                if(_leftHandHoveredXRObject.GetComponent<PersistentID>()!=null)
                    PlayModeManager.Instance.RegisterObjectChange(_leftHandHoveredXRObject);
                GameObject grabbedObject = _playerTransformation.leftgrabbedObject;
                _playerTransformation.EndGrab(Handedness.Left);
                if (grabbedObject.CompareTag("VRG_Mark") && !_grabRight)
                {
                    _playerCommunication.EncapsulateDrawing(grabbedObject.transform.parent.GetComponent<BoxCollider>(),grabbedObject.GetComponent<IdHolderInformation>().GetIDHolder().GetComponentsInChildren<Collider>().Where(c => c.transform != grabbedObject.transform.parent.transform).ToArray());
                }
                _grabLeft = false;
                twoHandGrab = false;
            }

            if (i_grabRight.IsPressed() && _rightHandHoverObject != null && _rightHandHoverObject == _rightHandHoveredXRObject)
            {
                _rightHandSingleInput = true;
                if (_graceTimeRight <= 0)
                {
                    if(_rightHandHoveredXRObject.CompareTag("VRG_WidgetCube")) return;

                    if(_rightHandHoveredXRObject.GetComponent<KeyFrameDisplay>() == null)
                        _playerTransformation.DeselectObject();
                    _playerTransformation.PerformGrab(Handedness.Right, _rightHandHoveredXRObject.GetComponent<XRGrabInteractable>());
                    if(_rightHandHoveredXRObject.GetComponent<PersistentID>()!=null)
                        PlayModeManager.Instance.RegisterObjectChange(_rightHandHoveredXRObject,true);
                    _grabRight = true;
                    if (_grabLeft && _rightHandHoveredXRObject == _leftHandHoveredXRObject)
                    {
                        twoHandGrab = true;
                    }

                }
            }else if (_grabRight && !i_grabRight.IsPressed())
            {
                if(_rightHandHoveredXRObject.GetComponent<PersistentID>()!=null)
                    PlayModeManager.Instance.RegisterObjectChange(_rightHandHoveredXRObject);
                GameObject grabbedObject = _playerTransformation.rightgrabbedObject;
                _playerTransformation.EndGrab(Handedness.Right);
                if (grabbedObject.CompareTag("VRG_Mark") && !_grabLeft)
                {
                    _playerCommunication.EncapsulateDrawing(grabbedObject.transform.parent.GetComponent<BoxCollider>(),grabbedObject.GetComponent<IdHolderInformation>().GetIDHolder().GetComponentsInChildren<Collider>().Where(c => c.transform != grabbedObject.transform.parent.transform).ToArray());
                }
                _grabRight = false;
                twoHandGrab = false;
            }
        }
        private void HandleInventory()
        {
            if ((i_axLeft.IsPressed() || i_axRight.IsPressed()) && !_inventoryChanged)
            {
                _inventoryChanged = true;
                if (_inventoryInstance != null)
                {
                    CloseInventory();
                }
                else
                {
                    DisplayInventory();
                }
            }else if (!i_axLeft.IsPressed() && !i_axRight.IsPressed())
            {
                _inventoryChanged = false;
            }
        }

        private void HandleMovementOptionsMenu()
        {
            if ((i_byLeft.IsPressed() || i_byRight.IsPressed()) && !_movementOptionsChanged)
            {
                _movementOptionsChanged = true;
                if (_movementOptionsMenuInstance != null)
                {
                    CloseMovementOptions();
                }
                else
                {
                    DisplayMovementOptions();
                }
            }else if (!i_byLeft.IsPressed() && !i_byRight.IsPressed())
            {
                _movementOptionsChanged = false;
            }
        }

        private void HandleColorMenu()
        {
            if ((i_byLeft.IsPressed() || i_byRight.IsPressed()) && !_colorPickerChanged)
            {
                _colorPickerChanged = true;
                if (_colorPickerInstance != null)
                {
                    CloseColorPickerMenu();
                }
                else
                {
                    CloseAllMenus();
                    DisplayColorPickerMenu();
                }
            }else if (!i_byRight.IsPressed() && !i_byLeft.IsPressed())
            {
                _colorPickerChanged = false;
            }
        }

        private void HandleDrawConfirmMenu()
        {
            if ((i_axLeft.IsPressed() || i_axRight.IsPressed()) && !_drawingConfirmMenuChanged)
            {
                _drawingConfirmMenuChanged = true;
                if (_confirmMenuInstance != null)
                {
                    CloseConfirmMenu();
                }
                else
                {
                    CloseAllMenus();
                    DisplayConfirmMenu(_playerCommunication.ConfirmDrawing,_playerCommunication.RevokeDrawing,"Save Drawing?");
                }
            }else if (!i_axRight.IsPressed() && !i_axLeft.IsPressed())
            {
                _drawingConfirmMenuChanged = false;
            }
        }

        private void HandleComSelect()
        { 
            if (i_triggerLeft.IsPressed()  && !_mainMenuChangedMode)
            {
                _leftHandSingleInput = true; 
                if (_graceTimeLeft <= 0 && _leftHandHoveredUI == null)
                {
                    _playerCommunication.DrawLine(Handedness.Left);
                    _triggerLeft = true;
                }
            }
            else if (i_triggerRight.IsPressed() && !_mainMenuChangedMode)
            {
                _rightHandSingleInput = true;
                if (_graceTimeRight <= 0 && _rightHandHoveredUI == null)
                {
                    _playerCommunication.DrawLine(Handedness.Right);
                    _triggerRight = true;
                }
            }
            else
            {
                if (_triggerLeft)
                {
                    _triggerLeft = false;
                    _playerCommunication.ResetDrawing();
                }

                if (_triggerRight)
                {
                    _triggerRight = false;
                    _playerCommunication.ResetDrawing();
                }

                _mainMenuChangedMode = false; 
            }
        }

        private void HandleComGrab()
        {
            if (i_grabLeft.IsPressed())
            {
                _leftHandSingleInput = true; 

                if (_graceTimeLeft <= 0)
                {
                    _playerCommunication.EraseLine(Handedness.Left);
                    _grabLeft = true;
                }
            }
            else if (i_grabRight.IsPressed())
            {
                _rightHandSingleInput = true;

                if (_graceTimeRight <= 0)
                {
                    _playerCommunication.EraseLine(Handedness.Right);
                    _grabRight = true;
                }
            }
            else
            {
                if (_grabLeft)
                {
                    _grabLeft = false;
                }
                if (_grabRight)
                {
                    _grabRight = false;
                }

            }
        }

        #region MovementInput
        
        
        private bool HandleGrabMove()
        {
            if (i_grabMoveLeft.IsPressed())
            {
                ResetGraceTime(Handedness.Left);
                ResetSelectionMenuTime(Handedness.Left);
                return true;
            }

            if (i_grabMoveRight.IsPressed())
            {
                ResetGraceTime(Handedness.Right);
                ResetSelectionMenuTime(Handedness.Right);
                return true;
            }
            return false;
        }
        private bool HandleZoomRotation()
        {
            
            if (i_triggerLeft.IsPressed() && i_triggerRight.IsPressed() && !_grabLeft && !_grabRight && _leftHandHoveredXRObject != null && _rightHandHoveredXRObject != null)
            {
                _playerNavigation.PerformZoomRotation(leftControllerRaycaster.hit, rightControllerRaycaster.hit,true);
                ResetGraceTime(Handedness.None);
                return true;
            }

            if (i_grabLeft.IsPressed() && i_grabRight.IsPressed() && !_grabLeft && !_grabRight)
            {
                _playerNavigation.PerformZoomRotation(leftControllerRaycaster.hit, rightControllerRaycaster.hit,false);
                ResetGraceTime(Handedness.None);
                return true;
            }
            if(_playerNavigation.movementCounter == 1)
            {
                _playerNavigation.StopMovement();
            }
            return false;
        }
        private void HandleTeleport()
        {
            if (i_grabMoveLeft.IsPressed() && i_axLeft.IsPressed() && !_grabLeft)
            {
                _playerNavigation.PerformTeleportRaycast(Handedness.Left);
                _performedTeleport = true;
                _teleportLeft = true;
            }
            else if (i_grabMoveRight.IsPressed() && i_axRight.IsPressed() && !_teleportLeft && !_grabRight)
            {
                _playerNavigation.PerformTeleportRaycast(Handedness.Right);
                _performedTeleport = true;
            }
            else if (_performedTeleport)
            {
                _performedTeleport = false;
                _teleportLeft = false;
                _playerNavigation.PerformTeleport();
            }
        }

        private void HandleCameraFigureExit()
        {
            if ((i_byLeft.IsPressed() || i_byRight.IsPressed()) && !_cameraExitMenuChanged)
            {
                _cameraExitMenuChanged = true;
                if (_confirmMenuInstance != null)
                {
                    CloseConfirmMenu();
                }
                else
                {
                    DisplayConfirmMenu(_playerTransformation.ExitCameraFigure,CloseConfirmMenu,"Exit Camera?");
                }
            }else if (!i_byRight.IsPressed() && !i_byLeft.IsPressed())
            {
                _cameraExitMenuChanged = false;
            }
        }

        private void HandleCameraFigureKeyFrameMenu()
        {
            if ((i_axLeft.IsPressed() || i_axRight.IsPressed()) && !_cameraKeyFrameMenuChanged)
            {
                _cameraKeyFrameMenuChanged = true;
                if (_confirmMenuInstance != null)
                {
                    CloseConfirmMenu();
                }
                else
                {
                    DisplayConfirmMenu(delegate
                    {
                        _playerTransformation.PlaceCameraKeyframe();
                        PlayModeManager.Instance.RegisterObjectChange(_playerTransformation.currentCameraFigure,cameraFigure: _playerTransformation.currentCameraFigure.GetComponent<CameraFigure>());
                    },CloseConfirmMenu,"Place Keyframe?");
                }
            }else if (!i_axLeft.IsPressed() && !i_axRight.IsPressed())
            {
                _cameraKeyFrameMenuChanged = false;
            }
        }
        private void ResetGraceTime(Handedness handedness)
        {
            const float resetGraceTime = 0.25f;
            switch (handedness)
            {
                case Handedness.Left:
                    _graceTimeLeft = resetGraceTime;
                    _leftHandSingleInput = false;
                    break;
                case Handedness.Right:
                    _graceTimeRight = resetGraceTime;
                    _rightHandSingleInput = false;
                    break;
                default:
                    _graceTimeLeft = resetGraceTime;
                    _graceTimeRight = resetGraceTime;
                    _leftHandSingleInput = false;
                    _rightHandSingleInput = false;
                    break;
            }
        }
        private void ResetSelectionMenuTime(Handedness handedness)
        {
            const float resetSelectionMenuTime = 1.5f;
            switch (handedness)
            {
                case Handedness.Left:
                    _leftHandSelectMenuTime = resetSelectionMenuTime;
                    _leftHandSelectInput = false;
                    break;
                case Handedness.Right:
                    _rightHandSelectMenuTime = resetSelectionMenuTime;
                    _rightHandSelectInput = false;
                    break;
                default:
                    _leftHandSelectMenuTime = resetSelectionMenuTime;
                    _rightHandSelectMenuTime = resetSelectionMenuTime;
                    _leftHandSelectInput = false;
                    _rightHandSelectInput = false;
                    break;
            }
        }

        private void ResetUndoRedoTime(Handedness handedness)
        {
            const float resetUndoRedoTime = 0.25f;
            switch (handedness)
            {
                case Handedness.Left:
                    _undoTimeLeft = resetUndoRedoTime;
                    _undoInput = false;
                    break;
                case Handedness.Right:
                    _redoTimeRight = resetUndoRedoTime;
                    _redoInput = false;
                    break;
                default:
                    _undoTimeLeft = resetUndoRedoTime;
                    _redoTimeRight = resetUndoRedoTime;
                    _undoInput = false;
                    _redoInput = false;
                    break;
            }
        }
        #endregion
        
        #endregion

        #region ObjectPreparation
        
        /**
         * Add components to hovered object for interaction
         */
        public void AssignHoverObject(GameObject hoveredObject, Handedness handedness)
        {
            if (handedness == Handedness.Left)
            {
                if (_leftHandHoverObject != hoveredObject)
                {
                    _leftHandHoverObject = hoveredObject;
                    if (_leftHandHoverObject != null)
                    {
                        if (!_leftHandHoverObject.CompareTag("VRG_Mark"))
                        {
                            if (_leftHandHoverObject.GetComponent<XRGrabInteractable>() == null)
                            {
                                XRGrabInteractable interactable =
                                    _leftHandHoverObject.AddComponent<XRGrabInteractable>();
                                _leftHandHoverObject.AddComponent<XRGeneralGrabTransformer>();
                                _leftHandHoverObject.AddComponent<TwoHandGrabTransformer>();
                                SetupInteractable(interactable);
                            }
                        }
                        else
                        {
                            if (_leftHandHoverObject.GetComponent<XRGrabInteractable>() == null &&
                                _leftHandHoverObject.GetComponent<PersistentID>() == null)
                            {
                                XRGrabInteractable interactable = _leftHandHoverObject.AddComponent<XRGrabInteractable>();
                                _leftHandHoverObject.AddComponent<XRGeneralGrabTransformer>();
                                IdHolderInformation idHolderInformation = _leftHandHoverObject.AddComponent<IdHolderInformation>();
                                idHolderInformation.iDHolder = _leftHandHoverObject.transform.parent;
                                SetupInteractable(interactable);
                                _leftHandHoverObject.AddComponent<DrawingGrabTransformer>();
                            }
                        }
                    }
                }
            }
            else
            {
                if (_rightHandHoverObject != hoveredObject)
                {
                    _rightHandHoverObject = hoveredObject;
                    if (_rightHandHoverObject != null)
                    {
                        if (!_rightHandHoverObject.CompareTag("VRG_Mark"))
                        {
                            if (_rightHandHoverObject.GetComponent<XRGrabInteractable>() == null)
                            {
                                XRGrabInteractable interactable =
                                    _rightHandHoverObject.AddComponent<XRGrabInteractable>();
                                _rightHandHoverObject.AddComponent<XRGeneralGrabTransformer>();
                                _rightHandHoverObject.AddComponent<TwoHandGrabTransformer>();
                                SetupInteractable(interactable);
                            }
                        }
                        else
                        {
                            if (_rightHandHoverObject.GetComponent<XRGrabInteractable>() == null &&
                                _rightHandHoverObject.GetComponent<PersistentID>() == null)
                            {
                                XRGrabInteractable interactable = _rightHandHoverObject.AddComponent<XRGrabInteractable>();
                                _rightHandHoverObject.AddComponent<XRGeneralGrabTransformer>();
                                IdHolderInformation idHolderInformation = _rightHandHoverObject.AddComponent<IdHolderInformation>();
                                idHolderInformation.iDHolder = _rightHandHoverObject.transform.parent;
                                SetupInteractable(interactable);
                                _rightHandHoverObject.AddComponent<DrawingGrabTransformer>();
                            }
                        }
                    }
                }

            }
        }
        
        /**
         * Configure VR interaction components
         */
        public void SetupInteractable(XRGrabInteractable interactable)
        {
            interactable.useDynamicAttach = true;
            interactable.matchAttachPosition = true;
            interactable.matchAttachRotation = true;
            interactable.throwOnDetach = false;
            interactable.trackRotation = true;
            interactable.selectMode = InteractableSelectMode.Multiple;
            interactable.focusMode = InteractableFocusMode.Multiple;
            interactable.snapToColliderVolume = true;
            interactable.reinitializeDynamicAttachEverySingleGrab = true;
            Rigidbody rigidcomponent = interactable.gameObject.GetComponent<Rigidbody>();
            rigidcomponent.isKinematic = true;
        }
        
        #endregion

        #region UI


        public Vector3 GetUIPosition()
        {
            return xROrigin.GetComponentInChildren<Camera>().transform.position+xROrigin.GetComponentInChildren<Camera>().transform.forward * (2);
        }
        
        private void CloseAllMenus()
        {
            CloseInventory();
            CloseSelectionMenu();
            CloseConfirmMenu();
            CloseMainMenu();
            CloseColorPickerMenu();
            CloseSceneSelectionMenu();
            CloseKeyframeEditMenu();
            CloseMovementOptions();
        }
        
        #region Inventory

        public void DisplayInventory()
        {
            _inventoryInstance = Instantiate(inventoryMenuPrefab,GetUIPosition(), Quaternion.identity);
            _inventoryInstance.transform.forward = xROrigin.GetComponentInChildren<Camera>().gameObject.transform.forward;
            _inventoryInstance.GetComponent<InventoryMenu>().FillInventoryMenu();
        }
   
        public void CloseInventory()
        {
            if(_inventoryInstance == null) return;
            Destroy(_inventoryInstance);
            _inventoryInstance = null;
        }
        
        public void SpawnObjectOnHand(GameObject prefab,Transform buttonTransform,Handedness handedness)
        {
            _lastPrefabIndex = PlayModeManager.Instance.editorDataSO.availablePrefabs.IndexOf(prefab);
            GameObject instance = _playerTransformation.CreatePrefabFromMenu(prefab,_lastPrefabIndex,buttonTransform);
            _playerTransformation.PerformGrab(handedness,instance.GetComponent<XRGrabInteractable>());
            if (handedness == Handedness.Left)
            {
                _leftHandSelectedXRObject = instance;
                _objectSpawnedLeft = true;
            }
            else
            {
                _objectSpawnedRight = true;
                _rightHandSelectedXRObject = instance;
            }
            CloseInventory();
        }
        
        #endregion

        #region MovementOptions

        public void DisplayMovementOptions()
        {
            _movementOptionsMenuInstance = Instantiate(movementOptionsMenuPrefab,GetUIPosition(), Quaternion.identity);
            _movementOptionsMenuInstance.transform.forward = xROrigin.GetComponentInChildren<Camera>().gameObject.transform.forward;

            foreach (var dropdown in _movementOptionsMenuInstance.GetComponentsInChildren<TMP_Dropdown>())
            {
                if (dropdown.options.Count == 3)
                {
                    dropdown.value = (int)PlayModeManager.Instance.editorDataSO.rotationMode;
                    dropdown.onValueChanged.AddListener(delegate
                    {
                        PlayModeManager.Instance.editorDataSO.rotationMode = (RotationMode)dropdown.value;
                    });
                }
                else
                {
                    dropdown.value = (int)PlayModeManager.Instance.editorDataSO.zoomMode;
                    dropdown.onValueChanged.AddListener(delegate
                    {
                        PlayModeManager.Instance.editorDataSO.zoomMode = (ZoomMode)dropdown.value;
                    });
                }
            }


        }

        public void CloseMovementOptions()
        {
            if(_movementOptionsMenuInstance == null) return;
            Destroy(_movementOptionsMenuInstance);
            _movementOptionsMenuInstance = null;
        }

        #endregion

        #region SelectionMenu
        private void DisplaySelectionMenu()
        {
            _selectionMenuInstance = Instantiate(selectionMenuPrefab,GetUIPosition(), Quaternion.identity);
            _selectionMenuInstance.transform.forward = xROrigin.GetComponentInChildren<Camera>().gameObject.transform.forward;
            _selectionMenuInstance.GetComponentInChildren<Button>().onClick.AddListener(CloseSelectionMenu);
            GameObject content = _selectionMenuInstance.GetComponentInChildren<GridLayoutGroup>().gameObject;
            
            //Deletion
            GameObject deletionButton = Instantiate(menuOptionButtonPrefab, content.transform);
            deletionButton.name = "Delete";
            deletionButton.GetComponentInChildren<Text>().text = "Delete";
            deletionButton.GetComponentInChildren<Image>().sprite = selectionMenuDeletionIcon;
            deletionButton.GetComponent<Button>().onClick.AddListener(_playerTransformation.DeleteSelectedObject);
            
            //Duplication
            GameObject duplicationButton = Instantiate(menuOptionButtonPrefab, content.transform);
            duplicationButton.name = "Duplicate";
            duplicationButton.GetComponentInChildren<Text>().text = "Duplicate";
            duplicationButton.GetComponentInChildren<Image>().sprite = seletionMenuDuplicateIcon;
            duplicationButton.AddComponent<ObjectDuplicationButton>();
            
            //CameraFigure
            if (_playerTransformation.selectedObject.CompareTag("VRG_CameraFigure"))
            {
                GameObject cameraButton = Instantiate(menuOptionButtonPrefab, content.transform);
                cameraButton.name = "CameraFigure";
                cameraButton.GetComponentInChildren<Text>().text = "Enter";
                cameraButton.GetComponentInChildren<Image>().sprite = selectionMenuCameraIcon;
                cameraButton.GetComponent<Button>().onClick.AddListener(_playerTransformation.EnterCameraFigure);

                if (GetSelectedObject().GetComponent<CameraFigure>().keyFrames.Count > 0)
                {
                    GameObject cameraPathButton = Instantiate(menuOptionButtonPrefab, content.transform);
                    cameraPathButton.name = "CameraPath";
                    cameraPathButton.GetComponentInChildren<Text>().text = "Camera Path";
                    cameraPathButton.GetComponentInChildren<Image>().sprite = selectionMenuCameraIcon;
                    cameraPathButton.GetComponent<Button>().onClick.AddListener(delegate
                    {
                        FollowCameraPath(GetSelectedObject().GetComponent<CameraFigure>());
                    });
                }
            }
            else
            {
                //Saving
                GameObject saveButton = Instantiate(menuOptionButtonPrefab, content.transform);
                saveButton.name = "Save";
                saveButton.GetComponentInChildren<Text>().text = "Save";
                saveButton.GetComponentInChildren<Image>().sprite = selectionMenuSaveIcon;
                saveButton.GetComponent<Button>().onClick.AddListener(_playerTransformation.SaveSelectedObject);
            }
        }

        public void CloseSelectionMenu()
        {
            if(_selectionMenuInstance == null) return;
            Destroy(_selectionMenuInstance);
            _selectionMenuInstance = null;
        }

        public void DisplaySelectionLoadingProgress(float currentTime,float maxTime)
        {
            if (_selectionMenuLoadingProgressInstance == null)
            {
                _selectionMenuLoadingProgressInstance = Instantiate(selectionMenuLoadingProgressPrefab,GetUIPosition(), Quaternion.identity);
                _selectionMenuLoadingProgressInstance.transform.forward = xROrigin.GetComponentInChildren<Camera>().gameObject.transform.forward;
            }
            _selectionMenuLoadingProgressInstance.GetComponent<SelectionMenuLoadingInfo>().DisplayLoadingProgress(currentTime,maxTime);
        }

        public void CloseSelectionLoadingProgress()
        {
            if(_selectionMenuLoadingProgressInstance == null) return;
            Destroy(_selectionMenuLoadingProgressInstance);
            _selectionMenuLoadingProgressInstance = null;
        }

        public void DuplicateSelectedObject(Transform trans,Handedness handedness)
        {
            GameObject go = _playerTransformation.DuplicateSelectedObject();
            go.transform.position = trans.position;
            if (go.GetComponent<IdHolderInformation>() != null)
            {
                _lastPrefabIndex = go.GetComponent<IdHolderInformation>().prefabIndex;
                if (_lastPrefabIndex == -1)
                    _lastDuplicationID = go.GetComponent<IdHolderInformation>().GetIDHolder().GetComponent<PersistentID>().uniqueId;
            }
            else
            {
                _lastPrefabIndex = -1;
                _lastDuplicationID = "";
            }

            _playerTransformation.PerformGrab(handedness,go.GetComponent<XRGrabInteractable>());
            if (handedness == Handedness.Left)
            {
                _leftHandSelectedXRObject = go;
                _objectSpawnedLeft = true;
            }
            else
            {
                _objectSpawnedRight = true;
                _rightHandSelectedXRObject = go;
            }
            CloseSelectionMenu();
        }
        #endregion

        #region Drawing

        private void DisplayColorPickerMenu()
        {
            _colorPickerInstance = Instantiate(colorPickerPrefab, GetUIPosition(), Quaternion.identity);
            _colorPickerInstance.transform.forward = xROrigin.GetComponentInChildren<Camera>().gameObject.transform.forward;
        }

        private void CloseColorPickerMenu()
        {
            if(_colorPickerInstance == null) return;
            Destroy(_colorPickerInstance);
            _colorPickerInstance = null;
        }

        #endregion

        #region InputMenu
        
        public async Task<string> DisplayInputMenu(string descriptiontext)
        {
            GameObject input = Instantiate(inputFieldPrefab,GetUIPosition(), Quaternion.identity);
            GameObject keyboard = Instantiate(keyboardPrefab,GetUIPosition()-Vector3.down, Quaternion.identity);
            input.transform.forward  = xROrigin.GetComponentInChildren<Camera>().gameObject.transform.forward;
            keyboard.transform.forward  = -xROrigin.GetComponentInChildren<Camera>().gameObject.transform.forward;
            TMP_InputField inputText = input.GetComponentInChildren<TMP_InputField>();
            _keyCaps = new List<KeyboardKey>();
            foreach (var keyboardKey in keyboard.GetComponentsInChildren<KeyboardKey>())
            {
                _keyCaps.Add(keyboardKey);
                if (keyboardKey.isEnter) keyboardKey.OnSubmitted += SubmitText;
                if (keyboardKey.isShift)
                {
                    keyboardKey.OnToggleShift += ToggleShift;
                    _shiftKeyImage = keyboardKey.GetComponentInChildren<Image>();
                }
                
                keyboardKey.GetComponent<Button>().onClick.AddListener(delegate
                {
                    keyboardKey.PressKey(inputText, _shift);
                });

            }

            _shift = true;
            ToggleShift(this,EventArgs.Empty);
            _textSubmitted = false;
            input.GetComponentInChildren<TextMeshProUGUI>().text = descriptiontext;
            while (!_textSubmitted)
            {
                await Task.Delay(500);
            }
            string result = inputText.text.Length > 0 ? inputText.text : "Saved Prefab";
            Destroy(input);
            Destroy(keyboard);
            return result;
        }

        private void SubmitText(object sender, EventArgs e)
        {
            Debug.Log("SubmitText");
            _textSubmitted = true;
        }

        private void ToggleShift(object sender, EventArgs e)
        {
            _shift = !_shift;
            if (_shift)
            {
                _shiftKeyImage.color = Color.blue;
                foreach (var keyCap in _keyCaps)
                {
                    if(keyCap.shiftKeyCharacter == keyCap.keyCharacter || keyCap.GetComponentInChildren<TextMeshProUGUI>() == null) continue;
                    keyCap.GetComponentInChildren<TextMeshProUGUI>().text = keyCap.shiftKeyCharacter;
                }
            }
            else
            {
                _shiftKeyImage.color = Color.white;
                foreach (var keyCap in _keyCaps)
                {
                    if(keyCap.shiftKeyCharacter == keyCap.keyCharacter || keyCap.GetComponentInChildren<TextMeshProUGUI>() == null) continue;
                    keyCap.GetComponentInChildren<TextMeshProUGUI>().text = keyCap.keyCharacter;
                }
            }
        }

        #endregion
        
        #region ConfirmMenu

        private void DisplayConfirmMenu(UnityAction confirmAction, UnityAction cancelAction, string confirmText)
        {
            _confirmMenuInstance = Instantiate(confirmMenuPrefab,GetUIPosition(), Quaternion.identity);
            _confirmMenuInstance.transform.forward = xROrigin.GetComponentInChildren<Camera>().gameObject.transform.forward;
            GameObject content = _confirmMenuInstance.GetComponentInChildren<GridLayoutGroup>().gameObject;
            _confirmMenuInstance.GetComponentInChildren<Text>().text = confirmText;

            //Confirm
            GameObject confirmButton = Instantiate(menuOptionButtonPrefab, content.transform);
            confirmButton.name = "Confirm";
            confirmButton.GetComponentInChildren<Text>().text = "Yes";
            confirmButton.GetComponentInChildren<Image>().sprite = confirmConfirmIcon;
            confirmButton.GetComponent<Button>().onClick.AddListener(confirmAction);
            
            //Cancel
            GameObject cancelButton = Instantiate(menuOptionButtonPrefab, content.transform);
            cancelButton.name = "Cancel";
            cancelButton.GetComponentInChildren<Text>().text = "No";
            cancelButton.GetComponentInChildren<Image>().sprite = confirmCancelIcon;
            cancelButton.GetComponent<Button>().onClick.AddListener(cancelAction);
        }

        public void CloseConfirmMenu()
        {
            if(_confirmMenuInstance == null) return;
            Destroy(_confirmMenuInstance);
            _confirmMenuInstance = null;
        }
        #endregion
        
        #region MainMenu

        private void DisplayMainMenu(GameObject controller)
        {
            _mainMenuInstance = Instantiate(mainMenuPrefab,controller.transform.position,controller.transform.rotation);
            _sceneMenuIndex = mainMenuOptionNames.IndexOf("Scenes");
            _closeIndex = mainMenuOptionNames.IndexOf("Close");
            
            _mainMenuInstance.GetComponent<RadialSelection>().SpawnRadialParts(mainMenuOptionSprites,mainMenuOptionNames,controller);
        }

        private void CloseMainMenu()
        {
            if(_mainMenuInstance == null) return;
            Destroy(_mainMenuInstance);
            _mainMenuInstance = null;
        }

        private void HandleMainMenuSelection(int selection)
        {
            CloseMainMenu();

            if (selection == _sceneMenuIndex)
            {
                DisplaySceneSelectionMenu();
            }
            else if (selection == _closeIndex)
            {
                DisplayConfirmMenu(StopPlaymode,CloseConfirmMenu,"End VRGreyboxing?");
            }
            else
            {
                switch (_currentEditMode)
                {
                    case EditMode.Transformation:
                        _playerTransformation.DeselectObject();
                        break;
                    case EditMode.Edit:
                        _playerEdit.DeselectObject();
                        _playerEdit.ClearFloatingPoints();
                        break;
                }
                
                _currentEditMode = (EditMode)selection;
            }
            _mainMenuChangedMode = true;
        }

        private void StopPlaymode()
        {
            _playerTransformation.RemoveKeyFrameDisplays();
            PlayModeManager.Instance.ResetWorldScale();
            EditorApplication.isPlaying = false;
        }
        
        #endregion

        #region SceneSelection

        private void DisplaySceneSelectionMenu(Vector3 offset = default)
        {
            _sceneMenuInstance = Instantiate(sceneMenuPrefab,GetUIPosition(), Quaternion.identity);
            _sceneMenuInstance.transform.position += offset;
            _sceneMenuInstance.transform.forward = xROrigin.GetComponentInChildren<Camera>().gameObject.transform.forward;
            _sceneMenuInstance.GetComponentInChildren<Button>().onClick.AddListener(CloseSceneSelectionMenu);
            GameObject content = _sceneMenuInstance.GetComponentInChildren<GridLayoutGroup>().gameObject;
            foreach (var sceneName in PlayModeManager.Instance.editorDataSO.sceneNames)
            {
                if(sceneName == SceneManager.GetActiveScene().name || sceneName.Equals("EditorBaseScene")) continue;
                
                GameObject sceneButton = Instantiate(sceneSelectButtonPrefab, content.transform);
                sceneButton.name = sceneName;
                sceneButton.GetComponentInChildren<Text>().text = sceneName;
                sceneButton.GetComponent<Button>().onClick.AddListener(delegate{SwitchToScene(sceneName);});
            }
        }

        private void CloseSceneSelectionMenu()
        {
            if(_sceneMenuInstance == null) return;
            Destroy(_sceneMenuInstance);
            _sceneMenuInstance = null;
        }

        private void SwitchToScene(string sceneName)
        {
            PlayModeManager.Instance.ResetWorldScale();
            SceneManager.LoadScene(sceneName);
        }

        #endregion

        #region Zoom

        public void DisplayZoomMenu()
        {
            if(_scaleMenuInstance != null)
            {
                CloseZoomMenu();
                return;
            }
            _scaleMenuInstance = Instantiate(scaleMenuPrefab,GetUIPosition(), Quaternion.identity);
            _scaleMenuInstance.transform.forward = xROrigin.GetComponentInChildren<Camera>().gameObject.transform.forward;
        }

        private void CloseZoomMenu()
        {
            if(_scaleMenuInstance == null) return;
            Destroy(_scaleMenuInstance);
            _scaleMenuInstance = null;
        }

        public void ScalePlayer(float scaleValue)
        {
            _playerNavigation.PerformMenuZoom(scaleValue);
            CloseZoomMenu();
        }

        #endregion

        #region KeyframeEditMenu

        public void DisplayKeyframeEditMenu(Vector3 keyPosition, int keyFrameIndex)
        {
            CloseAllMenus();
            _keyFrameMenuInstance = Instantiate(keyFrameMenuPrefab,keyPosition+Vector3.up*0.5f, Quaternion.identity);
            _keyFrameMenuInstance.transform.forward = xROrigin.GetComponentInChildren<Camera>().gameObject.transform.forward;
            _keyFrameMenuInstance.GetComponentInChildren<Button>().onClick.AddListener(CloseKeyframeEditMenu);
            GameObject content = _keyFrameMenuInstance.GetComponentInChildren<ContentSizeFitter>().gameObject;
            for (int i = 0; i < content.GetComponentsInChildren<Slider>().Length; i++)
            {
                Slider slider = content.transform.GetChild(i).GetComponent<Slider>();
                slider.onValueChanged.AddListener( delegate {slider.GetComponentInChildren<TextMeshProUGUI>().text = slider.value.ToString(); });
                slider.minValue = 0;
                slider.maxValue = 10;

                if (i == 0)
                {
                    slider.value = _playerTransformation.currentCameraFigure.GetComponent<CameraFigure>().keyFrames[keyFrameIndex].cameraMoveTime;
                    slider.onValueChanged.AddListener( delegate {_playerTransformation.currentCameraFigure.GetComponent<CameraFigure>().keyFrames[keyFrameIndex].cameraMoveTime = slider.value; });

                }
                else
                {
                    slider.value = _playerTransformation.currentCameraFigure.GetComponent<CameraFigure>().keyFrames[keyFrameIndex].cameraRotateTime;
                    slider.onValueChanged.AddListener( delegate {_playerTransformation.currentCameraFigure.GetComponent<CameraFigure>().keyFrames[keyFrameIndex].cameraRotateTime = slider.value; });

                }
            }
            content.GetComponentInChildren<Button>().onClick.AddListener(delegate
            {
                DisplayConfirmMenu(delegate
                {
                    _playerTransformation.DeleteCameraKeyframe(keyFrameIndex);
                    PlayModeManager.Instance.RegisterObjectChange(_playerTransformation.currentCameraFigure,cameraFigure: _playerTransformation.currentCameraFigure.GetComponent<CameraFigure>());
                    CloseKeyframeEditMenu();
                    CloseConfirmMenu();
                }, CloseConfirmMenu,"Delete Keyframe");
            });
        }

        public void CloseKeyframeEditMenu()
        {
            if(_keyFrameMenuInstance == null) return;
            Destroy(_keyFrameMenuInstance);
            _keyFrameMenuInstance = null;
        }

        #region CameraOverlayDisplay

        public void DisplayCameraOverlay(string text, float duration)
        {
            xROrigin.GetComponentInChildren<TextMeshPro>().text = text;
            xROrigin.GetComponentInChildren<TextMeshPro>(true).enabled = true;
            _cameraOverlayTime = duration;
        }

        public void HideCameraOverlay()
        {
            xROrigin.GetComponentInChildren<TextMeshPro>(true).enabled = false;
        }
        
        #endregion

        #endregion
        
        #endregion

        #region Editing

        public void SetCurrentPolyShape(GameObject polyShape)
        {
            _currentPolyShape = polyShape;
        }
        
        #endregion

        #region Drawing

        public void ChangeDrawColor(Color color)
        {
            _playerCommunication.SetColor(color);
            CloseColorPickerMenu();
        }

        public void ChangeLineWidth(float width)
        {
            _playerCommunication.lineWidth = width;
        }

        public float GetCurrentLineWidth()
        {
            return _playerCommunication.lineWidth;
        }

        #endregion

        #region Movement

        private void FollowCameraPath(CameraFigure cameraFigure)
        {
            _playerTransformation.EnterCameraFigure();
            _playerNavigation.FollowCameraPath(cameraFigure);
        }

        public void ExitCameraPath()
        {
            _playerTransformation.ExitCameraFigure();
            _playerTransformation.currentCameraFigure = null;

        }

        #endregion
        
        #region ObjectAssignment
        public void XRHoverEventLeft(HoverEnterEventArgs args)
        {
            _leftHandHoveredXRObject = args.interactableObject.transform.gameObject;
        }
        public void XRHoverEventRight(HoverEnterEventArgs args)
        {
            _rightHandHoveredXRObject = args.interactableObject.transform.gameObject;
        }
        public void XRSelectEventLeft(SelectEnterEventArgs args)
        {
            _leftHandSelectedXRObject = args.interactableObject.transform.gameObject;
        }
        public void XRSelectEventRight(SelectEnterEventArgs args)
        {
            _rightHandSelectedXRObject = args.interactableObject.transform.gameObject;
        }
        public void XRExitEventLeft(SelectExitEventArgs args)
        {
            _leftHandSelectedXRObject = null;
        }
        public void XRExitEventRight(SelectExitEventArgs args)
        {
            _rightHandSelectedXRObject = null;
        }

        public void XRHoverEnterUIEventLeft(UIHoverEventArgs args)
        {
            _leftHandHoveredUI = args.uiObject.transform.gameObject;
        }

        public void XRHoverExitUIEventLeft(UIHoverEventArgs args)
        {
            _leftHandHoveredUI = null;
        }
        
        public void XRHoverEnterUIEventRight(UIHoverEventArgs args)
        {
            _rightHandHoveredUI = args.uiObject.transform.gameObject;
        }

        public void XRHoverExitUIEventRight(UIHoverEventArgs args)
        {
            _rightHandHoveredUI = null;
        }

        public GameObject GetSelectedObject()
        {
            switch (_currentEditMode)
            {
                case EditMode.Transformation:
                    return _playerTransformation.selectedObject;
                case EditMode.Edit:
                    return _playerEdit.selectedObject;
            }
            return null;
        }

        private void SelectObject(Handedness handedness,GameObject selectedObject)
        {
            CloseSelectionMenu();
            switch (_currentEditMode)
            {
                case EditMode.Transformation:
                    _playerTransformation.SelectObject(handedness,selectedObject);
                    break;
                case EditMode.Edit:
                    _playerEdit.SelectObject(handedness,selectedObject);
                    break;
            }
        }

        public void RefreshCurrentWidget()
        {
            if (GetSelectedObject() != null)
            {
                switch (_currentEditMode)
                {
                    case EditMode.Transformation:
                        _playerTransformation.ApplyTransWidgetToSelectedObject();
                        break;
                    case EditMode.Edit:
                        _playerEdit.ApplyTransWidgetToSelectedObject();
                        break;
                }
            }
        }

        private void DeselectObject()
        {
            switch (_currentEditMode)
            {
                case EditMode.Transformation:
                    _playerTransformation.DeselectObject();
                    break;
                case EditMode.Edit:
                    _playerEdit.DeselectObject();
                    break;
            }
        }
        
        public T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            var type = original.GetType();
            var copy = destination.AddComponent(type);
            var fields = type.GetFields();
            foreach (var field in fields) field.SetValue(copy, field.GetValue(original));
            return copy as T;
        }
        
        
        #endregion
    }
}
#endif    

