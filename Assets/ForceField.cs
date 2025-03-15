using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceField : MonoBehaviour
{
    public LayerMask includeLayers;

    public float force = 10.0f;
    public float range = 10.0f;
    public float falloff = 1.0f;

    private void ApplyForce(Rigidbody target)
    {
        Vector3 direction = transform.position - target.transform.position;
        float distance = direction.magnitude;
        float appliedForce = force * (1 - Mathf.Pow(distance / range, falloff));
        target.AddForce(direction.normalized * appliedForce);
    }

    private void FixedUpdate()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, range, includeLayers);
        foreach (Collider collider in colliders)
        {
            Rigidbody target = collider.attachedRigidbody;
            if (target != null)
            {
                ApplyForce(target);
            }
        }
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
