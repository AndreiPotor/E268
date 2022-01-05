using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public ViewCone viewCone;
    public Shooting shooting;

    public float speed = 50f;
    public float speedModifier = 1f; // for buffs/debuffs

    public float health = 5f; // 0 == dead
    public float healthModifier = 1f; // for buffs/debuffs

    public float damageResistance = 0f; // for buffs/debuffs (0 < damageResistance <= 1)

    public Camera followingCamera;
    public Legs legsObject;

    private Rigidbody2D rb;

    private Vector2 moveVelocity = Vector2.zero;
    private Vector2 moveInput;
    private Vector3 mousePos;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // getting inputs for moving
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // Rotating the cannon following the mouse position
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float AngleRad = Mathf.Atan2(mousePos.y - transform.position.y, mousePos.x - transform.position.x);
        float AngleDeg = (180 / Mathf.PI) * AngleRad - 90f;
        Quaternion prevRotation = legsObject.transform.rotation;
        transform.rotation = Quaternion.Euler(0, 0, AngleDeg);

        // making sure the legs are not rotated with the body
        legsObject.transform.rotation = prevRotation;

        // sending the velocity to the legs GFX so they can animate
        legsObject.setVelocity(rb.velocity);

        // sending commands to the shooting script
        if (Input.GetMouseButton(0))
            shooting.startShoot(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        else
            shooting.stopShoot();

        // painting the player on the minimap
        UI.paintMinimap((int)Mathf.Round(transform.position.x), (int)Mathf.Round(transform.position.y), "Player");

        // putting camera to follow
        followingCamera.transform.position = new Vector3(rb.position.x, rb.position.y, followingCamera.transform.position.z);
    }

    public Vector3 getMousePos()
    {
        return mousePos;
    }

    private void FixedUpdate()
    {
        // processing movement
        moveVelocity = moveInput.normalized * speed;
        rb.AddForce(moveVelocity);
    }

    public void damage(float damage)
    {
        health -= damage * (1f - Mathf.Min(1f, damageResistance));
        // sending to UI
        UI.setHealthText("Health:" + health.ToString());
    }

    public Vector2 getVelocity()
    {
        return moveVelocity;
    }

    public void setSpeedModifier(float modifier)
    {
        speedModifier = modifier;
    }

    public void setWeaponDamageModifier(float modifier)
    {
        shooting.setWeaponDamageModifier(modifier);
    }

    public void setAttackRateModifier(float modifier)
    {
        shooting.setAttackRateModifier(modifier);
    }

    public void setProjectileSpeedModifier(float modifier)
    {
        shooting.setProjectileSpeedModifier(modifier);
    }

    public void setHealthModifier(float modifier)
    {
        healthModifier = modifier;
    }

    public void setDamageResistance(float modifier)
    {
        damageResistance = modifier;
    }

    public void setFrontalViewDistanceModifier(float modifier)
    {
        viewCone.setFrontalModifier(modifier);
    }

    public void setSurroundViewDistanceModifier(float modifier)
    {
        viewCone.setSurroundModifier(modifier);
    }

    public float getAttackRateModifier()
    {
        return shooting.getAttackRateModifier();
    }
}
