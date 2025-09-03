using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRGreyboxing
{
    /**
     * Costum ui window for greyboxing configuration
     */
    public class EditorWindow : UnityEditor.EditorWindow
    {
        
        [SerializeField] private List<DefaultAsset> prefabSourceDirectories = new List<DefaultAsset>();

        private SerializedObject editorData;
        private SerializedProperty foldersProperty;
        
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
            EditorGUILayout.Space(15f);
            
            
            EditorManager.editorDataSo.prefabSaveDirectory = EditorGUILayout.ObjectField("Prefab Save directory", EditorManager.editorDataSo.prefabSaveDirectory, typeof(Object), false);
            if (editorData == null || editorData.targetObject !=  EditorManager.editorDataSo)
            {
                editorData = new SerializedObject(EditorManager.editorDataSo);
                foldersProperty = editorData.FindProperty("prefabSourceDirectories");
            }
            editorData.Update();

            EditorGUILayout.PropertyField(foldersProperty, true);
            editorData.ApplyModifiedProperties();
            EditorManager.editorDataSo.prefabSourceDirectories = prefabSourceDirectories;
            EditorGUILayout.Space(15f);

            if (GUILayout.Button("Display Config File"))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>("Packages/com.bcatstudio.vrgreyboxing/EditorData.asset");
                if (obj != null)
                {
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }
        }
    }
}
