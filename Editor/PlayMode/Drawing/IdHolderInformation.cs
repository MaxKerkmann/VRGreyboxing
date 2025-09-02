using UnityEngine;


namespace VRGreyboxing
{
    /**
     * Information holder in case persistent id does sit on the object directly 
     */
    public class IdHolderInformation : MonoBehaviour
    {
        public Transform iDHolder;
        public int prefabIndex = -1;

        public Transform GetIDHolder()
        {
            if (iDHolder.GetComponent<IdHolderInformation>())
                return iDHolder.GetComponent<IdHolderInformation>().GetIDHolder();
            return iDHolder;
        }
    }
}