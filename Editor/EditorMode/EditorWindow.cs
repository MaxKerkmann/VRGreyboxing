using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRGreyboxing
{
    /**
     * Costume ui window for greyboxing configuration
     */
    public class EditorWindow : UnityEditor.EditorWindow
    {
        
        [SerializeField] private List<DefaultAsset> prefabSourceDirectories = new List<DefaultAsset>();

        private SerializedObject _editorData;
        private SerializedProperty _foldersProperty;
        
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
            if (_editorData == null || _editorData.targetObject !=  EditorManager.editorDataSo)
            {
                _editorData = new SerializedObject(EditorManager.editorDataSo);
                _foldersProperty = _editorData.FindProperty("prefabSourceDirectories");
            }
            _editorData.Update();

            EditorGUILayout.PropertyField(_foldersProperty, true);
            _editorData.ApplyModifiedProperties();
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
