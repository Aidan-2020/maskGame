using UnityEngine;
using System.Collections;

public class ShooterEnemy : Enemy
{
    [Header("Shooting Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float shootCooldown = 2f;
    private float shootTimer;

    [Header("Player Detection")]
    public float retreatDistance = 3f;   // Distance at which enemy retreats
    public float retreatSpeed = 3f;      // Speed when retreating
    public float minRetreatDistance = 5f; // Min distance enemy will retreat to
    private Transform player;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        shootTimer = shootCooldown; // Start ready to shoot

        // Ensure we have a firePoint
        if (firePoint == null)
        {
            // Create a firepoint if not assigned
            firePoint = new GameObject("FirePoint").transform;
            firePoint.parent = transform;
            firePoint.localPosition = new Vector3(1f, 0, 0); // Position in front
        }
    }

    void Update()
    {
        if (player == null) return;

        // Check if player is to the right or left of the enemy and flip if needed
        if ((player.position.x > transform.position.x && !facingRight) ||
            (player.position.x < transform.position.x && facingRight))
        {
            Flip();
        }

        // Shooting logic
        if (shootTimer <= 0)
        {
            Shoot();
            shootTimer = shootCooldown;
        }
        else
        {
            shootTimer -= Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        
    }

    void Shoot()
    {
        if (projectilePrefab == null) return;

        // Create projectile at fire point
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // Set projectile direction based on facing direction
        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.direction = facingRight ? Vector2.right : Vector2.left;
        }
        else
        {
            // If no Projectile component, try to set velocity on Rigidbody2D
            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            if (projectileRb != null)
            {
                float projectileSpeed = 10f;
                projectileRb.velocity = (facingRight ? Vector2.right : Vector2.left) * projectileSpeed;
            }
        }
    }

    public override void ApplyDamage(float damage, float knockback = 1.0f)
    {
        if (isInvincible) return;

        life -= damage;

        // Flash or show hit effect
        StartCoroutine(FlashEffect());

        if (life <= 0)
        {
            Destroy(gameObject);
        }
    }

    IEnumerator FlashEffect()
    {
        // Simple flash effect when taking damage
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            Color originalColor = sprite.color;
            sprite.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sprite.color = originalColor;
        }
        else
        {
            yield return null;
        }
    }
}