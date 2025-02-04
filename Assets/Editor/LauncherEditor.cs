using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Launcher))]
public class LauncherEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Launcher launcher = (Launcher)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Launch"))
            launcher.Launch();
    }
}
