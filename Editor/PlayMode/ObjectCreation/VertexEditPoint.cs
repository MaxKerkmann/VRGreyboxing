using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace VRGreyboxing
{
    /**
     * Placeable vertex to create new objects
     */
    public class VertexEditPoint : MonoBehaviour
    {
        public VertexEditPoint connectedObject;
        private LineRenderer _lineRenderer;

        private void Start()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, transform.position);
        }

        private void Update()
        {
            if (connectedObject != null)
            {
                _lineRenderer.SetPosition(0, transform.position);
                _lineRenderer.SetPosition(1, connectedObject.transform.position);
            }
        }

        /**
         * Connect placed vertex visually and save connection
         * If connection loops initialize shape creation
         **/
        public List<VertexEditPoint> Connect(VertexEditPoint obj, Handedness handedness,PlayerEdit playerEdit)
        {
            connectedObject = obj;
            _lineRenderer.SetPosition(1, obj.transform.position);
            List<VertexEditPoint> connectionPoints = new List<VertexEditPoint>();
            VertexEditPoint next = connectedObject;
            while (next != null)
            {
                connectionPoints.Add(next);
                if (next.connectedObject != this)
                {
                    next = next.connectedObject;
                    continue;
                }
                if(connectionPoints.Count == 1) break;
                connectionPoints.Add(this);
                playerEdit.ScaleCurrentPolyShape(handedness,null,connectionPoints);
                return connectionPoints;
            } 
            return null;
        }

        /**
         * Clear connection to vertex
         */
        public void ClearConnection()
        {
            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, transform.position);
            connectedObject = null;
        }
    }
}
