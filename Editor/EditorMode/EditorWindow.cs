using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRGreyboxing
{
    public class EditorWindow : UnityEditor.EditorWindow
    {
        
        [MenuItem("Tools/Greyboxing Editor")]
        public static void ShowWindow()
        {
            GetWindow<EditorWindow>("Greyboxing Editor");
        }

        private void OnGUI()
        {
            GUILayout.Label("VR Greyboxing Options", EditorStyles.boldLabel);


            
            if (GUILayout.Button("Start Greyboxing"))
            {
                EditorManager.StartGreyboxing();
            }
            
            EditorManager.editorDataSo.usingBuildScenesOnly = GUILayout.Toggle(EditorManager.editorDataSo.usingBuildScenesOnly, "Use Build Scenes Only");
            EditorManager.editorDataSo.prefabSaveDirectory = EditorGUILayout.ObjectField("Save directory", EditorManager.editorDataSo.prefabSaveDirectory, typeof(Object), false);

        }
    }
}
