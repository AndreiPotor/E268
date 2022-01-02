using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class Legs : MonoBehaviour
{
    public Animator animator;
    public float animationSpeed;

    private Vector2 moveVelocity = Vector2.zero;
    private float AngleRad, AngleDeg;

    public void setVelocity(Vector2 velocity) {
        moveVelocity = velocity;
    }

    // Update is called once per frame
    void Update()
    {

        // setting variable to start/stop the moving animation
        if (moveVelocity.magnitude > 0.1f)
        {
            animator.SetBool("walking", true);
            animator.speed = moveVelocity.magnitude * animationSpeed / 100f;
            // Rotating the legs according to the movement
            AngleRad = Mathf.Atan2(moveVelocity.y, moveVelocity.x);
            AngleDeg = (180 / Mathf.PI) * AngleRad - 90f;
            transform.rotation = Quaternion.Euler(0, 0, AngleDeg);
        }
        else
            animator.SetBool("walking", false);
    }
}
