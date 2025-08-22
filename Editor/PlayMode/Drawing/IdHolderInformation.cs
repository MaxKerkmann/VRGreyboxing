using UnityEngine;


namespace VRGreyboxing
{
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