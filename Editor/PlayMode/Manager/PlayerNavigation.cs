using System;
using System.Collections;
using System.Numerics;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace VRGreyboxing
{
    public class PlayerNavigation : MonoBehaviour
    {
        public GameObject navigationInputDisplay;
        public GameObject navigationInputDisplayBorder;
        private GameObject _displayInstance;
        private GameObject _displayBorderInstance;
        private GameObject _previewInstance;

        private Handedness _currentHandedness;
        [HideInInspector] public int movementCounter;

        private GameObject _leftController;
        public GrabMoveProvider leftGrabMoveProvider;
        private GameObject _rightController;
        public GrabMoveProvider rightGrabMoveProvider;

        private Vector3 _leftControllerZoomOrigin;
        private Vector3 _rightControllerZoomOrigin;


        private Vector3 _turnAnchor;
        private GameObject _turnAnchorObject;
        private Vector3 _playerViewCenter;
        private Vector3 _originControllerCenter;

        private LineRenderer _leftTeleportLine;
        private LineRenderer _rightTeleportLine;
        private Vector3 _lastTeleportPosition;

        private float _zoomMenuTimer;
        private float _lineSizePlayerRatio;
        
        private Vector3 _anchorToCenter;
        private Transform _originTransform;
        private Vector3 _destinyPos;
        private Vector3 _anchorToDestiny;
        private bool _reachedTpTreshhold;
        
                
        private CameraFigure _currentCameraFigure;
        public CameraKeyFrame currentCameraKeyFrame;
        private float _movementTimer;
        private float _rotationTimer;
        private Vector3 _startPosition;
        
        private Quaternion _savedChildWorldRotation;
        private Quaternion _originalChildWorldRotation;
        private Vector3 _originalChildWorldPosition;
        
        private int _keyFrameIndex;

        private bool _performedRotation;
        private bool _performedZoom;
        
        private void Start()
        {
            movementCounter = 0;
            _leftController = ActionManager.Instance.leftController;
            _rightController = ActionManager.Instance.rightController;
            _leftTeleportLine = _leftController.GetComponent<LineRenderer>();
            _rightTeleportLine = _rightController.GetComponent<LineRenderer>();
            currentCameraKeyFrame = null;
            _originTransform = ActionManager.Instance.xROrigin.transform;
            _lineSizePlayerRatio =
                ActionManager.Instance.xROrigin.GetComponentInChildren<LineRenderer>(true).startWidth /
                ActionManager.Instance.xROrigin.transform.localScale.x;
        }

        private void Update()
        {
            if(_zoomMenuTimer > -1) _zoomMenuTimer -= Time.deltaTime;
            
            if (movementCounter == 2)
            {
                leftGrabMoveProvider.enabled = true;
                rightGrabMoveProvider.enabled = true;
                movementCounter = 0;
                if(_displayInstance != null)
                    Destroy(_displayInstance);
                if(_displayBorderInstance != null)
                    Destroy(_displayBorderInstance);
                if(_previewInstance != null)
                    Destroy(_previewInstance);
            }
            
            if(currentCameraKeyFrame != null)
                MoveCamera();
            
            
        }

        public void StopMovement()
        {
            movementCounter = 2;
            if (PlayModeManager.Instance.editorDataSO.performLateTeleport && _reachedTpTreshhold)
            {
                TeleportRotation();
                _reachedTpTreshhold = false;
            }

            if (_performedRotation)
            {
                switch (PlayModeManager.Instance.editorDataSO.rotationMode)
                {
                    case 0:
                        PlayModeManager.Instance.editorDataSO.restrictedRotation++;
                        break;
                    case 1:
                        PlayModeManager.Instance.editorDataSO.unrestrictedRotation++;
                        break;
                }
                _performedRotation = false;
            }

            if (_zoomMenuTimer >= 0)
            {
                ActionManager.Instance.DisplayZoomMenu();
            }else if (_performedZoom)
            {
                PlayModeManager.Instance.editorDataSO.gestureZoom++;
                _performedZoom = false;
            }
        }

        public void PerformLeaning(Vector2 direction)
        {
            Transform cam = _originTransform.GetComponentInChildren<Camera>().transform;

            if (Mathf.Abs(direction.y) > 0.01f)
            {
                Vector3 right = cam.right;
                _originTransform.Rotate(right, direction.y * PlayModeManager.Instance.editorDataSO.leaningSpeed * Time.deltaTime, Space.World);
            }

            if (Mathf.Abs(direction.x) > 0.01f)
            {
                Vector3 forward = cam.forward;
                _originTransform.Rotate(forward, -direction.x * PlayModeManager.Instance.editorDataSO.leaningSpeed * Time.deltaTime, Space.World);
            }
        }

        public void PerformZoomRotation(RaycastHit hitLeft, RaycastHit hitRight, bool performRotation)
        {
            if (movementCounter == 0)
            {
                leftGrabMoveProvider.enabled = false;
                rightGrabMoveProvider.enabled = false;
                _leftControllerZoomOrigin = _leftController.transform.position;
                _rightControllerZoomOrigin = _rightController.transform.position;
                _playerViewCenter = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.position;
                _playerViewCenter += ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.forward;
                _originControllerCenter =
                    (_leftController.transform.position + _rightController.transform.position) / 2;

                if (hitLeft.collider != null && hitRight.collider != null)
                {
                    _turnAnchor = (hitLeft.point + hitRight.point) / 2;
                    _turnAnchorObject = hitLeft.collider.transform.gameObject;
                }
                else
                {
                    if (hitLeft.collider != null)
                        _turnAnchor = hitLeft.point;
                    else if (hitRight.collider != null)
                        _turnAnchor = hitRight.point;
                }

                movementCounter = 1;
                if(PlayModeManager.Instance.editorDataSO.zoomMode == 1 && !performRotation)
                    _zoomMenuTimer = PlayModeManager.Instance.editorDataSO.zoomMenuTime;
                return;
            }
            
            var cameraPosition = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.position;
            var leftControllerPosition = _leftController.transform.position;
            var rightControllerPosition = _rightController.transform.position;

            if (performRotation)
            {
                if (PlayModeManager.Instance.editorDataSO.rotationMode == 0)
                {
                    leftControllerPosition.y = 0;
                    rightControllerPosition.y = 0;
                    cameraPosition.y = 0;
                    _playerViewCenter.y = 0;
                    var normal = (_playerViewCenter - cameraPosition).normalized;
                    var leftControllerSide =
                        Vector3.Cross(normal, (leftControllerPosition - cameraPosition).normalized).y > 0
                            ? Handedness.Left
                            : Handedness.Right;
                    var rightControllerSide =
                        Vector3.Cross(normal, (rightControllerPosition - cameraPosition).normalized).y > 0
                            ? Handedness.Left
                            : Handedness.Right;


                    if (leftControllerSide == rightControllerSide)
                    {
                        _performedRotation = true;
                        PerformRestrictedRotation(leftControllerSide);
                    }
                }
                else if (PlayModeManager.Instance.editorDataSO.rotationMode == 1)
                {
                    if (_displayInstance == null)
                    {
                        _displayInstance = Instantiate(navigationInputDisplay);
                        _displayBorderInstance = Instantiate(navigationInputDisplayBorder);
                        _previewInstance = Instantiate(_turnAnchorObject);
                        _displayBorderInstance.transform.localScale = new Vector3(PlayModeManager.Instance.editorDataSO.freeRotationCenterDistance*2, PlayModeManager.Instance.editorDataSO.freeRotationCenterDistance*2, PlayModeManager.Instance.editorDataSO.freeRotationCenterDistance*2);
                        _displayInstance.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                        ScaleToTargetSize(_previewInstance,_displayBorderInstance,0.5f);
                    }

                    Vector3 controllerCenter = (leftControllerPosition + rightControllerPosition) / 2;
                    _displayInstance.transform.position = controllerCenter;
                    _displayBorderInstance.transform.position = _previewInstance.transform.position = _originControllerCenter;
                    Vector3 movementInput = controllerCenter - _originControllerCenter;
                    _displayInstance.transform.up = movementInput.normalized;
                    _displayBorderInstance.GetComponent<InputIndicatorVisualHelp>().DisplayIndicators(movementInput,PlayModeManager.Instance.editorDataSO.freeRotationCenterDistance,_displayInstance,0,PlayModeManager.Instance.editorDataSO.rotationMode,PlayModeManager.Instance.editorDataSO.enableTeleportRotationLeaning);
                    if (movementInput.magnitude > PlayModeManager.Instance.editorDataSO.freeRotationCenterDistance)
                    {
                        _performedRotation = true;
                        PerformUnrestrictedRotation(movementInput, controllerCenter);
                    }
                }
                else if (PlayModeManager.Instance.editorDataSO.rotationMode == 2)
                {
                    PerformTeleportRotation();
                }
            }
            else
            {
                if (PlayModeManager.Instance.editorDataSO.zoomMode == 0)
                {
                    PerformGestureZoom();
                }
            }
        }

        private void ScaleToTargetSize(GameObject obj, GameObject target, float relativeSize)
        {
            Renderer objRend = obj.GetComponentInChildren<Renderer>();
            Renderer targetRend = target.GetComponentInChildren<Renderer>();
            if (objRend == null || targetRend == null) return;
            
            Vector3 currentSize = objRend.bounds.size;
            Vector3 targetSize = targetRend.bounds.size;
            
            if (currentSize.x == 0 || currentSize.y == 0 || currentSize.z == 0 || targetSize.x == 0 || targetSize.y == 0 || targetSize.z == 0) return;
            
            Vector3 scaleFactor = new Vector3(
                targetSize.x*relativeSize / currentSize.x,
                targetSize.y*relativeSize / currentSize.y,
                targetSize.z*relativeSize / currentSize.z
            );
            float uniformScaleFactor = Mathf.Min(scaleFactor.x, scaleFactor.y, scaleFactor.z);

            obj.transform.localScale *= uniformScaleFactor;
        }

        private void PerformUnrestrictedRotation(Vector3 movementInput, Vector3 controllerCenter)
        {
            Vector3 centerToAnchor = _originControllerCenter - _turnAnchor;
            
            Vector3 dirNormalized = movementInput.normalized;
            float distance = Vector3.Dot(centerToAnchor, dirNormalized);
            Vector3 lockedPosition = controllerCenter + dirNormalized * distance;
            Vector3 projectedMovement = Vector3.ProjectOnPlane(movementInput, centerToAnchor.normalized);

            Vector3 turnAxis = Vector3.Cross(centerToAnchor, projectedMovement).normalized;

            float angle = projectedMovement.magnitude * PlayModeManager.Instance.editorDataSO.rotationSpeed;
            Quaternion rotation = Quaternion.AngleAxis(angle, turnAxis);

            Vector3 direction = _originTransform.position - _turnAnchor;
            direction = rotation * direction;
            _originTransform.position = _turnAnchor + direction;
            if (Vector3.Distance(lockedPosition, _turnAnchor) > Vector3.Distance(_originControllerCenter, _turnAnchor))
            {
                _originTransform.position += centerToAnchor.normalized * (distance * 0.01f);
            }
            else
            {
                _originTransform.position -= centerToAnchor.normalized * (distance * 0.01f);
            }
            _originTransform.rotation = rotation * _originTransform.rotation;

            _originControllerCenter = rotation * (_originControllerCenter - _turnAnchor) + _turnAnchor;
            if (Vector3.Distance(lockedPosition, _turnAnchor) > Vector3.Distance(_originControllerCenter, _turnAnchor))
            {
                _originControllerCenter += centerToAnchor.normalized * (distance * 0.01f);
            }
            else
            {
                _originControllerCenter -= centerToAnchor.normalized * (distance * 0.01f);
            }
            
        }

        private void PerformTeleportRotation()
        {
            if (_displayInstance == null)
            {
                _displayInstance = Instantiate(navigationInputDisplay);
                _displayBorderInstance = Instantiate(navigationInputDisplayBorder);
                _previewInstance = Instantiate(_turnAnchorObject);
                _displayBorderInstance.transform.localScale = new Vector3(PlayModeManager.Instance.editorDataSO.teleportationRotationDistance*2, PlayModeManager.Instance.editorDataSO.teleportationRotationDistance*2, PlayModeManager.Instance.editorDataSO.teleportationRotationDistance*2);
                _displayInstance.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                ScaleToTargetSize(_previewInstance,_displayBorderInstance,0.5f);
            }


            Vector3 controllerCenter = (_leftController.transform.position + _rightController.transform.position) / 2;
            _displayInstance.transform.position = controllerCenter;
            _displayBorderInstance.transform.position= _previewInstance.transform.position = _originControllerCenter;

            Vector3 movementInput = controllerCenter - _originControllerCenter;
            _displayInstance.transform.up = movementInput.normalized;
            _displayBorderInstance.GetComponent<InputIndicatorVisualHelp>().DisplayIndicators(movementInput,PlayModeManager.Instance.editorDataSO.teleportationRotationDistance,_displayInstance,PlayModeManager.Instance.editorDataSO.teleportationRotationDistance*1.5f,PlayModeManager.Instance.editorDataSO.rotationMode,PlayModeManager.Instance.editorDataSO.enableTeleportRotationLeaning);
            if (movementInput.magnitude > PlayModeManager.Instance.editorDataSO.teleportationRotationDistance)
            {
                _anchorToCenter = _originControllerCenter - _turnAnchorObject.transform.position;
                _destinyPos = _anchorToCenter.magnitude * movementInput.normalized + _turnAnchorObject.transform.position - _originTransform.GetComponentInChildren<Camera>().transform.localPosition;
                _anchorToDestiny = _destinyPos - _turnAnchorObject.transform.position;
                _reachedTpTreshhold = true;
                if (!PlayModeManager.Instance.editorDataSO.performLateTeleport)
                {
                    TeleportRotation();
                    _reachedTpTreshhold = false;
                    StopMovement();
                }
            }
            else
            {
                _reachedTpTreshhold = false;
            }

        }

        private void TeleportRotation()
        {
            _originTransform.RotateAround(_turnAnchorObject.transform.position,
                Vector3.Cross(_anchorToCenter, _anchorToDestiny), Vector3.Angle(_anchorToCenter, _anchorToDestiny));
            _originTransform.LookAt(_turnAnchorObject.transform.position);
            if (!PlayModeManager.Instance.editorDataSO.enableTeleportRotationLeaning)
            {
                _originTransform.up = Vector3.up;
                _originTransform.rotation = Quaternion.LookRotation(new Vector3(_turnAnchorObject.transform.position.x - _originTransform.position.x, 0, _turnAnchorObject.transform.position.z - _originTransform.position.z));
            }

            PlayModeManager.Instance.editorDataSO.teleportRotation++;
        }
        
        private void PerformRestrictedRotation(Handedness controllerSide)
        {
            if (controllerSide == Handedness.Right)
            {
                var controllerPosition = _leftController.transform.position;
                controllerPosition.y = 0;
                var distance = Vector3.Distance(controllerPosition, _playerViewCenter);
                ActionManager.Instance.xROrigin.transform.RotateAround(_turnAnchor, Vector3.up, distance * PlayModeManager.Instance.editorDataSO.rotationSpeed);
                _playerViewCenter = RotatePointAroundPivot(_playerViewCenter, _turnAnchor, Vector3.up * (distance * PlayModeManager.Instance.editorDataSO.rotationSpeed));

            }
            else
            {
                var controllerPosition = _rightController.transform.position;
                controllerPosition.y = 0;
                var distance = Vector3.Distance(controllerPosition, _playerViewCenter);
                ActionManager.Instance.xROrigin.transform.RotateAround(_turnAnchor, Vector3.up,
                    -distance * PlayModeManager.Instance.editorDataSO.rotationSpeed);
                _playerViewCenter = RotatePointAroundPivot(_playerViewCenter, _turnAnchor, Vector3.up * -(distance * PlayModeManager.Instance.editorDataSO.rotationSpeed));
            }
        }

        private void PerformGestureZoom()
        {
            var oldDistance = Vector3.Distance(_leftControllerZoomOrigin, _rightControllerZoomOrigin);
            var newDistance = Vector3.Distance(_leftController.transform.position, _rightController.transform.position);
            var zoomCalc = oldDistance > newDistance ? oldDistance / newDistance : newDistance / oldDistance;
            zoomCalc -= 1;
            int zoom = (int)(zoomCalc / PlayModeManager.Instance.editorDataSO.zoomStep);
            zoom = oldDistance < newDistance ? zoom : -zoom;
            Vector3 playerPos = ActionManager.Instance.xROrigin.transform.GetComponentInChildren<Camera>().transform.position;
            if (zoom != 0)
            {
                var scaleValue = PlayModeManager.Instance.currentWorldScaler.scale;
                if((Mathf.Approximately(scaleValue, PlayModeManager.Instance.editorDataSO.maximumZoom) && oldDistance < newDistance)||(Mathf.Approximately(scaleValue, PlayModeManager.Instance.editorDataSO.minimumZoom) && oldDistance > newDistance)) return;
        
                float zoomValue = Mathf.Clamp(scaleValue + zoom * PlayModeManager.Instance.editorDataSO.zoomStep, PlayModeManager.Instance.editorDataSO.minimumZoom, PlayModeManager.Instance.editorDataSO.maximumZoom);
                PlayModeManager.Instance.currentWorldScaler.SetScale(zoomValue);

                _leftControllerZoomOrigin = _leftController.transform.position;
                _rightControllerZoomOrigin = _rightController.transform.position;
                _originControllerCenter += (ActionManager.Instance.xROrigin.transform.GetComponentInChildren<Camera>().transform.position - playerPos);
                _playerViewCenter = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.position;
                _playerViewCenter += ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.forward;
                ActionManager.Instance.RefreshCurrentWidget();
                ActionManager.Instance.DisplayCameraOverlay("New size:"+ (1 / zoomValue).ToString("0.00"),2);
                _performedZoom = true;
            }
        }

        public void PerformMenuZoom(float sizeValue)
        {
            PlayModeManager.Instance.currentWorldScaler.SetScale(sizeValue);
            Vector3 playerPos = ActionManager.Instance.xROrigin.transform.GetComponentInChildren<Camera>().transform.position;
            _leftControllerZoomOrigin = _leftController.transform.position;
            _rightControllerZoomOrigin = _rightController.transform.position;
            _originControllerCenter += (ActionManager.Instance.xROrigin.transform.GetComponentInChildren<Camera>().transform.position - playerPos);
            _playerViewCenter = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.position;
            _playerViewCenter += ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.forward;
            ActionManager.Instance.RefreshCurrentWidget();
            PlayModeManager.Instance.editorDataSO.menuZoom++;
        }
        
        public void PerformTeleportRaycast(Handedness handedness)
        {
            LineRenderer usedLine = handedness == Handedness.Left ? _leftTeleportLine : _rightTeleportLine;
            usedLine.enabled = true;
            if (handedness == Handedness.Left)
            {
                leftGrabMoveProvider.enabled = false;
                _leftController.GetComponentInChildren<NearFarInteractor>().enabled = false;
            }

            if (handedness == Handedness.Right)
            {
                rightGrabMoveProvider.enabled = false;
                _rightController.GetComponentInChildren<NearFarInteractor>().enabled = false;
            }
            
            RaycastHit hit;
            if (Physics.Raycast(usedLine.gameObject.transform.position, usedLine.gameObject.transform.forward, out hit, PlayModeManager.Instance.editorDataSO.maxTeleportDistance))
            {
                usedLine.positionCount = 2;
                usedLine.SetPosition(0, usedLine.gameObject.transform.position);
                usedLine.SetPosition(1, hit.point);
                usedLine.startColor = Color.green;
                usedLine.endColor = Color.green;
                _lastTeleportPosition = hit.point;
            }
            else
            {
                usedLine.positionCount = 2;
                usedLine.SetPosition(0, usedLine.gameObject.transform.position);
                usedLine.SetPosition(1, usedLine.gameObject.transform.position+usedLine.gameObject.transform.forward*2);
                usedLine.startColor = Color.white;
                usedLine.endColor = Color.white;
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0, 0.002f), new GradientAlphaKey(0.5f, 0.12f),new GradientAlphaKey(0.25f, 0.5f),new GradientAlphaKey(0, 0.85f) }
                );
                usedLine.colorGradient = gradient;
                _lastTeleportPosition = Vector3.zero;
            }
        }

        public void PerformTeleport()
        {
            if(_lastTeleportPosition != Vector3.zero)
                ActionManager.Instance.xROrigin.transform.position = _lastTeleportPosition;
            leftGrabMoveProvider.enabled = true;
            rightGrabMoveProvider.enabled = true;
            _leftController.GetComponent<LineRenderer>().enabled = false;
            _rightController.GetComponent<LineRenderer>().enabled = false;
            _leftController.GetComponentInChildren<NearFarInteractor>().enabled = true;
            _rightController.GetComponentInChildren<NearFarInteractor>().enabled = true;

        }
        
        private Vector3 RotatePointAroundPivot(Vector3 point,Vector3 pivot, Vector3 angles){
            var dir = point - pivot; 
            dir = Quaternion.Euler(angles) * dir; 
            point = dir + pivot; 
            return point; 
        }

        public void PerformLinearMovement(Vector2 movement)
        {
            GameObject xrOrigin = ActionManager.Instance.xROrigin;
            var xrcamera = xrOrigin.GetComponent<XROrigin>().Camera.transform;
            Vector3 forwardDir = Vector3.ProjectOnPlane(xrcamera.forward, Vector3.up).normalized;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir);
            Vector3 moveDirection = forwardDir * movement.y + rightDir * movement.x;
            xrOrigin.GetComponent<CharacterController>().Move(moveDirection * (PlayModeManager.Instance.editorDataSO.movementSpeed * Time.deltaTime));
        }
        
        public void PerformFlyingMovement(Vector2 movement)
        {
            GameObject xrOrigin = ActionManager.Instance.xROrigin;
            Vector3 upDir = xrOrigin.transform.up;
            Vector3 moveDirection = upDir * movement.y;
            xrOrigin.GetComponent<CharacterController>().Move(moveDirection * (PlayModeManager.Instance.editorDataSO.movementSpeed * Time.deltaTime));
        }
        
        public void FollowCameraPath(CameraFigure cameraFigure)
        {
            _keyFrameIndex = 0;
            currentCameraKeyFrame = cameraFigure.keyFrames[_keyFrameIndex];
            _currentCameraFigure = cameraFigure;
            _movementTimer = 0;
            _rotationTimer = 0;
            _startPosition = ActionManager.Instance.xROrigin.transform.position;
            _savedChildWorldRotation = currentCameraKeyFrame.cameraRotation;
            _originalChildWorldRotation = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.rotation;
            _originalChildWorldPosition = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.position;
        }

        private void MoveCamera()
        {
            GameObject xrOrigin = ActionManager.Instance.xROrigin;

            if (_movementTimer / currentCameraKeyFrame.cameraMoveTime <= 1)
            {

                xrOrigin.transform.position = Vector3.Lerp(_startPosition,
                    currentCameraKeyFrame.cameraPosition - ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.localPosition,_movementTimer / currentCameraKeyFrame.cameraMoveTime);
                _originalChildWorldPosition = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.position;
                
                _movementTimer += Time.deltaTime;
            }

            _rotationTimer += Time.deltaTime;

            
            if (_rotationTimer / currentCameraKeyFrame.cameraRotateTime <= 1)
            {
                Quaternion currentLocalRotation = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.localRotation;
                Quaternion dynamicTargetWorldRotation = _savedChildWorldRotation * currentLocalRotation;
                Quaternion interpolatedWorldRotation = Quaternion.Slerp(_originalChildWorldRotation, dynamicTargetWorldRotation, _rotationTimer / currentCameraKeyFrame.cameraRotateTime);
                Quaternion requiredParentRotation = interpolatedWorldRotation * Quaternion.Inverse(currentLocalRotation);

                Vector3 rotatedOffset = requiredParentRotation * ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.localPosition;
                Vector3 requiredParentPosition = _originalChildWorldPosition - rotatedOffset;
                
                ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.parent.rotation = requiredParentRotation;
                ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.parent.position = requiredParentPosition;

            }

            if (_movementTimer / currentCameraKeyFrame.cameraMoveTime >= 1 && _rotationTimer / currentCameraKeyFrame.cameraRotateTime >= 1)
            {
                _keyFrameIndex++;
                currentCameraKeyFrame = _keyFrameIndex == _currentCameraFigure.keyFrames.Count ? null : _currentCameraFigure.keyFrames[_keyFrameIndex];
                if (currentCameraKeyFrame == null)
                {
                    ActionManager.Instance.ExitCameraPath();
                }
                else
                {
                    _movementTimer = 0;
                    _rotationTimer = 0;
                    _startPosition = ActionManager.Instance.xROrigin.transform.position;
                    _savedChildWorldRotation = currentCameraKeyFrame.cameraRotation;
                    _originalChildWorldRotation = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.rotation;
                    _originalChildWorldPosition = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.position;
                }
            }
        }
    }
}