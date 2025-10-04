using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace VRGreyboxing
{
    public class AlteredObject : ObjectBaseState
    {

        public List<CameraKeyFrame> keyFrames;
        
        public AlteredObject(GameObject gameObject, string persisentID, Vector3 position, Quaternion rotation, Vector3 scale,bool deleted,string originalScenePath, List<Vector3> alteredPositions) : base(gameObject, persisentID, position, rotation, scale,originalScenePath,alteredPositions)
        {
            base.deleted = deleted;
        }

        public override ObjectBaseState UndoChange()
        {
            AlteredObject alteredPrevState = prevState as AlteredObject;
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
