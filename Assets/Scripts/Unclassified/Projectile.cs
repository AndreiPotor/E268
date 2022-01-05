using UnityEngine;

public class Projectile : MonoBehaviour
{
    public GameObject spark; // sparks that fly when the projectile hits
    public float damage = 1f;
    public bool enemyProjectile = false;

    private Vector2 targetPosition;
    private Vector2 trajectory;
    private Vector2 trajectoryNormalized;
    private float trajectoryMagnitude;

    private int sparkNr = 5;

    private Player player;

    private bool destroyObject = false;

    public void setParams(Vector3 position, float damage, float travelSpeed, bool isEnemy)
    {
        // initializing parameters
        Vector2 target = (Vector2)position;
        trajectory = target - new Vector2(transform.position.x, transform.position.y);
        this.damage = damage;
        enemyProjectile = isEnemy;

        // adding random angle variation
        trajectory = trajectory.normalized;
        float a = (Random.value - 0.5f) * 0.02f; // the angle
        trajectory = new Vector2(trajectory.x * Mathf.Cos(a) - trajectory.y * Mathf.Sin(a), trajectory.x * Mathf.Sin(a) + trajectory.y * Mathf.Cos(a)).normalized;

        // setting the correct magnitude
        trajectory = trajectory * travelSpeed * (5 + Random.value) / 5f / 100f;

        /*
        if (!enemyProjectile)
        {
            // adding the player movement to the trajectory vector
            player = GameObject.Find("Player").GetComponent<Player>();
            trajectory += player.getVelocity() * Time.deltaTime * 0.2f;
        }
        */

        // rotating the sprite
        float AngleRad = Mathf.Atan2(target.y - transform.position.y, target.x - transform.position.x);
        float AngleDeg = (180 / Mathf.PI) * AngleRad + 90f;
        transform.rotation = Quaternion.Euler(0, 0, AngleDeg);

        // used for raycast collision calculations
        trajectoryNormalized = trajectory.normalized;
        trajectoryMagnitude = trajectory.magnitude;
        
    }

    void FixedUpdate()
    {
        if (destroyObject)
            Destroy(gameObject);

        // Casting a ray along the trajectory(of length up to the next position of the projectile)
        int mask = LayerMask.GetMask("Obstacle", "ObstacleNonAI");
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, trajectoryNormalized, trajectoryMagnitude, mask);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null)
            {
                if (enemyProjectile)
                {
                    if (enemyRoutine(hit))
                        return;
                }
                else if (playerRoutine(hit))
                    return;
            }
        }
        
        //compute the next position
        transform.position += new Vector3(trajectory.x, trajectory.y, 0);
    }

    bool playerRoutine(RaycastHit2D hit)
    {
        if (hit.collider.gameObject.CompareTag("Enemy"))
        {
            hit.collider.GetComponent<EnemyAI>().damage(damage);
            shootSpark(hit, sparkNr);
            endProjectile(hit);
            return true;
        }
        else if (hit.collider.gameObject.CompareTag("Wall") || hit.collider.gameObject.CompareTag("Door"))
        {
            shootSpark(hit, sparkNr);
            endProjectile(hit);
            return true;
        }
        else if ( ! hit.collider.gameObject.CompareTag("Player"))
        {
            shootSpark(hit, sparkNr);
            endProjectile(hit);
            return true;
        }

        return false;
    }

    bool enemyRoutine(RaycastHit2D hit)
    {
        if (hit.collider.gameObject.CompareTag("Player"))
        {
            shootSpark(hit, sparkNr);
            hit.collider.gameObject.GetComponent<Player>().TakeDamage(damage);
            endProjectile(hit);
            return true;
        }
        else if (hit.collider.gameObject.CompareTag("Wall") || hit.collider.gameObject.CompareTag("Door"))
        {
            shootSpark(hit, sparkNr);
            endProjectile(hit);
            return true;
        }/*
        else if (hit.collider.gameObject.CompareTag("Enemy") && !GameObject.ReferenceEquals(hit.collider.gameObject, gameObject)) // hit another enemy
        {
            // should add a method to reposition itself since it hit an ally
            hit.collider.gameObject.GetComponent<EnemyAI>().damage(damage);
            Destroy(gameObject);
            return true;
        } 
        */
        else if ( ! hit.collider.gameObject.CompareTag("Enemy"))
        {
            shootSpark(hit, sparkNr);
            endProjectile(hit);
            return true;
        }

        return false;
    }

    void shootSpark(RaycastHit2D hit, int nr)
    {
        for (int i = 0; i < nr; i++)
        {
            Vector2 point = hit.point;
            Vector2 target = point + ((Vector2)Random.onUnitSphere).normalized;

            GameObject mySpark = Instantiate(spark, point, Quaternion.identity) as GameObject;

            float AngleRad = Mathf.Atan2(target.y - point.y, target.x - point.x);
            float AngleDeg = (180 / Mathf.PI) * AngleRad + 90f;
            mySpark.transform.rotation = Quaternion.Euler(0, 0, AngleDeg);

            Rigidbody2D rb = mySpark.GetComponent<Rigidbody2D>();
            rb.AddForce((target - point) * 1000f);
        }
    }

    private void endProjectile(RaycastHit2D hit)
    {
        gameObject.transform.position = hit.point; // moving the position to the hit position so that the trail renderer picks it up
        // for this however we need for physics update to pass:
        destroyObject = true; // sets it so that the object will be destroy on next physics engine update
    }
}
