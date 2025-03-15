using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DebugOverlay : MonoBehaviour
{

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 200));
        GUILayout.Label("Debug Overlay");
        GUILayout.Label("Press 'F1' to toggle");
        GUILayout.EndArea();
    }
}
