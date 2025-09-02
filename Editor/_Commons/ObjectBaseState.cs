using System.Collections.Generic;
using UnityEngine;

namespace VRGreyboxing
{
    
    /**
     * Base class for saving object changes during greyboxing
     */
    [System.Serializable]
    public class ObjectBaseState
    {
        public GameObject gameObject;
        public string persisentID;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public bool Deleted;
        public bool Untouched;
        public bool justCreated;
        public bool disabled;
        public string newParentID;
        public List<Vector3> alteredPositions;
        public ObjectBaseState prevState;
        public ObjectBaseState nextState;

        protected ObjectBaseState(GameObject gameObject, string persisentID, Vector3 position, Quaternion rotation, Vector3 scale, List<Vector3> alteredPositions)
        {
            this.persisentID = persisentID;
            this.Position = position;
            this.Rotation = rotation;
            Scale = scale;
            this.alteredPositions = alteredPositions;
            Deleted = false;
            Untouched = false;
            disabled = false;
            this.gameObject = gameObject;
        }
        
        /**
         * Set state of object to saved previous state to undo last changes
         */
        public virtual ObjectBaseState UndoChange()
        {
            return null;
        }

        /**
         * Set state of object to saved next state to redo already made changes
         */
        public virtual ObjectBaseState RedoChange()
        {
            return null;
        }

        /**
         * Delete object indefinitely when object was removed and new changes were performed
         */
        public virtual void RemoveChange()
        {
            ObjectBaseState next = this;
            do
            {
                if (next.Deleted)
                {
                    Object.Destroy(gameObject);
                    return;
                }
                next = next.nextState;
            }while (next != this && next != null);
        }
    }
}
