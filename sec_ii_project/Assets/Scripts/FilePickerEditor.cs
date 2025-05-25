using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(ReplayTester))] 
public class FilePickerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ReplayTester myObjectScript = (ReplayTester)target;
        GUILayout.Space(10f);
        if (GUILayout.Button("Open File Picker"))
        {
            string path = EditorUtility.OpenFilePanel("Select File", "", "");
            if (!string.IsNullOrEmpty(path))
            {
                myObjectScript.filePath = path;
                GUI.changed = true;
            }
        }
    }
}