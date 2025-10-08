using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace VRGreyboxing
{
    public class CreatedObject : ObjectBaseState
    {
        public List<Vector3> BasePositions;
        public bool FlippedVertices;
        

        public CreatedObject(GameObject gameObject, string persisentID, Vector3 position, Quaternion rotation, Vector3 scale,bool deleted, List<Vector3> basePositions,List<Vector3> positions,bool flippedVertices, string originalScenePath, bool justCreated) : base(gameObject, persisentID, position, rotation, scale,originalScenePath,positions)
        {
            BasePositions = basePositions;
            this.originalScenePath = originalScenePath;
            this.justCreated = justCreated;
            FlippedVertices = flippedVertices;
            this.deleted = deleted;
        }
        
        public override ObjectBaseState UndoChange()
        {
            if (justCreated)
            {
                gameObject.SetActive(false);
                justCreated = false;
                disabled = true;
                return this;
            }
            
            CreatedObject createdPrevState = prevState as CreatedObject;
            if (!createdPrevState.deleted)
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
            gameObject.transform.position = createdPrevState.position;
            gameObject.transform.rotation = createdPrevState.rotation;
            gameObject.transform.localScale = createdPrevState.scale;
            if (createdPrevState.alteredPositions.Count > 0)
            {
                ProBuilderMesh pbm = gameObject.GetComponent<ProBuilderMesh>();
                pbm.positions = createdPrevState.alteredPositions.ToArray();
                pbm.ToMesh();
                pbm.Refresh();
            }
            createdPrevState.nextState = this;
            return prevState;
        }

        public override ObjectBaseState RedoChange()
        {
            CreatedObject createdNextState = nextState as CreatedObject;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                disabled = false;
                justCreated = true;
                return this;
            }
            
            if (createdNextState.deleted)
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
            gameObject.transform.position = createdNextState.position;
            gameObject.transform.rotation = createdNextState.rotation;
            gameObject.transform.localScale = createdNextState.scale;
            if (createdNextState.alteredPositions.Count > 0)
            {
                ProBuilderMesh pbm = gameObject.GetComponent<ProBuilderMesh>();
                pbm.positions = createdNextState.alteredPositions.ToArray();
                pbm.ToMesh();
                pbm.Refresh();
            }
            nextState.prevState = this;
            return nextState;
        }
    }
}