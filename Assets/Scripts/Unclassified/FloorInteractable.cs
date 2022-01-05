using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorInteractable : MonoBehaviour
{
    private Animator animator;

    public void Start() {
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D c) {
        animator.SetTrigger("Entered");
    }
    private void OnTriggerExit2D(Collider2D c) {
        animator.SetTrigger("Left");
    }
}
