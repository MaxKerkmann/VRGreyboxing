using System.Collections.Generic;
using UnityEngine;

namespace VRGreyboxing
{
    public class EditWidgetEditPoint : MonoBehaviour
    {
        [HideInInspector] 
        public PlayerEdit playerEdit;
        public GameObject assignedObject;
        public List<List<int>> handledPositionIndices;
    }
}