using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spark : MonoBehaviour
{
    private Rigidbody2D rb;
    public float scale = 0.2f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.localScale = rb.velocity.magnitude * new Vector3(1,1,1) * scale;
        if (rb.velocity.magnitude <= 2.5f)
            Destroy(gameObject);
    }
}
