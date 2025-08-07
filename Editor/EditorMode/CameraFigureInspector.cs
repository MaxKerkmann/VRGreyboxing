using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace VRGreyboxing
{
    [CustomEditor(typeof(CameraFigure))]

    public class CameraFigureInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Show Camera Path"))
            {
                EditorManager.StartCameraPath((CameraFigure)target);
            }
        }

    }
}
