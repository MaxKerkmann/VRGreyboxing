using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace VRGreyboxing
{
    public class CreatedObject : ObjectBaseState
    {
        public List<Vector3> basePositions;
        public string OriginalScenePath;
        public bool flippedVertices;
        

        public CreatedObject(GameObject gameObject, string persisentID, Vector3 position, Quaternion rotation, Vector3 scale, List<Vector3> basePositions,List<Vector3> positions,bool flippedVertices, string originalScenePath, bool justCreated) : base(gameObject, persisentID, position, rotation, scale,positions)
        {
            this.basePositions = basePositions;
            OriginalScenePath = originalScenePath;
            this.justCreated = justCreated;
            this.flippedVertices = flippedVertices;
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
            if (!createdPrevState.Deleted)
            {
                gameObject.SetActive(true);
            }
            gameObject.transform.position = createdPrevState.Position;
            gameObject.transform.rotation = createdPrevState.Rotation;
            gameObject.transform.localScale = createdPrevState.Scale;
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
            
            if (createdNextState.Deleted)
            {
                gameObject.SetActive(false);
            }
            gameObject.transform.position = createdNextState.Position;
            gameObject.transform.rotation = createdNextState.Rotation;
            gameObject.transform.localScale = createdNextState.Scale;
            if (createdNextState.alteredPositions.Count > 0)
            {
                ProBuilderMesh pbm = gameObject.GetComponent<ProBuilderMesh>();
                pbm.positions = createdNextState.alteredPositions.ToArray();
                pbm.ToMesh();
                pbm.Refresh();
            }
            nextState.prevState = this;
            return base.RedoChange();
        }
    }
}