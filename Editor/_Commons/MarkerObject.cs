using System.Collections.Generic;
using UnityEngine;

namespace VRGreyboxing
{
    
    public class MarkerObject : ObjectBaseState
    {

        public List<List<Vector3>> MarkPoints;
        public List<Vector3> DrawingOffsets;
        public List<Vector3> ColliderCenters;
        public List<Vector3> ColliderSizes;
        public List<Color> Colors;
        public List<float> LineWidths;



        public MarkerObject(GameObject gameObject, string persisentID, Vector3 position, Quaternion rotation, Vector3 scale,bool deleted, List<Vector3> alteredPositions,string originalScenePath, List<List<Vector3>> markPoints,List<Vector3> drawingOffsets, List<Vector3> colliderCenters, List<Vector3> colliderSizes,List<Color> colors,List<float> lineWidths) : base(gameObject, persisentID, position, rotation, scale, originalScenePath,alteredPositions)
        {
            MarkPoints = markPoints;
            ColliderCenters = colliderCenters;
            ColliderSizes = colliderSizes;
            this.originalScenePath = originalScenePath;
            Colors = colors;
            LineWidths = lineWidths;
            DrawingOffsets = drawingOffsets;
            this.deleted = deleted;
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
            
            if (!alteredPrevState.deleted)
            {
                gameObject.SetActive(true);
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    if (gameObject.transform.GetChild(i).GetComponent<PersistentID>() != null)
                    {
                        var persistentChildID = gameObject.transform.GetChild(i).GetComponent<PersistentID>();
                        if (persistentChildID == null)
                        {
                            persistentChildID = gameObject.transform.GetChild(i).GetComponent<IdHolderInformation>().GetIDHolder().GetComponentInParent<PersistentID>();
                        }
                        ObjectBaseState childBaseState = PlayModeManager.Instance.GetObjectTypeForPersistentID(persistentChildID);
                        if(childBaseState != null)
                            childBaseState.deleted = false;
                    }
                }
            }
            gameObject.transform.position = alteredPrevState.position;
            gameObject.transform.rotation = alteredPrevState.rotation;
            gameObject.transform.localScale = alteredPrevState.scale;
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
            
            if (alteredNextState.deleted)
            {
                gameObject.SetActive(false);
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    if (gameObject.transform.GetChild(i).GetComponent<PersistentID>() != null)
                    {
                        var persistentChildID = gameObject.transform.GetChild(i).GetComponent<PersistentID>();
                        if (persistentChildID == null)
                        {
                            persistentChildID = gameObject.transform.GetChild(i).GetComponent<IdHolderInformation>().GetIDHolder().GetComponentInParent<PersistentID>();
                        }
                        ObjectBaseState childBaseState = PlayModeManager.Instance.GetObjectTypeForPersistentID(persistentChildID);
                        if(childBaseState != null)
                            childBaseState.deleted = true;
                    }
                }
            }
            gameObject.transform.position = nextState.position;
            gameObject.transform.rotation = nextState.rotation;
            gameObject.transform.localScale = nextState.scale;
            nextState.prevState = this;
            return nextState;
        }
    }
}
