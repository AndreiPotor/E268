using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyCharger : MonoBehaviour
{
    private Animator animator;
    public float energyAmount = 500;
    public float chargingRatePerSec = 50; // per second
    public float transferInterval = 0.1f;
    private Player player;

    public void Start() {
        animator = GetComponent<Animator>();
    }

    private void TransferEnergy() {
        if (energyAmount > chargingRatePerSec) { // normal charging
            float val = chargingRatePerSec * transferInterval;
            energyAmount += player.AddEnergy(val) - val; // AddEnergy() only adds up to max player energy, and returns what's not been added
        }
        else if (energyAmount > 0) { // about to run out
            float val = energyAmount; // sending whatever fraction we have left
            player.AddEnergy(val);
            energyAmount += player.AddEnergy(val) - val; // AddEnergy() only adds up to max player energy, and returns what's not been added
        }
        else { // ran out
            animator.SetTrigger("Depleted");
        }
    }

    private void OnTriggerEnter2D(Collider2D c) {
        if (energyAmount != 0 && c.tag.Equals("Player")) {
            player = c.transform.gameObject.GetComponent<Player>();
            animator.SetTrigger("Entered");
            InvokeRepeating("TransferEnergy", 0, transferInterval);
        }
    }
    private void OnTriggerExit2D(Collider2D c) {
        if (energyAmount != 0 && c.tag.Equals("Player")) {
            animator.SetTrigger("Left");
            CancelInvoke("TransferEnergy");
        }
    }
}
