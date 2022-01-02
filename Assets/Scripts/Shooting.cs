using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour
{
    public GameObject shot;

    public bool isEnemy = true;

    public float weaponDamage = 1f;
    public float weaponDamageModifier = 1f; // for buffs/debuffs

    public float attackRate = 0.2f;
    public float attackRateModifier = 1f;  // for buffs/debuffs

    public float projectileSpeed = 100f;
    public float projectileSpeedModifier = 1f; // for buffs/debuffs

    private Rigidbody2D rb;

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
        rb = GetComponent<Rigidbody2D>();
        chargeDuration = attackRate * attackRateModifier * (1 - fireDurationPercent);
        fireDuration = attackRate * attackRateModifier * fireDurationPercent;
    }

    public void startShoot(Vector2 target)
    {
        this.target = target;
        shooting = true;
    }

    public void stopShoot()
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
                projectile.setParams(target, weaponDamage * weaponDamageModifier, projectileSpeed * projectileSpeedModifier, isEnemy);
            }
        }
    }

    public void setWeaponDamageModifier(float modifier)
    {
        weaponDamageModifier = modifier;
    }

    public void setAttackRateModifier(float modifier)
    {
        attackRateModifier = modifier;
        chargeDuration = attackRate * attackRateModifier * (1 - fireDurationPercent);
        fireDuration = attackRate * attackRateModifier * fireDurationPercent;
    }

    public void setProjectileSpeedModifier(float modifier)
    {
        projectileSpeedModifier = modifier;
    }

    public float getAttackRateModifier()
    {
        return attackRateModifier;
    }
}
