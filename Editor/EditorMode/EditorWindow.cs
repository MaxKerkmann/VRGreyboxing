using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRGreyboxing
{
    public class EditorWindow : UnityEditor.EditorWindow
    {
        
        [SerializeField] private List<DefaultAsset> prefabSourceDirectories = new List<DefaultAsset>();

        private SerializedObject serializedObject;
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
            if (serializedObject == null || serializedObject.targetObject !=  EditorManager.editorDataSo)
            {
                serializedObject = new SerializedObject(EditorManager.editorDataSo);
                foldersProperty = serializedObject.FindProperty("prefabDirectories");
            }
            serializedObject.Update();

            EditorGUILayout.PropertyField(foldersProperty, true);
            serializedObject.ApplyModifiedProperties();
            EditorManager.editorDataSo.prefabDirectories = prefabSourceDirectories;
        }
    }
}
