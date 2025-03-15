using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class NavigationNode
{
    public Vector3 position;
    public List<NavigationNode> edges = new List<NavigationNode>();
    public List<float> costs = new List<float>();
}

[System.Serializable]
public class NavigationGraph 
{
    public List<NavigationNode> nodes = new List<NavigationNode>();
    
}
