using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class Rope : MonoBehaviour
{
    public SpringJoint segmentPrefab;
    public GameObject segmentsParent;
    public Rigidbody anchorPoint;

    public float reelInSpeed = 0.1f;
    public float reelInDistance = 0.1f;
    public float reelOutDistance = 0.1f;

    public BarIndicator stringLengthIndicator;
    public BarIndicator stringTensionIndicator;

    public int maximumSegments = 500;
    public float BreakForce => segmentPrefab.breakForce;

    private Vector3 lastReelOutPosition;
    private List<SpringJoint> segments = new List<SpringJoint>();

    public Material realisticMaterial;
    public Material unrealisticMaterial;

    [ContextMenu("Activate Realistic Rope")]
    public void ActivateRealisticRope()
    {
        Rigidbody prefabRb = segmentPrefab.GetComponent<Rigidbody>();
        prefabRb.useGravity = true;

        MeshRenderer prefabRenderer = segmentPrefab.gameObject.GetFirstChildOnLayer<MeshRenderer>(LayerMask.GetMask("Rope"));
        prefabRenderer.sharedMaterial = realisticMaterial;

        foreach (SpringJoint segment in segments)
        {
            Rigidbody rb = segment.GetComponent<Rigidbody>();
            rb.useGravity = true;
            //MeshRenderer renderer = segment.gameObject.GetFirstChildOnLayer<MeshRenderer>(LayerMask.GetMask("Rope"));
            //renderer.sharedMaterial = realisticMaterial;
        }
    }

    [ContextMenu("Activate Unrealistic Rope")]
    public void ActivateUnrealisticRope()
    {
        Rigidbody prefabRb = segmentPrefab.GetComponent<Rigidbody>();
        prefabRb.useGravity = false;
        MeshRenderer prefabRenderer = segmentPrefab.gameObject.GetFirstChildOnLayer<MeshRenderer>(LayerMask.GetMask("Rope"));
        prefabRenderer.sharedMaterial = unrealisticMaterial;

        foreach (SpringJoint segment in segments)
        {
            Rigidbody rb = segment.GetComponent<Rigidbody>();
            rb.useGravity = false;
            //MeshRenderer renderer = segment.gameObject.GetFirstChildOnLayer<MeshRenderer>(LayerMask.GetMask("Rope"));
            //renderer.sharedMaterial = unrealisticMaterial;
        }
    }

    public void UpdateBarIndicators()
    {
        stringLengthIndicator.SetPercentage(1 - SegmentsPercentage);
        stringTensionIndicator.SetPercentage(ForcePercentage);
    }

    public void ConnectStartTo(Vector3 anchor)
    {
        if (segments.Count <= 0)
            ReelOut();

        SpringJoint start = segments[0];
        start.transform.position = anchor;
        Rigidbody rb = start.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezePosition;
    }

    public bool CheckForBreaks()
    {
        int length = segments.Count;
        bool isBroken = false;
        for (int i = length - 1; i>= 0; i--)
        {
            if (isBroken || segments[i] == null)
            {
                isBroken = true;
                segments.RemoveAt(i);
            }
        }

        return isBroken;
    }

    public void ReelOut()
    {
        if (segments.Count >= maximumSegments)
            return;

        SpringJoint newJoint = Instantiate(segmentPrefab, anchorPoint.transform.position, anchorPoint.transform.rotation, segmentsParent.transform);

        if (segments.Count > 0)
            segments[segments.Count - 1].connectedBody = newJoint.GetComponent<Rigidbody>();

        newJoint.connectedBody = anchorPoint;

        segments.Add(newJoint);
    }


    public void DestroyRope()
    {
        List<SpringJoint> toDestroy = new List<SpringJoint>(segments);
        for (int i = toDestroy.Count - 1; i >= 0; i--)
            Destroy(toDestroy[i].gameObject);
        segments.Clear();

        if (segmentsParent == null)
            return;
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in segmentsParent.transform)
            children.Add(child.gameObject);

        for (int i = children.Count - 1; i >= 0; i--)
            Destroy(children[i]);
    }

    /// <summary>
    /// Returns the largest amount of tension between two balls.
    /// This is the force that will cause the rope to break.
    /// </summary>
    /// <returns></returns>
    public float MaximumForce
    {
        get
        {
            float maxForce = 0;
            for (int i = 0; i < segments.Count - 1; i++)
            {
                SpringJoint segment = segments[i];
                float force = segment.currentForce.magnitude;
                maxForce = Mathf.Max(maxForce, force);
            }
            return maxForce;
        }
    }

    public float ForcePercentage => MaximumForce / BreakForce;

    public float AverageForce => TotalForce / segments.Count;

    public int Count => segments.Count;
    public float SegmentsPercentage => segments.Count / (float)maximumSegments;

    public float TotalForce
    {
        get 
        {
            float totalForce = 0;
            for (int i = 0; i < segments.Count - 1; i++)
            {
                SpringJoint segment = segments[i];
                float force = segment.currentForce.magnitude;
                totalForce += force;
            }
            return totalForce;
        }
    }

    public void ReelIn()
    {
        // if the rope has no segments, return
        if (segments.Count == 0)
            return;

        SpringJoint segment = segments[segments.Count - 1];

        // if the nearest segment is close enough to the player, destroy it.
        float distance = Vector3.Distance(segment.transform.position, transform.position);
        if (distance < reelInDistance)
        {
            segments.RemoveAt(segments.Count - 1);
            Destroy(segment.gameObject);
            // if there is still a segment left, set the connected body of the last segment to the player's rigidbody
            if (segments.Count > 0)
                segments[segments.Count - 1].connectedBody = anchorPoint;
        }
        else // move the last segment towards the player carefully, so it doesn't break
        {
            // Get the last segment and its rigidbody
            Rigidbody segmentRigidbody = segment.GetComponent<Rigidbody>();
            
            // Calculate the movement vector between the player and the segment
            Vector3 delta = anchorPoint.transform.position - segment.transform.position;
            float magnitude = delta.magnitude;
            Vector3 movement = delta / magnitude * reelInSpeed * Time.deltaTime;
            movement = Vector3.ClampMagnitude(movement, magnitude);
            
            // apply the movement to the segment and reduce its velocity
            segmentRigidbody.MovePosition(segment.transform.position + movement);
//            segmentRigidbody.velocity *= 0.1f;
        }
    }

    void Start()
    {
        if (anchorPoint == null)
            throw new System.Exception("Anchor point is not set!");
        lastReelOutPosition = anchorPoint.transform.position;
    }


    void LateUpdate()
    {
        bool broken = CheckForBreaks();
        if (broken)
        {
            GameManager uim = FindFirstObjectByType<GameManager>();
            uim.ShowDeathScreen();
            return;    
        }
        UpdateBarIndicators();

        foreach (SpringJoint segment in segments)
        {   
            Vector3 clampedY = segment.transform.position;
            clampedY.y = Mathf.Clamp(clampedY.y, 0, 1);
            segment.transform.position = clampedY;

            BoxCollider box = segment.gameObject.GetComponentInChildren<BoxCollider>();

            Vector3 position = segment.transform.position;
            Vector3 connectedPosition = segment.connectedBody.transform.position;
            float distance = Vector3.Distance(position, connectedPosition);

            //Vector3 center = collider.center;
            //Vector3 size = collider.size;



            //center.z = distance / 2;
            //size.z = distance;

            //collider.center = center;
            //collider.size = size;

            Vector3 childScale = box.transform.localScale;
            childScale.z = distance;

            Vector3 childPosition = (position + connectedPosition) / 2;

            box.transform.position = childPosition;
            box.transform.localScale = childScale;
            box.transform.LookAt(connectedPosition);

        }

    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            debug = !debug;

        if (Input.GetKey(KeyCode.R))
        {
            ReelIn();
        }
        else
        {
            Vector3 reelOutPosition = anchorPoint.transform.position;

            // check if suitable position
            
            if (reelOutPosition.y >= 0 && reelOutPosition.y <= 1)
            {
                float distance = Vector3.Distance(lastReelOutPosition, reelOutPosition);
                if (distance > reelOutDistance)
                {
                    ReelOut();
                    lastReelOutPosition = reelOutPosition;
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(lastReelOutPosition, 0.01f);
        Gizmos.DrawLine(lastReelOutPosition, anchorPoint.transform.position);

        Gizmos.color = Color.white;
        for (int i = 0; i < segments.Count - 1; i++)
            Gizmos.DrawLine(segments[i].transform.position, segments[i + 1].transform.position);
    



// if the rope has no segments, return
        if (segments.Count == 0)
            return;

        // if the nearest segment is close enough to the player, destroy it.
        float distance = Vector3.Distance(segments[segments.Count - 1].transform.position, anchorPoint.transform.position);
        if (distance < reelInDistance)
        {

        }
        else // move the last segment towards the player carefully, so it doesn't break
        {
            // Get the last segment and its rigidbody
            SpringJoint segment = segments[segments.Count - 1];
            Rigidbody segmentRigidbody = segment.GetComponent<Rigidbody>();
            
            // Calculate the movement vector between the player and the segment
            Vector3 delta = anchorPoint.transform.position - segment.transform.position;
            float magnitude = delta.magnitude;
            Vector3 movement = delta.normalized * reelInSpeed * Time.deltaTime;
            //movement = Vector3.ClampMagnitude(movement, magnitude);
            
            Gizmos.color = Color.red;
            //aGizmos.DrawLine(segment.transform.position, segment.transform.position + delta);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(segment.transform.position, segment.transform.position + movement);
        }



    }

    public bool debug;

    private void OnGUI()
    {


        if (!debug)
            return;
        GUILayout.BeginArea(new Rect(10, 10, 200, 200));
        GUILayout.Label("Debug Overlay");
        GUILayout.Label("Press 'F1' to toggle");
        

        string line_0 = $"Segments: {segments.Count}";
        string line_1 = $"Max Force: { MaximumForce }";
        string line_2 = $"Total Force: { TotalForce }";
        string line_3 = $"Average Force: { AverageForce }";

        GUILayout.Label(line_0);
        GUILayout.Label(line_1);
        GUILayout.Label(line_2);
        GUILayout.Label(line_3);


        
        GUILayout.EndArea();
    }
}
