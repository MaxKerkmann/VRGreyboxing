using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace VRGreyboxing
{
    public class SpawnedObject : ObjectBaseState
    {
        public int PrefabIndex;
        public List<CameraKeyFrame> KeyFrames;
        public string BasePersistentID;

        public SpawnedObject(GameObject gameObject, string persistentId,Vector3 position, Quaternion rotation, Vector3 scale,bool deleted,List<Vector3> positions, int prefabIndex, string originalScenePath,string basePersistentID) : base(gameObject,persistentId,position, rotation, scale,originalScenePath,positions)
        {
            this.PrefabIndex = prefabIndex;
            this.originalScenePath = originalScenePath;
            this.BasePersistentID = basePersistentID;
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
            
            SpawnedObject spawnedPrevObject = prevState as SpawnedObject;
            if (!spawnedPrevObject.deleted)
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
            gameObject.transform.position = spawnedPrevObject.position;
            gameObject.transform.rotation = spawnedPrevObject.rotation;
            gameObject.transform.localScale = spawnedPrevObject.scale;
            if (spawnedPrevObject.alteredPositions.Count > 0)
            {
                ProBuilderMesh pbm = gameObject.GetComponent<ProBuilderMesh>();
                pbm.positions = spawnedPrevObject.alteredPositions.ToArray();
                pbm.ToMesh();
                pbm.Refresh();
            }
            KeyFrames = spawnedPrevObject.KeyFrames;
            spawnedPrevObject.nextState = this;
            return prevState;
        }

        public override ObjectBaseState RedoChange()
        {
            SpawnedObject spawnedNextState = nextState as SpawnedObject;
            
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                justCreated = true;
                disabled = false;

                return this;
            }
            
            if (spawnedNextState.deleted)
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
            gameObject.transform.position = spawnedNextState.position;
            gameObject.transform.rotation = spawnedNextState.rotation;
            gameObject.transform.localScale = spawnedNextState.scale;
            if (spawnedNextState.alteredPositions.Count > 0)
            {
                ProBuilderMesh pbm = gameObject.GetComponent<ProBuilderMesh>();
                pbm.positions = spawnedNextState.alteredPositions.ToArray();
                pbm.ToMesh();
                pbm.Refresh();
            }
            KeyFrames = spawnedNextState.KeyFrames;
            nextState.prevState = this;
            return nextState;
        }
    }
}
