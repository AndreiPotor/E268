using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public ViewCone viewCone;
    public Shooting shooting;

    // movement speed
    public float baseSpeed = 50f;
    private float currentSpeed;
    public float speedModifier = 1f; // for buffs/debuffs

    // health of the robot - when it reaches zero it's game over
    public float baseMaxHealth = 25f; // robot's default maximum health
    public float healthModifier = 1f; // for buffs/debuffs
    private float maxHealth; // robot's maximum health after modifiers
    private float currentHealth; // robot's current health
    public float damageResistance = 0f; // for buffs/debuffs (0 < damageResistance <= 1)

    // energy is spent when moving
    // for laser robots, it's also spent when firing the weapon
    // also when it reaches zero it's game over
    public float baseMaxEnergy = 1000f;
    public float energyModifier = 1f;
    private float maxEnergy;     // same concept as health
    private float currentEnergy;
    public float energyDamageResistance = 0f; // for buffs/debuffs (0 < damageResistance <= 1)
    // energy management
    public float baseEnergyIdleConsumption = 1f;
    private float currentEnergyIdleConsumption; // energy lost per second passively
    public float energyIdleConsumptionModifier = 1f;
    public float energyMovingModifier = 2f; // energy lost per second while moving -> it is multiplied with <energyIdleConsumption>
    private float lastEnergyTime; // for managing energy

    // relevant gameObjects
    public Camera followingCamera;
    public Legs legsObject;
    private Rigidbody2D rb;

    void Start()
    {
        // initializing stats
        currentSpeed = baseSpeed * speedModifier;
        maxHealth = baseMaxHealth * healthModifier;
        maxEnergy = baseMaxEnergy * energyModifier;
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
        currentEnergyIdleConsumption = baseEnergyIdleConsumption * energyIdleConsumptionModifier;
        UI.setHealth(currentHealth, maxHealth);
        UI.setEnergy(currentEnergy, maxEnergy);

        rb = GetComponent<Rigidbody2D>();
    }

    private void EnergyManagement(bool moving) {
        // getting the time difference
        float time = Time.time;
        float timeDiff = time - lastEnergyTime;
        lastEnergyTime = time;

        // base formula + check if we are moving
        float energyLoss = currentEnergyIdleConsumption * energyIdleConsumptionModifier;
        if (moving)
            energyLoss *= energyMovingModifier;

        Debug.Log(energyLoss);

        // applying the energy loss
        currentEnergy -= energyLoss * timeDiff;
        UI.setEnergy(currentEnergy, maxEnergy);
    }

    private void Update()
    {
        // getting inputs for moving
        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // processing movement
        rb.AddForce(50f * currentSpeed * Time.deltaTime * moveInput.normalized);

        // managing energy loss - parameter says if we're moving or not
        EnergyManagement(moveInput.x != 0 || moveInput.y != 0);

        // Rotating the cannon following the mouse position
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float AngleRad = Mathf.Atan2(mousePos.y - transform.position.y, mousePos.x - transform.position.x);
        float AngleDeg = (180 / Mathf.PI) * AngleRad - 90f;
        Quaternion prevRotation = legsObject.transform.rotation;
        transform.rotation = Quaternion.Euler(0, 0, AngleDeg);

        // making sure the legs are not rotated with the body
        legsObject.transform.rotation = prevRotation;

        // sending the velocity to the legs GFX so they can animate
        legsObject.setVelocity(rb.velocity);

        // sending commands to the shooting script
        if (Input.GetMouseButton(0)) {
            shooting.StartShoot(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }
        else
            shooting.StopShoot();

        // testing
        if (Input.GetKeyUp(KeyCode.Space)) {
            TakeDamage(1);
            SetEnergyMovingModifier(100f);
        }

        // painting the player on the minimap
        UI.paintMinimap((int)Mathf.Round(transform.position.x), (int)Mathf.Round(transform.position.y), "Player");

        // putting camera to follow
        followingCamera.transform.position = new Vector3(rb.position.x, rb.position.y, followingCamera.transform.position.z);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage * (1f - Mathf.Min(1f, damageResistance));
        // sending to UI
        UI.setHealth(currentHealth, maxHealth);
    }

    public void TakeEnergyDamage(float damage) {
        currentEnergy -= damage * (1f - Mathf.Min(1f, energyDamageResistance));
        // sending to UI
        UI.setEnergy(currentEnergy, maxEnergy);
    }

    public float AddHealth(float val) {
        currentHealth += val;
        if(currentHealth > maxHealth) {
            float ret = currentHealth - maxHealth;
            currentHealth = maxHealth;
            return ret;
        }
        return 0;
    }

    public float AddEnergy(float val) {
        currentEnergy += val;
        if (currentEnergy > maxEnergy) {
            float ret = currentEnergy - maxEnergy;
            currentEnergy = maxEnergy;
            return ret;
        }
        return 0;
    }

    public void SetSpeedModifier(float modifier)
    {
        speedModifier = modifier;
        currentSpeed = baseSpeed * speedModifier;
    }

    public void SetEnergyModifier(float modifier) {
        energyModifier = modifier;
        maxEnergy = baseMaxEnergy * energyModifier;
        if(currentEnergy > maxEnergy)
            currentEnergy = maxEnergy;
        UI.setEnergy(currentEnergy, maxEnergy);
    }

    public void SetHealthModifier(float modifier)
    {
        healthModifier = modifier;
        maxHealth = baseMaxHealth * healthModifier;
        if(currentHealth > maxHealth)
            currentHealth = maxHealth;
        UI.setHealth(currentHealth, maxHealth);
    }

    public void SetDamageResistance(float modifier)
    {
        damageResistance = modifier;
    }

    public void SetEnergyDamageResistance(float modifier) {
        energyDamageResistance = modifier;
    }

    public void SetEnergyIdleConsumptionModifier(float modifier) {
        energyIdleConsumptionModifier = modifier;
        currentEnergyIdleConsumption = baseEnergyIdleConsumption * energyIdleConsumptionModifier;
    }

    public void SetEnergyMovingModifier(float modifier) {
        energyMovingModifier = modifier;
    }
}
