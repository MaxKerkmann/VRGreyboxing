using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace VRGreyboxing
{
    public class AlteredObject : ObjectBaseState
    {

        public List<CameraKeyFrame> keyFrames;
        
        public AlteredObject(GameObject gameObject, string persisentID, Vector3 position, Quaternion rotation, Vector3 scale,bool deleted, List<Vector3> alteredPositions) : base(gameObject, persisentID, position, rotation, scale,alteredPositions)
        {
            Deleted = deleted;
        }

        public override ObjectBaseState UndoChange()
        {
            AlteredObject alteredPrevState = prevState as AlteredObject;
            if (!alteredPrevState.Deleted)
            {
                gameObject.SetActive(true);
            }
            gameObject.transform.position = alteredPrevState.Position;
            gameObject.transform.rotation = alteredPrevState.Rotation;
            gameObject.transform.localScale = alteredPrevState.Scale;
            if (alteredPrevState.alteredPositions.Count > 0)
            {
                ProBuilderMesh pbm = gameObject.GetComponent<ProBuilderMesh>();
                pbm.positions = alteredPrevState.alteredPositions.ToArray();
                pbm.ToMesh();
                pbm.Refresh();
            }
            
            keyFrames = alteredPrevState.keyFrames;
            
            alteredPrevState.nextState = this;
            return prevState;
        }

        public override ObjectBaseState RedoChange()
        {
            AlteredObject alteredNextState = nextState as AlteredObject;
            if (alteredNextState.Deleted)
            {
                gameObject.SetActive(false);
            }
            gameObject.transform.position = nextState.Position;
            gameObject.transform.rotation = nextState.Rotation;
            gameObject.transform.localScale = nextState.Scale;
            if (nextState.alteredPositions.Count > 0)
            {
                ProBuilderMesh pbm = gameObject.GetComponent<ProBuilderMesh>();
                pbm.positions = nextState.alteredPositions.ToArray();
                pbm.ToMesh();
                pbm.Refresh();
            }

            keyFrames = alteredNextState.keyFrames;
            nextState.prevState = this;
            return nextState;
        }
    }
}
