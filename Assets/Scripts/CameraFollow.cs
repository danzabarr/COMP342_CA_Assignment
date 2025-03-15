using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Follows a target object, such as the player, and looks at a certain side of it from a set distance and pitch.
/// Moving the mouse to the edges of the screen will rotate the camera but it will snap back to the target when the mouse is in the center of the screen.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    /// <summary>
    /// The distance from the target that the camera will be.
    /// </summary>
    public float distance = 10.0f;
    /// <summary>
    /// The angle around the target that the camera will be.
    /// </summary>
    public float yaw = 0.0f;
    /// <summary>
    /// The angle above the target that the camera will be.
    /// </summary>
    public float pitch = 45.0f;

    public bool avoidObstacles = true;

    public Vector3 offset;

    private Vector2 mouseRotationAdjustment = Vector2.zero;

    public float rotationSpeed = 10.0f;
    public float moveSpeed = 10.0f;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 targetPosition = target.position + target.localToWorldMatrix.MultiplyVector(offset);

// Get the forward direction of the target
Vector3 forwardDirection = target.forward;

// Apply yaw rotation around the target's up axis
Quaternion yawRotation = Quaternion.AngleAxis(yaw, target.up);
Vector3 rotatedDirection = yawRotation * forwardDirection;

// Apply pitch separately
Quaternion pitchRotation = Quaternion.AngleAxis(pitch, target.right);
Vector3 finalDirection = pitchRotation * rotatedDirection;

// Set the camera position at the correct distance
Vector3 desiredPosition = targetPosition - finalDirection.normalized * distance;

if (avoidObstacles && Physics.SphereCast
(
    targetPosition,
    0.1f,
    desiredPosition - targetPosition,
    out RaycastHit hit,
    distance
))
{
    desiredPosition = hit.point;
}

transform.position = desiredPosition;
transform.LookAt(targetPosition);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 targetPosition = target.position + target.localToWorldMatrix.MultiplyVector(offset);

        // Get the forward direction of the target
        Vector3 forwardDirection = target.forward;

        // Apply yaw rotation around the target's up axis
        Quaternion yawRotation = Quaternion.AngleAxis(yaw, target.up);
        Vector3 rotatedDirection = yawRotation * forwardDirection;

        // Apply pitch separately
        Quaternion pitchRotation = Quaternion.AngleAxis(pitch, target.right);
        Vector3 finalDirection = pitchRotation * rotatedDirection;

        // Set the camera position at the correct distance
        Vector3 desiredPosition = targetPosition - finalDirection.normalized * distance;

        if (avoidObstacles && Physics.SphereCast
        (
            targetPosition,
            0.1f,
            desiredPosition - targetPosition,
            out RaycastHit hit,
            distance
        ))
        {
            desiredPosition = hit.point;
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * moveSpeed);

        // Make the camera look at the target
        //transform.LookAt(targetPosition);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetPosition - transform.position), Time.deltaTime * rotationSpeed);



        // now adjust the mouseRotationAdjustment based on the mouse input
        float mouseX = Input.mousePosition.x / Screen.width - 0.5f;
        float mouseY = Input.mousePosition.y / Screen.height - 0.5f;

        // if the mouse is in the center of the screen, reset the adjustment
        if (Mathf.Abs(mouseX) < 0.1f)
        {
            mouseRotationAdjustment.x = 0;
        }

        if (Mathf.Abs(mouseY) < 0.1f)
        {
            mouseRotationAdjustment.y = 0;
        }

        // adjust the adjustment
        mouseRotationAdjustment.x = mouseX * 10;
        mouseRotationAdjustment.y = mouseY * 10;

        // adjust the transform
        //transform.Rotate(Vector3.right, -mouseRotationAdjustment.y);
        //transform.Rotate(Vector3.up, mouseRotationAdjustment.x);
    }

    private void DontOnDrawGizmos()
    {
Vector3 targetPosition = target.position + target.localToWorldMatrix.MultiplyVector(offset);

// Get the forward direction of the target
Vector3 forwardDirection = target.forward;

// Apply yaw rotation around the target's up axis
Quaternion yawRotation = Quaternion.AngleAxis(yaw, target.up);
Vector3 rotatedDirection = yawRotation * forwardDirection;

// Apply pitch separately
Quaternion pitchRotation = Quaternion.AngleAxis(pitch, target.right);
Vector3 finalDirection = pitchRotation * rotatedDirection;

// Set the camera position at the correct distance
Vector3 desiredPosition = targetPosition - finalDirection.normalized * distance;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(targetPosition, desiredPosition);
        Gizmos.DrawSphere(desiredPosition, 0.1f);
        Gizmos.DrawSphere(targetPosition, 0.1f);
    }
}
