using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UIElements;

public enum Shot 
{
    Straight,
    Lob
}

public enum Mode
{
    FixedPitch,
    FixedVelocity
}

[ExecuteAlways]
public class Launcher : MonoBehaviour
{
    public Rigidbody projectilePrefab;
    public Transform target;
    public Mode mode;
    public Shot shot;
    public float velocity;
    public float drawGizmosTrajectoryDuration = 1000;
    private bool withinRange;

    public void Launch()
    {
        if (!withinRange)
        {
            Debug.LogWarning("Target out of range");
            return;
        }
        Rigidbody projectile = Instantiate(projectilePrefab, transform.position, transform.rotation);
        projectile.velocity = transform.forward * velocity;
    }

    public void OnDrawGizmos()
    {
        if (withinRange)
            Gizmos.color = Color.green;   
        else
            Gizmos.color = Color.red;

        float increment = 0.1f;
        Vector3 last = transform.position;
        for (float t = 0; t < drawGizmosTrajectoryDuration; t += increment)
        {
            Vector3 position = transform.position + transform.forward * velocity * t + Physics.gravity * t * t / 2;
            Gizmos.DrawLine(last, position);
            last = position;
        }
    }

    public void Update()
    {
        float gravity = -Physics.gravity.y;
        Vector3 position = transform.position;
        Vector3 target = this.target.position;
        
        switch (mode)
        {
            case Mode.FixedVelocity:
            {
                // given a launch velocity, calculate the launch angle
                int slns = Ballistics.SolveArcDirection(position, velocity, target, gravity, out Vector3 s0, out Vector3 s1);
                if (slns == 0)
                {
                    withinRange = false;
                    return;
                }

                transform.LookAt(target);
                Vector3 force  = slns == 1 || shot == Shot.Straight ? s0 : s1;
                transform.forward = force.normalized;
                withinRange = true;
                break;
            }

            case Mode.FixedPitch:
            {
                float pitch = Vector3.Angle(transform.forward, new Vector3(transform.forward.x, 0, transform.forward.z));

                // given a launch angle, calculate the launch velocity
                if (Ballistics.SolveArcPitch(position, target, pitch, out Vector3 force))
                {
                    transform.LookAt(target);
                    transform.eulerAngles = new Vector3(pitch, transform.eulerAngles.y, transform.eulerAngles.z);
                    transform.forward = force.normalized;
                    velocity = force.magnitude;
                    withinRange = true;
                }
                else
                    withinRange = false;
                    
                break;
            }
        }
    }
}
