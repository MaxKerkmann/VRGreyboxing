using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ProBuilder;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRGreyboxing
{
    public class PlayerEdit : MonoBehaviour
    {
        public XRInteractionManager xrInteractionManager;

        private GameObject _leftController;
        private GameObject _rightController;
        
        [HideInInspector]
        public EditWidgetEditPoint currentEditPoint;
        [HideInInspector]
        public GameObject selectedObject;
        private bool _justSelected;
        private GameObject _currentEditWidget;
        private Handedness _selectEditHandedness;
        public GameObject currentSelectedEditWidgetPoint;
        public GameObject editWidgetPointPrefab;
        public float vertexDetectionRange;
        public GameObject vertexEditPointPrefab;
        public Material meshMaterial;

        private GameObject _lastSelectedEditPoint;
        private List<GameObject> _hoveredVertices;
        private List<GameObject> _placedVertexEditPoints;
        public bool flipVertices;

        private List<VertexEditPoint> _invalidConnections;
        
        private void Start()
        {
            _leftController = ActionManager.Instance.leftController;
            _rightController = ActionManager.Instance.rightController;
            _hoveredVertices = new List<GameObject>();
            _invalidConnections = new List<VertexEditPoint>();
            _placedVertexEditPoints = new List<GameObject>();
        }

        public void CheckForVertexPositions()
        {
            Vector3 posLeft = _leftController.GetComponentInChildren<XRControllerRaycaster>().pokePointTransform.position;
            RaycastHit[] hits = Physics.SphereCastAll(posLeft, vertexDetectionRange, Vector3.up, vertexDetectionRange);
            if (hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject.CompareTag("VRG_WidgetCube") && hit.collider.gameObject != _lastSelectedEditPoint)
                    {
                        hit.collider.gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor",Color.blue);
                        _hoveredVertices.Add(hit.collider.gameObject);
                        if (hit.collider.gameObject.GetComponent<VertexEditPoint>() == null)
                        {
                            hit.collider.gameObject.AddComponent<VertexEditPoint>();
                            ActionManager.Instance.CopyComponent(vertexEditPointPrefab.GetComponent<LineRenderer>(), hit.collider.gameObject);
                        }
                    }
                }
            }
            Vector3 posRight = _rightController.GetComponentInChildren<XRControllerRaycaster>().pokePointTransform.position;
            hits = Physics.SphereCastAll(posRight, vertexDetectionRange, Vector3.up, vertexDetectionRange);
            if (hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject.CompareTag("VRG_WidgetCube") && hit.collider.gameObject != _lastSelectedEditPoint)
                    {
                        hit.collider.gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor",Color.blue);
                        _hoveredVertices.Add(hit.collider.gameObject);
                        if (hit.collider.gameObject.GetComponent<VertexEditPoint>() == null)
                        {
                            hit.collider.gameObject.AddComponent<VertexEditPoint>();
                            ActionManager.Instance.CopyComponent(vertexEditPointPrefab.GetComponent<LineRenderer>(), hit.collider.gameObject);
                            hit.collider.gameObject.GetComponent<LineRenderer>().material = vertexEditPointPrefab.GetComponent<LineRenderer>().material;
                        }
                    }
                }
            }
            
            foreach (var vertex in _hoveredVertices.ToList())
            {
                if (vertex != _lastSelectedEditPoint && Vector3.Distance(vertex.transform.position, posLeft) > vertexDetectionRange && Vector3.Distance(vertex.transform.position, _rightController.transform.position) > vertexDetectionRange)
                {
                    _hoveredVertices.Remove(vertex);
                    vertex.GetComponent<Renderer>().material.SetColor("_BaseColor", _invalidConnections.Contains(vertex.GetComponent<VertexEditPoint>()) ? Color.red : Color.white);

                    if (vertex.GetComponent<EditWidgetEditPoint>() != null)
                    {
                        if (vertex.GetComponent<VertexEditPoint>().connectedObject == null)
                        {
                            Destroy(vertex.GetComponent<VertexEditPoint>());
                            Destroy(vertex.GetComponent<LineRenderer>());
                        }
                    }
                }
            }
        }

        public void ClearFloatingPoints()
        {
            foreach (var floatingObject in _placedVertexEditPoints.ToList())
            {
                Destroy(floatingObject);
            }
            _placedVertexEditPoints = new List<GameObject>();
        }
        
        public List<VertexEditPoint> PlaceSelectVertex(Handedness handedness)
        {
            GameObject usedController = handedness == Handedness.Left ? _leftController : _rightController;
            Vector3 pos = usedController.GetComponentInChildren<XRControllerRaycaster>().pokePointTransform.position;

            foreach (var vertex in _hoveredVertices.ToList())
            {
                if (Vector3.Distance(vertex.transform.position, pos) < vertexDetectionRange)
                {
                    if (_lastSelectedEditPoint != null)
                    {
                        List<VertexEditPoint> points = _lastSelectedEditPoint.GetComponent<VertexEditPoint>().Connect(vertex.GetComponent<VertexEditPoint>(), handedness, this);
                        if (points != null)
                        {
                            _lastSelectedEditPoint = null;
                            _hoveredVertices = new List<GameObject>();
                            _placedVertexEditPoints = new List<GameObject>();
                            return points;
                        }
                        else
                        {
                            _lastSelectedEditPoint.GetComponent<Renderer>().material.SetColor("_BaseColor",Color.white);
                            _lastSelectedEditPoint = vertex;
                            _lastSelectedEditPoint.GetComponent<Renderer>().material.SetColor("_BaseColor", Color.green);
                        }
                    }
                    else
                    {
                        if (_invalidConnections.Contains(vertex.GetComponent<VertexEditPoint>()))
                        {
                            foreach (var vertexEditPoint in _invalidConnections)
                            {
                                vertexEditPoint.GetComponent<Renderer>().material.SetColor("_BaseColor",Color.white);
                            }
                            _invalidConnections = new List<VertexEditPoint>();
                        }
                        _lastSelectedEditPoint = vertex;
                        _lastSelectedEditPoint.GetComponent<Renderer>().material.SetColor("_BaseColor", Color.green);
                    }

                    return null;
                }
            }
            
            GameObject editPoint = Instantiate(vertexEditPointPrefab, pos, Quaternion.identity);
            _placedVertexEditPoints.Add(editPoint);
            Debug.Log("Placing New one");
            if (_lastSelectedEditPoint != null)
            { 
                Debug.Log("Connecting new one");
                _lastSelectedEditPoint.GetComponent<VertexEditPoint>().Connect(editPoint.GetComponent<VertexEditPoint>(), handedness,this);
                _lastSelectedEditPoint.GetComponent<Renderer>().material.SetColor("_BaseColor",Color.white);
                _lastSelectedEditPoint = editPoint;
                _lastSelectedEditPoint.GetComponent<Renderer>().material.SetColor("_BaseColor",Color.green);
            }
            else
            {
                _lastSelectedEditPoint = editPoint;
                _lastSelectedEditPoint.GetComponent<Renderer>().material.SetColor("_BaseColor",Color.green);
            }
            return null;
        }

        public void InvalidVertexConnection(List<VertexEditPoint> vertexEditPoints)
        {
            foreach (var editPoint in vertexEditPoints)
            {
                editPoint.ClearConnection();
                editPoint.GetComponent<Renderer>().material.SetColor("_BaseColor",Color.red);
            }
            _invalidConnections = vertexEditPoints;
        }
        
        public void ScaleCurrentPolyShape(Handedness handedness,GameObject polyShape,List<VertexEditPoint> vertexEditPoints)
        {
            GameObject usedController = handedness == Handedness.Left ? _leftController : _rightController;
            Vector3 center = Vector3.zero;
            GameObject connectedToExistingObject = null;
            foreach (VertexEditPoint vertexEditPoint in vertexEditPoints)
            {
                center += vertexEditPoint.transform.position;
                if(vertexEditPoint.gameObject.GetComponent<EditWidgetEditPoint>() != null)
                    connectedToExistingObject = vertexEditPoint.gameObject.GetComponent<EditWidgetEditPoint>().assignedObject;
            }
            center /= vertexEditPoints.Count;
            
            if (polyShape == null)
            {
                polyShape = new GameObject("PolyShape");
                polyShape.transform.position = center;
                polyShape.AddComponent<ProBuilderMesh>();
                MeshCollider col = polyShape.AddComponent<MeshCollider>();
                col.convex = true;
            }
            
            float controllerHeight = (usedController.transform.position - polyShape.transform.position).y;
            ProBuilderMesh pbm = polyShape.GetComponent<ProBuilderMesh>();
            polyShape.GetComponent<MeshRenderer>().material = meshMaterial;
            List<Vector3> vertices = vertexEditPoints.Select(obj => obj.transform.position).ToList();
            ActionResult result = pbm.CreateShapeFromPolygon(vertices,controllerHeight,!(controllerHeight > 0));
            for (int i = 0;i<vertices.Count;i++)
            {
                vertices[i] -= center;
            }
            if (result.status != ActionResult.Status.Success)
            {
                InvalidVertexConnection(vertexEditPoints);
                Destroy(polyShape);
                ActionManager.Instance.SetCurrentPolyShape(null);
            }
            else
            {
                pbm.CreateShapeFromPolygon(vertices, controllerHeight, false);
                ActionManager.Instance.SetCurrentPolyShape(polyShape);
                if(connectedToExistingObject != null)
                    polyShape.gameObject.transform.parent = connectedToExistingObject.transform;
                flipVertices = !(controllerHeight > 0);
            }
        }
        public void SelectObject(Handedness handedness, GameObject obj)
        {
            var constrainGrabTransformer = obj.GetComponent<NoneConstrainGrabTransformer>();
            if (constrainGrabTransformer != null)
            {
                EditWidgetEditPoint editWidgetEditPoint = constrainGrabTransformer.editWidgetEditPoint;
                    
                if (!_justSelected)
                {
                    currentSelectedEditWidgetPoint = editWidgetEditPoint != null ? editWidgetEditPoint.gameObject : null;
                    _selectEditHandedness = handedness;
                    GameObject usedController = handedness == Handedness.Right ? _rightController : _leftController;
                    var interactor = usedController.GetComponentInChildren<IXRSelectInteractor>();
                    var interactable = constrainGrabTransformer.gameObject.GetComponent<XRGrabInteractable>();
                    xrInteractionManager.SelectEnter(interactor, interactable);
                    return;
                }
            }
            currentSelectedEditWidgetPoint = null;

            if(obj.GetComponent<ProBuilderMesh>()==null) return;
            
            if (_justSelected) return;

            if (obj == selectedObject)
            {
                DeselectObject();
                return;
            }
            
            if (selectedObject != null)
                DeselectObject();
            
            selectedObject = obj;
            _selectEditHandedness = Handedness.None;
            _justSelected = true;
            ApplyTransWidgetToSelectedObject();
        }
        public void EndSelection(GameObject obj)
        {
            if (_selectEditHandedness != Handedness.None)
            {
                GameObject usedController = _selectEditHandedness == Handedness.Left
                    ? _leftController
                    : _rightController;
                var interactor = usedController.GetComponentInChildren<IXRSelectInteractor>();
                var interactable = obj.GetComponent<XRGrabInteractable>();
                xrInteractionManager.SelectExit(interactor, interactable);
                _selectEditHandedness = Handedness.None;
                if (currentSelectedEditWidgetPoint != null)
                {
                    currentSelectedEditWidgetPoint = null;
                    ApplyTransWidgetToSelectedObject();
                }
            }
            else
            {
                _justSelected = false;
            }
        }

        public void ApplyTransWidgetToSelectedObject()
        {
            GameObject transWidget = new GameObject("TransWidget");
            transWidget.transform.position = selectedObject.transform.position;
            if(_currentEditWidget != null)
                Destroy(_currentEditWidget);
            _currentEditWidget = transWidget;
            SetCurrentTransWidgetPositions();
        }

        public void DisableEditWidget(GameObject currentCube)
        {
            for (int i = 0;i < _currentEditWidget.transform.childCount;i++)
            {
                if ( _currentEditWidget.transform.GetChild(i).gameObject == currentCube) continue;
                _currentEditWidget.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
        
        public void SetCurrentTransWidgetPositions()
        {
            ProBuilderMesh pbm = selectedObject.GetComponent<ProBuilderMesh>();
            List<Vector3> worldPositions = pbm.positions.Select(v => pbm.transform.TransformPoint(v)).ToList();

            // Detect flat mesh
            bool isFlat = IsMeshFlat(worldPositions, 0.001f);

            // --- EDGE MARKERS ---
            Dictionary<Vector3Int, EditWidgetEditPoint> edgeMarkersByRoundedCenter = new Dictionary<Vector3Int, EditWidgetEditPoint>();

            
            foreach (var face in pbm.faces)
            {
                foreach (var edge in face.edges)
                {
                    Vector3 a = pbm.transform.TransformPoint(pbm.positions[edge.a]);
                    Vector3 b = pbm.transform.TransformPoint(pbm.positions[edge.b]);
                    Vector3 center = (a + b) * 0.5f;
                    Vector3Int key = RoundedPositionKey(center, 0.05f); // Coarse to absorb floating point offset

                    var handledIndices = new List<List<int>>
                    {
                        Enumerable.Range(0, pbm.positions.Count).Where(i => pbm.positions[i] == pbm.positions[edge.a]).ToList(),
                        Enumerable.Range(0, pbm.positions.Count).Where(i => pbm.positions[i] == pbm.positions[edge.b]).ToList()
                    };

                    if (isFlat && edgeMarkersByRoundedCenter.TryGetValue(key, out var existing))
                    {
                        existing.handledPositionIndices.AddRange(handledIndices);
                    }
                    else if (!edgeMarkersByRoundedCenter.ContainsKey(key))
                    {
                        GameObject widgetPoint = Instantiate(editWidgetPointPrefab, _currentEditWidget.transform);
                        widgetPoint.transform.localScale *= ActionManager.Instance.GetCurrentSizeRatio();
                        widgetPoint.transform.position = center;
                        var editPoint = widgetPoint.GetComponent<EditWidgetEditPoint>();
                        editPoint.playerEdit = this;
                        editPoint.handledPositionIndices = handledIndices;
                        editPoint.assignedObject = selectedObject;

                        edgeMarkersByRoundedCenter[key] = editPoint;
                    }
                }
            }

            // --- CORNER MARKERS ---
            Dictionary<Vector3Int, EditWidgetEditPoint> cornerMarkers = new Dictionary<Vector3Int, EditWidgetEditPoint>();

            foreach (var shared in pbm.sharedVertices)
            {
                int index = shared[0];
                Vector3 worldPos = pbm.transform.TransformPoint(pbm.positions[index]);
                Vector3Int key = RoundedPositionKey(worldPos, 0.001f);

                if (isFlat && cornerMarkers.TryGetValue(key, out var existing))
                {
                    existing.handledPositionIndices.Add(shared.ToList());
                }
                else if (!cornerMarkers.ContainsKey(key))
                {
                    GameObject widgetPoint = Instantiate(editWidgetPointPrefab, _currentEditWidget.transform);
                    widgetPoint.transform.position = worldPos;
                    widgetPoint.transform.localScale *= ActionManager.Instance.GetCurrentSizeRatio();

                    var editPoint = widgetPoint.GetComponent<EditWidgetEditPoint>();
                    editPoint.playerEdit = this;
                    editPoint.handledPositionIndices = new List<List<int>> { shared.ToList() };
                    editPoint.assignedObject = selectedObject;

                    
                    cornerMarkers[key] = editPoint;
                }
            }
        }
        private Vector3Int RoundedPositionKey(Vector3 pos, float precision = 0.001f)
        {
            return new Vector3Int(
                Mathf.RoundToInt(pos.x / precision),
                Mathf.RoundToInt(pos.y / precision),
                Mathf.RoundToInt(pos.z / precision)
            );
        }
        private bool IsMeshFlat(List<Vector3> points, float tolerance)
        {
            if (points.Count < 3)
                return true;

            Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;
            Plane plane = new Plane(normal, points[0]);

            foreach (var pt in points)
            {
                float distance = Mathf.Abs(plane.GetDistanceToPoint(pt));
                if (distance > tolerance)
                    return false;
            }
            return true;
        }
        
        public void DeselectObject()
        {
            Destroy(_currentEditWidget); 
            selectedObject = null;
        }

        public void EditSelectedObjectVertices(Vector3 transformPos,List<List<int>> vertexIndices)
        {
            ProBuilderMesh pbm = selectedObject.GetComponent<ProBuilderMesh>();
            Vector3 offset;
            if (vertexIndices.Count == 1)
            {
                offset = Vector3.zero;
            }
            else
            {
                var center = (pbm.positions[vertexIndices[1][0]] + pbm.positions[vertexIndices[0][0]]) / 2;
                offset = center - pbm.positions[vertexIndices[0][0]];
            }
            
            for(int i = 0;i<vertexIndices.Count;i++)
            {
                var positions = pbm.positions.ToArray();
                var posList = new List<Vector3>(positions);

                foreach (var vertexIndex in vertexIndices[i])
                {
                    posList[vertexIndex] = pbm.transform.InverseTransformPoint(transformPos);
                    posList[vertexIndex] -= offset*Mathf.Pow(-1,i);
                }
                pbm.positions = posList;
            }
            pbm.ToMesh();
            pbm.Refresh();
        }



    }
}