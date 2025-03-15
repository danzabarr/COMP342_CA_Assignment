using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(SpringJoint), typeof(Rigidbody))]
public class DebugSpring : MonoBehaviour
{

    private SpringJoint springJoint;
    private Rigidbody rb;

    public Rigidbody ConnectedBody
    {
        get
        {
            if (springJoint == null)
                springJoint = GetComponent<SpringJoint>();
            return springJoint.connectedBody;
        }
        set
        {
            springJoint.connectedBody = value;
        }
    }

    public float Distance => Vector3.Distance(transform.position, ConnectedBody.transform.position);

    public float Force 
    {
        get { 
            if (springJoint == null)
                springJoint = GetComponent<SpringJoint>(); 
            return springJoint.currentForce.magnitude;
        }
    }

    public float BreakForce
    {
        get 
        {
            if (springJoint == null)
                springJoint = GetComponent<SpringJoint>();
            return springJoint.breakForce;
        }
    }

    public float ForceProportion => Force / BreakForce;

    // Start is called before the first frame update
    void Start()
    {
        springJoint = GetComponent<SpringJoint>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log($"Distance: {Distance}" +
                  $"\nForce: {Force}" +
                  $"\nBreakForce: {BreakForce}" +
                  $"\nForceProportion: {ForceProportion}");
    }

    public void OnDrawGizmos()
    {
        float breakForceProportion = ForceProportion;
        Color color = Color.Lerp(Color.green, Color.red, breakForceProportion);
        Gizmos.DrawLine(transform.position, ConnectedBody.transform.position);
    }
}
