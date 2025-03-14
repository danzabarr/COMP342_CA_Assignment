using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class ControllerInput : MonoBehaviour
{
    public float speed = 3.0f;
    public float rotationSpeed = 3.0f;

    private CharacterController controller;
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        float vertical = Input.GetAxis("Vertical") * speed;
        float horizontal = Input.GetAxis("Horizontal") * speed;
        Vector3 move = new Vector3(horizontal, 0, vertical);
        move = Vector3.ClampMagnitude(move, 1);
        if (vertical < 0)
        {
            move = new Vector3(horizontal, 0, vertical) /( 1-vertical);
        }
        move = transform.TransformDirection(move);

        animator.SetFloat("runSpeed", move.magnitude);
        controller.Move(move * Time.deltaTime);

        animator.SetFloat("vertical", vertical);
        animator.SetFloat("horizontal", horizontal);

        // rotate the player to face the direction of movement
        if (move != Vector3.zero)
        {
            if (vertical < 0)
            {
                move *= -1;
            }
            //transform.rotation = Quaternion.LookRotation(move); 
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), Time.deltaTime * rotationSpeed);
        }

    }
}
