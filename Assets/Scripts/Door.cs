using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Door : MonoBehaviour
{
public enum State
{
    Open,
    Closed
}

    private State _state = State.Closed;
    public State state
    {
        get
        {
            return _state;
        }
        set
        {
            _state = value;
            if (_state == State.Open)
            {
                Open();
            }
            else
            {
                Close();
            }
        }
    }

    public float openAngle = 90.0f;
    public float closeAngle = 0.0f;

    public float speed = 90.0f;

    public void Open()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateDoor(openAngle));
    }

    public void Close()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateDoor(closeAngle));
    }

    public void Toggle()
    {
        state = state == State.Open ? State.Closed : State.Open;
    }

    private IEnumerator AnimateDoor(float targetAngle)
    {
        float currentAngle = transform.localEulerAngles.y;
        while (Mathf.Abs(currentAngle - targetAngle) > 0.01f)
        {
            currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, speed * Time.deltaTime);
            transform.localEulerAngles = new Vector3(0, currentAngle, 0);
            yield return null;
        }
    }

    // on click
    private void OnMouseDown()
    {
        Toggle();
    }
}
