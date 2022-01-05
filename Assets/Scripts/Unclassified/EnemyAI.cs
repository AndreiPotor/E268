using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class EnemyAI : MonoBehaviour
{
    // Line of Sight
    public float seeDistance = 100f;
    public float scanInterval = .3f;

    // scouting
    float newScoutPointInterval = 5f;
    float newScoutPointCounter = 1000f;
    private float scoutDistance = 10f;

    // AI
    Vector2 target = Vector2.zero;
    AIDestinationSetter dest;
    AIPath aiPath;
    Vector3 playerPos = Vector3.zero;

    // chasing mechanics
    bool chasingPlayer = false;
    bool canShootPlayer = false;
    bool isCloseEnough = false;
    float chaseTime = 5f; // how many seconds to chase after losing sight of player
    float currentChaseTime = 0f;
    Vector3 lastSeenLocation = Vector3.zero;
    float cheatPercent = 0f;

    // Legs GFX
    public Legs legsObject;
    public Transform turret;
    private Vector2 prevPos = Vector2.zero;

    // Stats + combat
    public Shooting shooting;
    public GameObject shot;
    public float health = 5f;
    public float damageResistance = 0f;
    public float optimalRange = 10f;


    // Start is called before the first frame update
    void Start()
    {
        // disable path logging (in the console)
        AstarPath.active.logPathResults = PathLog.None;

        prevPos = transform.position;

        dest = GetComponent<AIDestinationSetter>();
        aiPath = GetComponent<AIPath>();

        // starting the AI
        InvokeRepeating("UpdatePath", 0f, scanInterval);
    }

    private void UpdatePath()
    {
        playerPos = GameObject.FindGameObjectWithTag("Player").transform.position;
        if (targetInSight(playerPos, seeDistance))
        {
            chasingPlayer = true; // following the player
            canShootPlayer = true; // can shoot because player in line of sight
            lastSeenLocation = playerPos; // used for remembering where we last saw the player so that if we dont see him anymore, we go there

            // will not getting closer if we are close enough to shoot
            if (Vector3.Distance(playerPos, transform.position) <= optimalRange)
                isCloseEnough = true;
            else
                isCloseEnough = false;

            cheatPercent = 0.7f;
        }
        else
        {
            isCloseEnough = false; // we will get closer if we can't see the player anymore
            canShootPlayer = false; // cant shoot because not in line of sight
            if (chasingPlayer == true) // either we see the player and chase, or we go to his last location
            {
                // getting a bit closer to the player's location even if we dont see him
                lastSeenLocation = (1 - cheatPercent) * lastSeenLocation + cheatPercent * playerPos;
                cheatPercent *= 0.7f;
                // this part makes sure that we follow the player for only the required time - after that, start scouting
                currentChaseTime += scanInterval;
                if (currentChaseTime > chaseTime)
                {
                    chasingPlayer = false;
                    currentChaseTime = 0f;
                }
            }
        }

        if (chasingPlayer)
        {
            if (isCloseEnough)
                target = transform.position;
            else
                target = lastSeenLocation;
            // shoot for player
            if (canShootPlayer)
                shooting.startShoot(playerPos);
        }
        else // pick a random point and scout
        {
            scout();
        }

        // setting the final target position - path destination component only takes in an uninitializable Transform object though
        // should find a better way to do this
        GameObject temp = new GameObject();
        temp.transform.position = target;
        dest.target = temp.transform;
        aiPath.SearchPath();
        Destroy(temp);
    }

    private void scout()
    {
        // Choosing a new scout point every <newScoutPointInterval> seconds
        if (newScoutPointCounter > newScoutPointInterval)
        {
            newScoutPointCounter = 0f;
            target = (Vector2)transform.position + Random.insideUnitCircle * scoutDistance;
        }
        else
            newScoutPointCounter += scanInterval * (1 + Random.value);
    }

    // Update is called once per frame
    void Update()
    {
        playerPos = GameObject.FindGameObjectWithTag("Player").transform.position;
        Vector2 moveVel = (Vector2)transform.position - prevPos;

        // Turret GFX: look at player if in line of sight, otherwise scan around
        if (canShootPlayer)
            turret.rotation = Quaternion.LookRotation(Vector3.forward, playerPos - transform.position);
        else
            if(moveVel.magnitude > 0.01f)
                turret.rotation = Quaternion.LookRotation(Vector3.forward, moveVel);

        //Debug.Log(rb.velocity.magnitude);

        legsObject.setVelocity(50f * moveVel);
    }

    private void FixedUpdate()
    {
        prevPos = transform.position;
    }

    bool targetInSight(Vector3 position, float maxRange)
    {
        Vector2 trajectory = (Vector2)(position - transform.position);
        // Casting a ray along the trajectory(of length up to the next position of the projectile)
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, trajectory.normalized, maxRange);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null)
            {
                if (hit.collider.gameObject.layer == 8)
                {
                    return false;
                }
                else if (hit.collider.gameObject.CompareTag("Player"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void damage(float damage)
    {
        health -= damage * (1f - Mathf.Min(1f, damageResistance));
        if (health <= 0)
        {
            Destroy(gameObject);
            Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
            float modif = player.getAttackRateModifier();
            if(modif >= 0.15f)
                player.setAttackRateModifier(modif * 0.8f);
        }
    }
}
