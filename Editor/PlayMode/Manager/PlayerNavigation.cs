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
        public float zoomMoveDistancePerStep;
        public float rotationFactor;
        public float maxTeleportDistance;
        public float freeRotationCenterDistance;
        public float teleportationRotationDistance;
        public float leaningSpeed;
        public float zoomMenuTime;
        public float movementSpeed;

        public GameObject navigationInputDisplay;
        public GameObject navigationInputDisplayBorder;
        private GameObject _displayInstance;
        private GameObject _displayBorderInstance;

        private Handedness _currentHandedness;
        [HideInInspector] public int movementCounter;

        private GameObject _leftController;
        public GrabMoveProvider leftGrabMoveProvider;
        private GameObject _rightController;
        public GrabMoveProvider rightGrabMoveProvider;

        private Vector3 _leftControllerZoomOrigin;
        private Vector3 _rightControllerZoomOrigin;
        private Vector3 _leftControllerRotationOrigin;
        private Vector3 _rightControllerRotationOrigin;

        private Vector3 _turnAnchor;
        private GameObject _turnAnchorObject;
        private Vector3 _playerCenter;
        private Vector3 _originControllerCenter;

        private bool _startedZoom;
        private bool _startedRotation;

        private LineRenderer _leftTeleportLine;
        private LineRenderer _rightTeleportLine;
        private Vector3 _lastTeleportPosition;

        private float _zoomMenuTimer;
        private float _lineSizePlayerRatio;

        private void Start()
        {
            movementCounter = 0;
            _leftController = ActionManager.Instance.leftController;
            _rightController = ActionManager.Instance.rightController;
            _leftTeleportLine = _leftController.GetComponent<LineRenderer>();
            _rightTeleportLine = _rightController.GetComponent<LineRenderer>();
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
            }
        }

        public void StopMovement()
        {
            movementCounter = 2;
            if(_zoomMenuTimer >= 0) ActionManager.Instance.DisplayZoomMenu();
        }

        public void PerformLeaning(Vector2 direction)
        {
            Transform originTransform = ActionManager.Instance.xROrigin.transform;
            if(Mathf.Abs(direction.y) > 0.01f)
                originTransform.Rotate(originTransform.right,direction.y*leaningSpeed*Time.deltaTime);
            if(Mathf.Abs(direction.x) > 0.01f)
                originTransform.Rotate(originTransform.forward,-direction.x*leaningSpeed*Time.deltaTime);
        }

        public void PerformZoomRotation(RaycastHit hitLeft, RaycastHit hitRight)
        {
            if (movementCounter == 0)
            {
                leftGrabMoveProvider.enabled = false;
                rightGrabMoveProvider.enabled = false;
                _leftControllerZoomOrigin = _leftController.transform.position;
                _rightControllerZoomOrigin = _rightController.transform.position;
                _leftControllerRotationOrigin = _leftController.transform.position;
                _rightControllerRotationOrigin = _rightController.transform.position;
                _playerCenter = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.position;
                _playerCenter += ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.forward;
                _originControllerCenter =
                    (_leftController.transform.position + _rightController.transform.position) / 2;
                _startedRotation = _startedZoom = false;

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
                    else
                        _startedZoom = true;
                }

                movementCounter = 1;
                if(PlayModeManager.Instance.editorDataSO.zoomMode == 1)
                    _zoomMenuTimer = zoomMenuTime;
                return;
            }
            
            var cameraPosition =
                ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.position;
            var leftControllerPosition = _leftController.transform.position;
            var rightControllerPosition = _rightController.transform.position;

            if (!_startedZoom)
            {
                if (PlayModeManager.Instance.editorDataSO.rotationMode == 0)
                {
                    leftControllerPosition.y = 0;
                    rightControllerPosition.y = 0;
                    cameraPosition.y = 0;
                    _playerCenter.y = 0;
                    var normal = (_playerCenter - cameraPosition).normalized;
                    var leftControllerSide =
                        Vector3.Cross(normal, (leftControllerPosition - cameraPosition).normalized).y > 0
                            ? Handedness.Left
                            : Handedness.Right;
                    var rightControllerSide =
                        Vector3.Cross(normal, (rightControllerPosition - cameraPosition).normalized).y > 0
                            ? Handedness.Left
                            : Handedness.Right;
                    Debug.DrawLine(_playerCenter, cameraPosition, Color.red);
                    Debug.DrawLine(leftControllerPosition, cameraPosition, Color.green);
                    Debug.DrawLine(rightControllerPosition, cameraPosition, Color.blue);

                    if (leftControllerSide == rightControllerSide)
                    {
                        _startedRotation = true;
                        PerformRestrictedRotation(leftControllerSide);
                        return;
                    }
                }
                else if (PlayModeManager.Instance.editorDataSO.rotationMode == 1)
                {
                    if (_displayInstance == null)
                    {
                        _displayInstance = Instantiate(navigationInputDisplay);
                        _displayBorderInstance = Instantiate(navigationInputDisplayBorder);

                    }
                    _displayBorderInstance.transform.localScale = new Vector3(freeRotationCenterDistance*2, freeRotationCenterDistance*2, freeRotationCenterDistance*2) * ActionManager.Instance.GetCurrentSizeRatio();
                    _displayInstance.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f) * ActionManager.Instance.GetCurrentSizeRatio();
                    Vector3 controllerCenter = (leftControllerPosition + rightControllerPosition) / 2;
                    _displayInstance.transform.position = controllerCenter;
                    _displayBorderInstance.transform.position = _originControllerCenter;
                    // Get the movement delta from the initial controller center
                    Vector3 movementInput = controllerCenter - _originControllerCenter;
                    Debug.DrawLine(_originControllerCenter, controllerCenter, Color.red);
                    if (movementInput.magnitude > freeRotationCenterDistance*ActionManager.Instance.GetCurrentSizeRatio())
                    {
                        _startedRotation = true;

                        // Vector from anchor to original center
                        Vector3 centerToAnchor = _originControllerCenter - _turnAnchor;


                        Vector3 dirNormalized = movementInput.normalized;
                        float distance = Vector3.Dot(centerToAnchor, dirNormalized);
                        Vector3 lockedPosition = controllerCenter + dirNormalized * distance;


                        // Project the movement onto a plane perpendicular to the vector from anchor to center
                        Vector3 projectedMovement =
                            Vector3.ProjectOnPlane(movementInput, centerToAnchor.normalized);

                        // Calculate rotation axis as cross product
                        Vector3 turnAxis = Vector3.Cross(centerToAnchor, projectedMovement).normalized;

                        // Use angle based on projected movement magnitude
                        float angle = projectedMovement.magnitude * rotationFactor;

                        // Apply rotation using quaternion to prevent gimbal lock
                        Quaternion rotation = Quaternion.AngleAxis(angle, turnAxis);
                        Transform originTransform = ActionManager.Instance.xROrigin.transform;

                        // Rotate around anchor
                        Vector3 direction = originTransform.position - _turnAnchor;
                        direction = rotation * direction;
                        originTransform.position = _turnAnchor + direction;

                        if (Vector3.Distance(lockedPosition, _turnAnchor) >
                            Vector3.Distance(_originControllerCenter, _turnAnchor))
                        {
                            originTransform.position += centerToAnchor.normalized * (distance * 0.01f);
                        }
                        else
                        {
                            originTransform.position -= centerToAnchor.normalized * (distance * 0.01f);
                        }

                        originTransform.rotation = rotation * originTransform.rotation;

                        // Rotate original controller center to match
                        _originControllerCenter = rotation * (_originControllerCenter - _turnAnchor) + _turnAnchor;
                        if (Vector3.Distance(lockedPosition, _turnAnchor) >
                            Vector3.Distance(_originControllerCenter, _turnAnchor))
                        {
                            _originControllerCenter += centerToAnchor.normalized * (distance * 0.01f);
                        }
                        else
                        {
                            _originControllerCenter -= centerToAnchor.normalized * (distance * 0.01f);
                        }

                        return;
                    }
                }
                else if (PlayModeManager.Instance.editorDataSO.rotationMode == 2)
                {
                    PerformTeleportRotation();
                }
            }

            if (!_startedRotation)
            {
                if (PlayModeManager.Instance.editorDataSO.zoomMode == 0)
                {
                    PerformGestureZoom();
                }
            }
        }

        private void PerformUnrestrictedRotation(Vector3 input)
        {
            Vector3 rotationVector = new Vector3(input.x, input.y,0);
            Vector3 centerToAnchor = ActionManager.Instance.xROrigin.transform.InverseTransformDirection(_turnAnchor) -
                                     ActionManager.Instance.xROrigin.transform.InverseTransformDirection(
                                         _originControllerCenter);
            Vector3 turnAxis = Vector3.Cross(centerToAnchor, rotationVector);
            ActionManager.Instance.xROrigin.transform.RotateAround(_turnAnchor, turnAxis,
                -rotationVector.magnitude * rotationFactor);
            _originControllerCenter = RotatePointAroundPivot(_originControllerCenter, _turnAnchor,
                turnAxis * (-rotationVector.magnitude * rotationFactor));
            
        }

        private void PerformTeleportRotation()
        {
            if (_displayInstance == null)
            {
                _displayInstance = Instantiate(navigationInputDisplay);
                _displayBorderInstance = Instantiate(navigationInputDisplayBorder);

            }
            _displayBorderInstance.transform.localScale = new Vector3(freeRotationCenterDistance*2, freeRotationCenterDistance*2, freeRotationCenterDistance*2) * ActionManager.Instance.GetCurrentSizeRatio();
            _displayInstance.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f) * ActionManager.Instance.GetCurrentSizeRatio();

            Vector3 controllerCenter = (_leftController.transform.position + _rightController.transform.position) / 2;
            _displayInstance.transform.position = controllerCenter;
            _displayBorderInstance.transform.position = _originControllerCenter;

            // Get the movement delta from the initial controller center
            Vector3 movementInput = controllerCenter - _originControllerCenter;
            if (movementInput.magnitude > teleportationRotationDistance*ActionManager.Instance.GetCurrentSizeRatio())
            {
                Vector3 anchorToCenter = _originControllerCenter - _turnAnchorObject.transform.position;
                Transform originTransform = ActionManager.Instance.xROrigin.transform;
                Vector3 destinyPos = anchorToCenter.magnitude * movementInput.normalized + _turnAnchorObject.transform.position - originTransform.GetComponentInChildren<Camera>().transform.localPosition;
                Vector3 anchorToDestiny = destinyPos - _turnAnchorObject.transform.position;
                originTransform.RotateAround(_turnAnchorObject.transform.position,Vector3.Cross(anchorToCenter,anchorToDestiny),Vector3.Angle(anchorToCenter,anchorToDestiny));
                originTransform.LookAt(_turnAnchorObject.transform.position);
                originTransform.up = Vector3.up;
                originTransform.rotation = Quaternion.LookRotation(new Vector3(_turnAnchorObject.transform.position.x-originTransform.position.x,0,_turnAnchorObject.transform.position.z-originTransform.position.z));
                StopMovement();
            }

        }
        
        private void PerformRestrictedRotation(Handedness controllerSide)
        {
            if (controllerSide == Handedness.Right)
            {
                var controllerPosition = _leftController.transform.position;
                controllerPosition.y = 0;
                var distance = Vector3.Distance(controllerPosition, _playerCenter);
                ActionManager.Instance.xROrigin.transform.RotateAround(_turnAnchor, Vector3.up,
                    distance * rotationFactor);
                _playerCenter = RotatePointAroundPivot(_playerCenter, _turnAnchor,
                    Vector3.up * (distance * rotationFactor));

            }
            else
            {
                var controllerPosition = _rightController.transform.position;
                controllerPosition.y = 0;
                var distance = Vector3.Distance(controllerPosition, _playerCenter);
                ActionManager.Instance.xROrigin.transform.RotateAround(_turnAnchor, Vector3.up,
                    -distance * rotationFactor);
                _playerCenter = RotatePointAroundPivot(_playerCenter, _turnAnchor, Vector3.up * -(distance * rotationFactor));
            }
        }

        private void PerformGestureZoom()
        {
            var oldDistance = Vector3.Distance(_leftControllerZoomOrigin, _rightControllerZoomOrigin);
            var newDistance = Vector3.Distance(_leftController.transform.position, _rightController.transform.position);
            var zoomCalc = oldDistance > newDistance ? oldDistance / newDistance : newDistance / oldDistance;
            zoomCalc -= 1;
            int zoom = (int)(zoomCalc / PlayModeManager.Instance.editorDataSO.zoomStep);
            zoom = oldDistance > newDistance ? zoom : -zoom;
            Vector3 playerPos = ActionManager.Instance.xROrigin.transform.GetComponentInChildren<Camera>().transform.position;
            if (zoom != 0)
            {
                var scaleValue = ActionManager.Instance.xROrigin.transform.localScale.x;
                if((Mathf.Approximately(scaleValue, PlayModeManager.Instance.editorDataSO.maximumZoom) && oldDistance > newDistance)||(Mathf.Approximately(scaleValue, PlayModeManager.Instance.editorDataSO.minimumZoom) && oldDistance < newDistance)) return;
                float zoomValue = Mathf.Clamp(scaleValue + zoom * PlayModeManager.Instance.editorDataSO.zoomStep, PlayModeManager.Instance.editorDataSO.minimumZoom, PlayModeManager.Instance.editorDataSO.maximumZoom);
                ActionManager.Instance.xROrigin.transform.localScale = new Vector3(zoomValue, zoomValue, zoomValue);
                ActionManager.Instance.xROrigin.transform.Translate(Camera.main.transform.forward * (zoomMoveDistancePerStep * -zoom), Space.World);
                LineRenderer[] lines = ActionManager.Instance.xROrigin.GetComponentsInChildren<LineRenderer>(true);
                foreach (LineRenderer l in lines)
                {
                    l.startWidth = zoomValue * _lineSizePlayerRatio;
                    l.endWidth = zoomValue * _lineSizePlayerRatio;
                }

                _leftControllerZoomOrigin = _leftController.transform.position;
                _rightControllerZoomOrigin = _rightController.transform.position;
                _originControllerCenter += (ActionManager.Instance.xROrigin.transform.GetComponentInChildren<Camera>().transform.position - playerPos);
                _playerCenter = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.position;
                _playerCenter += ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.forward;
            }
        }

        public void PerformMenuZoom(float sizeValue)
        {
            var oldSize = ActionManager.Instance.xROrigin.transform.localScale.x;
            ActionManager.Instance.xROrigin.transform.localScale = new Vector3(sizeValue, sizeValue, sizeValue);
            Vector3 playerPos = ActionManager.Instance.xROrigin.transform.GetComponentInChildren<Camera>().transform.position;
            //ActionManager.Instance.xROrigin.transform.Translate(Camera.main.transform.forward * (zoomMoveDistancePerStep * ((sizeValue-oldSize)/PlayModeManager.Instance.editorDataSO.zoomStep)), Space.World);
            LineRenderer[] lines = ActionManager.Instance.xROrigin.GetComponentsInChildren<LineRenderer>(true);
            foreach (LineRenderer l in lines)
            {
                l.startWidth = sizeValue * _lineSizePlayerRatio;
                l.endWidth = sizeValue * _lineSizePlayerRatio;
            }

            _leftControllerZoomOrigin = _leftController.transform.position;
            _rightControllerZoomOrigin = _rightController.transform.position;
            _originControllerCenter += (ActionManager.Instance.xROrigin.transform.GetComponentInChildren<Camera>().transform.position - playerPos);
            _playerCenter = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.position;
            _playerCenter += ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.forward;

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
            if (Physics.Raycast(usedLine.gameObject.transform.position, usedLine.gameObject.transform.forward, out hit, maxTeleportDistance))
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
            }
        }

        public void PerformTeleport()
        {
            ActionManager.Instance.xROrigin.transform.position = _lastTeleportPosition;
            leftGrabMoveProvider.enabled = true;
            rightGrabMoveProvider.enabled = true;
            _leftController.GetComponent<LineRenderer>().enabled = false;
            _rightController.GetComponent<LineRenderer>().enabled = false;
            _leftController.GetComponentInChildren<NearFarInteractor>().enabled = true;
            _rightController.GetComponentInChildren<NearFarInteractor>().enabled = true;

        }
        
        private Vector3 RotatePointAroundPivot(Vector3 point,Vector3 pivot, Vector3 angles){
            var dir = point - pivot; // get point direction relative to pivot
            dir = Quaternion.Euler(angles) * dir; // rotate it
            point = dir + pivot; // calculate rotated point
            return point; // return it
        }

        public void PerformLinearMovement(Vector2 movement)
        {
            GameObject xrOrigin = ActionManager.Instance.xROrigin;
            var xrcamera = xrOrigin.GetComponent<XROrigin>().Camera.transform;
            Vector3 forwardDir = Vector3.ProjectOnPlane(xrcamera.forward, Vector3.up).normalized;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir);
            Vector3 moveDirection = forwardDir * movement.y + rightDir * movement.x;
            xrOrigin.GetComponent<CharacterController>().Move(moveDirection * movementSpeed * Time.deltaTime);
        }
    }
}
