using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeSegment : MonoBehaviour
{

    public SpringJoint joint;
    public Rigidbody rigidBody;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void SetMaterial(Material material)
    {
        meshRenderer.material = material;
    }

    public void SetMesh(Mesh mesh)
    {
        meshFilter.mesh = mesh;
    }
    



}
