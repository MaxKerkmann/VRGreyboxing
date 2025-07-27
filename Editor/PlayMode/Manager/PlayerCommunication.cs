using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRGreyboxing
{
    public class PlayerCommunication : MonoBehaviour
    {
        public XRInteractionManager xrInteractionManager;

        public Material drawingMaterial;
        public float lineWidth = 0.01f;

        private LineRenderer _currentDrawing;
        private int _currentIndex;
        private Color _currentColor;
        private List<GameObject> _currentDrawingObjects;
        
        private GameObject _leftController;
        private GameObject _rightController;

        private void Start()
        {
            _leftController = ActionManager.Instance.leftController;
            _rightController = ActionManager.Instance.rightController;
            _currentDrawingObjects = new List<GameObject>();
            SetColor(Color.black);
        }

        public void DrawLine(Handedness handedness)
        {
            GameObject usedController = handedness == Handedness.Left ? _leftController : _rightController;
            Vector3 pos = usedController.GetComponentInChildren<XRControllerRaycaster>().pokePointTransform.position;

            if (_currentDrawing == null)
            {
                _currentIndex = 0;
                _currentDrawing = new GameObject("Drawing " + _currentDrawingObjects.Count).AddComponent<LineRenderer>();
                _currentDrawing.useWorldSpace = false;
                _currentDrawing.gameObject.tag = "VRG_Mark";
                _currentDrawing.material = drawingMaterial;
                _currentDrawing.startColor = _currentDrawing.endColor = _currentColor;
                _currentDrawing.startWidth = _currentDrawing.endWidth = lineWidth*ActionManager.Instance.GetCurrentSizeRatio();
                _currentDrawing.positionCount = 1;
                _currentDrawing.SetPosition(0, pos);
                _currentDrawingObjects.Add(_currentDrawing.gameObject);
            }
            else
            {
                var currentPos = _currentDrawing.GetPosition(_currentIndex);
                if (Vector3.Distance(currentPos, pos) > lineWidth*ActionManager.Instance.GetCurrentSizeRatio())
                {
                    _currentIndex++;
                    _currentDrawing.positionCount = _currentIndex+1;
                    _currentDrawing.SetPosition(_currentIndex, pos);
                }
            }
        }
        public void ResetDrawing()
        {
            if(_currentDrawing==null) return;
            Vector3 center = Vector3.zero;
            for (int i = 0; i < _currentDrawing.positionCount; i++)
            {
                center += _currentDrawing.GetPosition(i);
            }
            center /= _currentDrawing.positionCount;
            _currentDrawing.gameObject.transform.position = center;

            for (int i = 0; i < _currentDrawing.positionCount; i++)
            {
                _currentDrawing.SetPosition(i, _currentDrawing.GetPosition(i)-center);
            }
            
            _currentDrawing = null;

        }

        public void EraseLine(Handedness handedness)
        {
            GameObject usedController = handedness == Handedness.Left ? _leftController : _rightController;
            Vector3 pos = usedController.GetComponentInChildren<XRControllerRaycaster>().pokePointTransform.position;
            GameObject[] drawings = GameObject.FindGameObjectsWithTag("VRG_Mark");
            foreach (var drawing in drawings)
            {
                LineRenderer line = drawing.GetComponent<LineRenderer>();
                if(line == null) continue;
                int numPoints = line.positionCount;
                if (numPoints < 2) continue;

                List<Vector3> pointList = new List<Vector3>();
                for (int i = 0; i < numPoints; i++)
                {
                    pointList.Add(line.transform.TransformPoint(line.GetPosition(i)));
                }

                for (int i = 0; i < pointList.Count - 1; i++)
                {
                    Vector3 a = pointList[i];
                    Vector3 b = pointList[i + 1];

                    float distance = DistancePointToSegment(pos, a, b);
                    if (distance < lineWidth*ActionManager.Instance.GetCurrentSizeRatio()*3)
                    {
                        SplitLine(line, i + 1);
                        return;
                    }
                }
            }
        }
        
        private float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            Vector3 ap = point - a;
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
            Vector3 closestPoint = a + t * ab;
            return Vector3.Distance(point, closestPoint);
        }

        private void SplitLine(LineRenderer original, int splitIndex)
        {
            int total = original.positionCount;
            if (splitIndex <= 0 || splitIndex >= total) return;

            Vector3[] before = new Vector3[splitIndex];
            Vector3[] after = new Vector3[total - splitIndex];

            for (int i = 0; i < splitIndex; i++) before[i] = original.GetPosition(i);
            for (int i = splitIndex; i < total; i++) after[i - splitIndex] = original.GetPosition(i);

            if (before.Length > 1)
            {
                GameObject beforeObj = new GameObject("LineBefore");
                beforeObj.transform.position = original.transform.position;
                _currentDrawingObjects.Add(beforeObj);
                beforeObj.tag = "VRG_Mark";
                LineRenderer lineBefore = beforeObj.AddComponent<LineRenderer>();
                SetupLineCopy(original, lineBefore, before);
            }

            if (after.Length > 1)
            {
                GameObject afterObj = new GameObject("LineAfter");
                afterObj.transform.position = original.transform.position;
                _currentDrawingObjects.Add(afterObj);
                afterObj.tag = "VRG_Mark";
                LineRenderer lineAfter = afterObj.AddComponent<LineRenderer>();
                SetupLineCopy(original, lineAfter, after);
            }

            Destroy(original.gameObject);
        }

        private void SetupLineCopy(LineRenderer original, LineRenderer copy, Vector3[] points)
        {
            copy.positionCount = points.Length;
            copy.SetPositions(points);
            copy.material = original.material;
            copy.startColor = copy.endColor = original.startColor;
            copy.widthCurve = original.widthCurve;
            copy.widthMultiplier = original.widthMultiplier;
            copy.numCapVertices = original.numCapVertices;
            copy.useWorldSpace = original.useWorldSpace;
            Vector3 center = Vector3.zero;
            for (int i = 0; i < copy.positionCount; i++)
            {
                center += copy.transform.TransformPoint(points[i]);
            }
            center /= copy.positionCount;
            for (int i = 0; i < copy.positionCount; i++)
            {
                copy.SetPosition(i, copy.transform.InverseTransformPoint(copy.transform.TransformPoint(copy.GetPosition(i))+(copy.transform.position-center)));
            }
            copy.gameObject.transform.position = center;
        }

        public void ConfirmDrawing()
        {
            GameObject drawingContainer = new GameObject("DrawingContainer");
            drawingContainer.tag = "VRG_Mark";
            Vector3 center  = Vector3.zero;
            foreach (var drawingObject in _currentDrawingObjects)    
            {
                drawingObject.transform.parent = drawingContainer.transform;
                center += drawingObject.transform.position;
            }
            center /= _currentDrawingObjects.Count;
            drawingContainer.transform.position = center;
            foreach (var drawingObject in _currentDrawingObjects)    
            {
                drawingObject.transform.position -= center;
            }

            SaveDrawing(drawingContainer);
            _currentDrawingObjects.Clear();
            ActionManager.Instance.CloseConfirmMenu();
        }
        
        private void SaveDrawing(GameObject drawing)
        {

            List<List<Vector3>> drawingpoints = new List<List<Vector3>>();
            List<Vector3> colliderCenters = new List<Vector3>();
            List<Vector3> colliderSizes = new List<Vector3>();
            List<Color> drawingColors = new List<Color>();
            List<float> lineWidths = new List<float>();
            for (int i = 0; i < drawing.transform.childCount; i++)
            {
                LineRenderer line = drawing.transform.GetChild(i).GetComponent<LineRenderer>();
                if (line.positionCount == 0) continue;

                IdHolderInformation idHolderInfo = drawing.transform.GetChild(i).gameObject.AddComponent<IdHolderInformation>();
                idHolderInfo.iDHolder = drawing.transform;
                
                drawingColors.Add(line.startColor);
                lineWidths.Add(line.startWidth);
                Vector3[] positions = new Vector3[line.positionCount];
                line.GetPositions(positions);
                drawingpoints.Add(positions.ToList());
                // Calculate bounds
                Bounds bounds = new Bounds(positions[0], Vector3.zero);
                for (int y = 1; y < positions.Length; y++)
                {
                    bounds.Encapsulate(positions[y]);
                }

                // Create or update BoxCollider
                BoxCollider box = drawing.transform.GetChild(i).gameObject.GetComponent<BoxCollider>();
                if (box == null)
                {
                    box = drawing.transform.GetChild(i).gameObject.AddComponent<BoxCollider>();
                }
                box.center = bounds.center;
                box.size = bounds.size;
                colliderCenters.Add(bounds.center);
                colliderSizes.Add(bounds.size);
                drawing.transform.GetChild(i).gameObject.AddComponent<Rigidbody>();
                XRGrabInteractable interactable = drawing.transform.GetChild(i).gameObject.AddComponent<XRGrabInteractable>();
                ActionManager.Instance.SetupInteractable(interactable);
                drawing.transform.GetChild(i).gameObject.AddComponent<DrawingGrabTransformer>();
            }

            Collider[] childColliders = drawing.GetComponentsInChildren<Collider>();
            BoxCollider parentCollider = drawing.AddComponent<BoxCollider>();
            
            EncapsulateDrawing(parentCollider,childColliders);
            
            parentCollider.enabled = false;
            ResetDrawing();
            PlayModeManager.Instance.RegisterObjectChange(drawing,false,-1,false,false,"","",new List<Vector3>(),false,drawingpoints,colliderCenters,colliderSizes,drawingColors,lineWidths);
        }

        public void EncapsulateDrawing(BoxCollider parentCollider, Collider[] drawingColliders)
        {
            // Start with first child's bounds
            Bounds combinedBounds = drawingColliders[0].bounds;

            for (int i = 1; i < drawingColliders.Length; i++)
            {
                combinedBounds.Encapsulate(drawingColliders[i].bounds);
            }

            // Convert world bounds to local space
            Vector3 localCenter = parentCollider.transform.InverseTransformPoint(combinedBounds.center);
            Vector3 localSize = parentCollider.transform.InverseTransformVector(combinedBounds.size);

            parentCollider.center = localCenter;
            parentCollider.size = localSize;
        }

        public void RevokeDrawing()
        {
            foreach (var drawing in _currentDrawingObjects.ToList())
            {
                Destroy(drawing);
            }
            _currentDrawingObjects.Clear();
            ActionManager.Instance.CloseConfirmMenu();
        }
        
        public void SetColor(Color color)
        {
            _currentColor = color;
            _leftController.GetComponentInChildren<XRControllerRaycaster>().pokePointTransform.parent.GetComponentInChildren<SkinnedMeshRenderer>().material.SetColor("_BaseColor",_currentColor); 
            _leftController.GetComponentInChildren<XRControllerRaycaster>().pokePointTransform.parent.GetComponentInChildren<SkinnedMeshRenderer>().material.SetColor("_RimColor",_currentColor); 
            _rightController.GetComponentInChildren<XRControllerRaycaster>().pokePointTransform.parent.GetComponentInChildren<SkinnedMeshRenderer>().material.SetColor("_BaseColor",_currentColor); 
            _rightController.GetComponentInChildren<XRControllerRaycaster>().pokePointTransform.parent.GetComponentInChildren<SkinnedMeshRenderer>().material.SetColor("_RimColor",_currentColor); 
        }
        
        
    }
}