using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    private SpriteRenderer doorRenderer;
    private GameObject doorCollidingChild;

    public void Start() {
        doorRenderer = GetComponent<SpriteRenderer>();
        doorCollidingChild = transform.GetChild(0).gameObject;
    }

    private void OnTriggerEnter2D(Collider2D c)
    {
        doorRenderer.enabled = false;
        doorCollidingChild.SetActive(false);
    }
    private void OnTriggerExit2D(Collider2D c)
    {
        doorRenderer.enabled = true;
        doorCollidingChild.SetActive(true);
    }
}
