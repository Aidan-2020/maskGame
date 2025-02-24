using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class simpleFlyingEnemy : Enemy
{

    private Rigidbody2D rb;

    private bool playerIsInRange = false;
    public float verticalSpeed;
    public CircleCollider2D cc;

    public Transform target;
   // public int dropsCordycep = 3;
    bool dead = false;

    float x = 0;
    // Start is called before the first frame update
    void Start()
    {
        // targetVector = new Vector2(target.position.x, target.position.y, -8f)
        rb = GetComponent<Rigidbody2D>();

    }

    // Update is called once per frame
    void Update()
    {
        target = Player.instance.transform;

        // ignore collision with player depending on several factors
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), Player.instance.GetComponent<Collider2D>(), dead || Player.controller.invincible || Player.controller.isDashing || Player.controller.resetting || Player.controller.dead || !Player.instance.GetComponent<Attack>().canAttack);

        if (playerIsInRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            x = transform.position.x;
            //  transform.position = new Vector3(x, .3f * Mathf.Sin(Time.time * verticalSpeed), 0f);
        }

        if (target.position.x > this.transform.position.x && !facingRight)
        {
            Flip();
        }
        else if (target.position.x < this.transform.position.x && facingRight)
        {
            Flip();
        }

        if (life <= 0)
        {
            // print("enemy died");
            // isHitted = true;
            if (!dead)
                StartCoroutine(DestroyEnemy());

        }

    }


    void OnCollisionEnter2D(Collision2D col)
    {
        //  Debug.Log("OnCollisionEnter2D");
        //  print(col.gameObject.tag);
        if (col.gameObject.tag == "Player")
        {
            col.gameObject.GetComponent<Player>().ApplyDamage(1.0f, this.transform.position, 30f);
        }

    }


    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.gameObject.tag == "Player")
        {
            playerIsInRange = true;
            cc.radius += 7;
            //print(isHunterMode);
        }


    }

    private void OnTriggerExit2D(Collider2D collision)
    {

        if (collision.gameObject.tag == "Player")
        {
            playerIsInRange = false;
            // print(isHunterMode);
        }


    }


    public override void ApplyDamage(float damage, float knockback = 1.0f)
    {

        float direction = damage / Mathf.Abs(damage);
        damage = Mathf.Abs(damage);
        // transform.GetComponent<Animator>().SetBool("Hit", true);
        life -= damage;
        if (life < 0) life = 0;
        rb.velocity = Vector2.zero;
        rb.AddForce(new Vector2(direction * (1500f + (speed * 800)), 300f) * knockback);
     //   StartCoroutine(HitTime());

    }


    IEnumerator HitTime()
    {
        GetComponent<SimpleFlash>().Flash(0.4f, 1, true);
        //  isHitted = true;
        //   isInvincible = true;
        speed -= 3;
        yield return new WaitForSeconds(0.1f);
        speed += 3;
        //    isHitted = false;
        // isInvincible = false;
    }

    IEnumerator DestroyEnemy()
    {
        dead = true;
        //CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
        //  capsule.size = new Vector2(1f, 0.25f);
        //   capsule.offset = new Vector2(0f, -0.8f);
        //capsule.direction = CapsuleDirection2D.Horizontal;
        yield return new WaitForSeconds(0.25f);
        rb.velocity = new Vector2(0, rb.velocity.y);
        //  yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }


}
