using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

namespace VRGreyboxing
{
    public class SpawnedObject : ObjectBaseState
    {
        public int prefabIndex;
        public List<CameraKeyFrame> keyFrames;
        public string basePersistentID;
        public string OriginalScenePath;

        public SpawnedObject(GameObject gameObject, string persistentId,Vector3 position, Quaternion rotation, Vector3 scale,List<Vector3> positions, int prefabIndex, string scene,string basePersistentID) : base(gameObject,persistentId,position, rotation, scale,positions)
        {
            this.prefabIndex = prefabIndex;
            this.OriginalScenePath = scene;
            this.basePersistentID = basePersistentID;
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
            if (!spawnedPrevObject.Deleted)
            {
                gameObject.SetActive(true);
            }
            gameObject.transform.position = spawnedPrevObject.Position;
            gameObject.transform.rotation = spawnedPrevObject.Rotation;
            gameObject.transform.localScale = spawnedPrevObject.Scale;
            if (spawnedPrevObject.alteredPositions.Count > 0)
            {
                ProBuilderMesh pbm = gameObject.GetComponent<ProBuilderMesh>();
                pbm.positions = spawnedPrevObject.alteredPositions.ToArray();
                pbm.ToMesh();
                pbm.Refresh();
            }
            keyFrames = spawnedPrevObject.keyFrames;
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
            
            if (spawnedNextState.Deleted)
            {
                gameObject.SetActive(false);
            }
            gameObject.transform.position = spawnedNextState.Position;
            gameObject.transform.rotation = spawnedNextState.Rotation;
            gameObject.transform.localScale = spawnedNextState.Scale;
            if (spawnedNextState.alteredPositions.Count > 0)
            {
                ProBuilderMesh pbm = gameObject.GetComponent<ProBuilderMesh>();
                pbm.positions = spawnedNextState.alteredPositions.ToArray();
                pbm.ToMesh();
                pbm.Refresh();
            }
            keyFrames = spawnedNextState.keyFrames;
            nextState.prevState = this;
            return base.RedoChange();
        }
    }
}
