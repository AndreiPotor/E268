using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    public SpriteRenderer sr;
    public GameObject obstacle;

    private void OnTriggerEnter2D(Collider2D c)
    {
        sr.enabled = false;
        obstacle.SetActive(false);
    }
    private void OnTriggerExit2D(Collider2D c)
    {
        sr.enabled = true;
        obstacle.SetActive(true);
    }
}
