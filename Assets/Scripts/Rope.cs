using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Rope : MonoBehaviour
{
    public SpringJoint ropePrefab;

    private List<SpringJoint> segments = new List<SpringJoint>();

    public void AddSegment()
    {
        SpringJoint newJoint = Instantiate(ropePrefab, transform.position, Quaternion.identity);

        if (segments.Count > 0)
            segments[segments.Count - 1].connectedBody = newJoint.GetComponent<Rigidbody>();
        newJoint.connectedBody = GetComponent<Rigidbody>();

        segments.Add(newJoint);
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddSegment();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        for (int i = 0; i < segments.Count - 1; i++)
            Gizmos.DrawLine(segments[i].transform.position, segments[i + 1].transform.position);
    }
}
