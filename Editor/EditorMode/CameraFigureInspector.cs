using UnityEditor;
using UnityEngine;

namespace VRGreyboxing
{
    
    /**
     * Add button to inspector ui of CameraFigure component
     */
    [CustomEditor(typeof(CameraFigure))]
    public class CameraFigureInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Show Camera Path"))
            {
                EditorManager.StartCameraMovement((CameraFigure)target);
            }
        }

    }
}
