using System.Collections.Generic;
using UnityEngine;

namespace VRGreyboxing
{
    
    public class MarkerObject : ObjectBaseState
    {

        public List<List<Vector3>> markPoints;
        public List<Vector3> drawingOffsets;
        public List<Vector3> colliderCenters;
        public List<Vector3> colliderSizes;
        public List<Color> colors;
        public List<float> lineWidths;
        public string originalScenePath;



        public MarkerObject(GameObject gameObject, string persisentID, Vector3 position, Quaternion rotation, Vector3 scale,bool deleted, List<Vector3> alteredPositions,string originalScenePath, List<List<Vector3>> markPoints,List<Vector3> drawingOffsets, List<Vector3> colliderCenters, List<Vector3> colliderSizes,List<Color> colors,List<float> lineWidths) : base(gameObject, persisentID, position, rotation, scale, alteredPositions)
        {
            this.markPoints = markPoints;
            this.colliderCenters = colliderCenters;
            this.colliderSizes = colliderSizes;
            this.originalScenePath = originalScenePath;
            this.colors = colors;
            this.lineWidths = lineWidths;
            this.drawingOffsets = drawingOffsets;
            this.Deleted = deleted;
        }

        public override ObjectBaseState UndoChange()
        {
            AlteredObject alteredPrevState = prevState as AlteredObject;
            
            if (justCreated)
            {
                gameObject.SetActive(false);
                justCreated = false;
                disabled = true;

                return this;
            }
            
            if (!alteredPrevState.Deleted)
            {
                gameObject.SetActive(true);
            }
            gameObject.transform.position = alteredPrevState.Position;
            gameObject.transform.rotation = alteredPrevState.Rotation;
            gameObject.transform.localScale = alteredPrevState.Scale;
            alteredPrevState.nextState = this;
            return prevState;
        }

        public override ObjectBaseState RedoChange()
        {
            AlteredObject alteredNextState = nextState as AlteredObject;
            
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                justCreated = true;
                disabled = false;

                return this;
            }
            
            if (alteredNextState.Deleted)
            {
                gameObject.SetActive(false);
            }
            gameObject.transform.position = nextState.Position;
            gameObject.transform.rotation = nextState.Rotation;
            gameObject.transform.localScale = nextState.Scale;
            nextState.prevState = this;
            return nextState;
        }
    }
}
