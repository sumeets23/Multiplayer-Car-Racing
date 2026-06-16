using UnityEngine;
using UnityEditor;

namespace Car.WheelsManagement
{
    [CustomEditor(typeof(WheelColliderAutoSetup))]
    public class WheelColliderAutoSetupEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            WheelColliderAutoSetup script = (WheelColliderAutoSetup)target;

            GUILayout.Space(20);
            if (GUILayout.Button("Setup Wheels from Meshes", GUILayout.Height(40)))
            {
                Undo.RecordObject(script, "Auto Setup Wheels");
                script.SetupWheels();
                
                // Allow user to see the updates in inspector
                EditorUtility.SetDirty(script);
                
                // If we created objects, we want them to show in hierarchy
                EditorApplication.RepaintHierarchyWindow();
            }
        }
    }
}
