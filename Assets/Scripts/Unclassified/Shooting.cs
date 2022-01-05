using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour
{
    public GameObject shot;

    public bool isEnemy = true;

    public float baseDamage = 1f;
    private float currentDamage;
    public float damageModifier = 1f; // for buffs/debuffs

    public float baseAttackRate = 1f;
    private float currentAttackRate;
    public float attackRateModifier = 1f;  // for buffs/debuffs

    public float baseProjectileSpeed = 100f;
    private float currentProjectileSpeed;
    public float projectileSpeedModifier = 1f; // for buffs/debuffs

    public float baseEnergyCost = 5f;
    private float currentEnergyCost;
    public float energyCostModifier = 1f;

    private Rigidbody2D rb;
    private Player player;

    public Animator animator;
    public AnimationClip shootAnim;
    public AnimationClip chargeAnim;
    public AnimationClip readyAnim;
    public float animationSpeed;

    // for shooting synchronization
    private float chargeDuration = 0.0f; // final duration of one attack
    private float chargeStartTime = 0.0f;
    public float fireDurationPercent = 0.1f;
    private float fireDuration = 0.0f;
    private float fireStartTime = 0.0f;
    private bool charged = true;
    private bool firingAnimation = false;

    private bool shooting = false;
    private Vector2 target;

    private void Start()
    {
        player = GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();
        // params
        currentDamage = baseDamage * damageModifier;
        currentAttackRate = baseAttackRate * attackRateModifier;
        currentProjectileSpeed = baseProjectileSpeed * projectileSpeedModifier;
        currentEnergyCost = baseEnergyCost * energyCostModifier;

        // animation params
        chargeDuration = currentAttackRate * (1 - fireDurationPercent);
        fireDuration = currentAttackRate * fireDurationPercent;
    }

    public void StartShoot(Vector2 target)
    {
        this.target = target;
        shooting = true;
    }

    public void StopShoot()
    {
        shooting = false;
    }

    private void FixedUpdate()
    {
        if (!charged) // not charged
        {
            if (firingAnimation)
            {
                if (Time.time - fireStartTime > fireDuration)
                {
                    firingAnimation = false;
                    chargeStartTime = Time.time; // marking the start of the charge period
                    animator.speed = chargeAnim.length / chargeDuration; // setting the speed for the following charge animation
                    animator.SetTrigger("stopfire");
                }
            }
            else
            {
                if (Time.time - chargeStartTime > chargeDuration)
                {
                    charged = true;
                    animator.SetTrigger("charged");
                    // we don't care about animator speed in the charged state
                }
            }
        }
        else
        {   // charged, ready to shoot
            if (shooting)
            {
                // handle shooting animation
                charged = false;
                firingAnimation = true;
                animator.speed = shootAnim.length / fireDuration; // setting the speed for the following shooting animation
                animator.SetTrigger("fire");
                fireStartTime = Time.time; // marking the start of the fire period

                // handle shooting object creation
                //animator.speed = animationSpeed / cooldownDuration * 0.1f;
                GameObject myShot = Instantiate(shot, transform.position, Quaternion.identity) as GameObject;
                Projectile projectile = myShot.GetComponent<Projectile>();
                projectile.setParams(target, currentDamage * damageModifier, currentProjectileSpeed * projectileSpeedModifier, isEnemy);

                // informing player that we shot so he can manage energy costs and other
                player.TakeEnergyDamage(currentEnergyCost);
            }
        }
    }

    public void SetWeaponDamageModifier(float modifier)
    {
        damageModifier = modifier;
        currentDamage = baseDamage * damageModifier;
    }

    public void SetAttackRateModifier(float modifier)
    {
        attackRateModifier = modifier;
        currentAttackRate = baseAttackRate * attackRateModifier;
        chargeDuration = currentAttackRate * (1 - fireDurationPercent);
        fireDuration = currentAttackRate * fireDurationPercent;
    }

    public void SetProjectileSpeedModifier(float modifier)
    {
        projectileSpeedModifier = modifier;
        currentProjectileSpeed = baseProjectileSpeed * projectileSpeedModifier;
    }

    public void SetEnergyCostModifier(float modifier) {
        energyCostModifier = modifier;
        currentEnergyCost = baseEnergyCost * energyCostModifier;
    }

    public float GetAttackRateModifier()
    {
        return attackRateModifier;
    }
}
