using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 1f;
    public float lifetime = 5f;
    public Vector2 direction = Vector2.right;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Set velocity based on direction
        rb.velocity = direction * speed;

        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Deal damage to player
        if (other.CompareTag("Player"))
        {
            other.gameObject.GetComponent<Player>().ApplyDamage(1.0f, this.transform.position, 60f);

            Destroy(gameObject);
        }

        // Destroy when hitting walls/obstacles
        if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}