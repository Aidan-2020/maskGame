using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public static Player controller;
    public static GameObject instance;
    public Transform camTarget;

    [SerializeField] private float m_JumpForce = 2000f;                         // Amount of force added when the player jumps.
    [Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;   // How much to smooth out the movement
    [SerializeField] private bool m_AirControl = false;                         // Whether or not a player can steer while jumping;
    [SerializeField] public LayerMask m_WhatIsGround;                          // A mask determining what is ground to the character
    [SerializeField] public Transform m_GroundCheck;                            // A position marking where to check if the player is grounded.
    [SerializeField] public Transform m_LeftGroundCheck, m_RightGroundCheck;    // Positions marking where to check if the player is solidly grounded.
    [SerializeField] public Transform m_LowGroundCheck;                         // A position marking where to check if the player will be grounded.
    [SerializeField] private Transform m_WallCheck;				                //Posicion que controla si el personaje toca una pared
    [SerializeField] private Transform m_FarWallCheck;
    [SerializeField] private Transform groundParticle;

    const float k_GroundedRadius = .12f; // Radius of the overlap circle to determine if grounded
    public bool m_Grounded;            // Whether or not the player is grounded.
    public bool m_leftGrounded, m_rightGrounded; // Whether or not the player is solidly grounded.
    public Rigidbody2D m_Rigidbody2D;
    public bool m_FacingRight = true;  // For determining which way the player is currently facing.
    private Vector3 velocity = Vector3.zero;
    public float limitFallSpeed = 20f; // Limit fall speed
    public bool wallSlide_Unlocked = false;
    public bool doubleJump_Unlocked = true;
    public bool specialAttack_Unlocked = false;
    public bool canDoubleJump = false; //If player can double jump
    [SerializeField] private float m_DashForce = 25f;
    public bool canDash = false;
    public bool isDashing = false; //If player is dashing
    private bool speedBoost = false; //Gives player speed boost during dash
    public bool m_IsWall = false; //If there is a wall in front of the player
    public bool m_IsFarWall = false; //If there is a wall close enough to the player to wall jump
    public bool isWallSliding = false; //If player is sliding in a wall
    private bool oldWallSlidding = false; //If player is sliding in a wall in the previous frame
    public bool canWallSlide = true; //If player can slide down this wall
    private bool canWallGrip = true; //If player can grip on this wall
    private float prevVelocityX = 0f;
    private bool canCheck = false; //For check if player is wallsliding
    public float wallSlideSpeed = 0; // the default wallSlidSpeed was -5

    public float wallSlidingFor = 0.0f;
    private float cantMove = 0f;
    public float life = 10f; //Life of the player
    public bool invincible = false; //If player can die
    public bool dead = false; // Dead status
    public bool canMove = true; //If player can move
    public bool isJumping = false;
    public bool isJumpingDJ = false;
    public bool resetting = false;
    public float jumpTime;
    private bool holdingJump = false;
    public bool isStill = false;

    private AudioSource[] sources;
   // public AudioClip audioJump;

    public float stunDuration = 0.25f;
    public float iFrames = 1f;
    public float lastOnLand = 0f;
    public float jumpCooldown = 0f;
    public float notFallingFor = 0f;
    public float beenOnLand = 0f;
    public float notSimulated = 0f;
    public Transform reset_point;
    public bool inDeathZone;
    private Vector3 lastOnLandLocation;

    private CapsuleCollider2D cc;
    private Vector2 colliderSize;
    [SerializeField] private float slopeCheckDistance;
    [SerializeField] private float maxSlopeAngle;
    private float slopeDownAngle;
    private float slopeDownAngleOld;
    private float slopeSideAngle;
    private Vector2 slopeNormalPerp;
    public bool isOnSlope;
    private bool canWalkOnSlope;

    //private Animator animator;

    
    public ParticleSystem particleJumpUp; //Trail particles
    public ParticleSystem particleJumpDown; //Explosion particles
    public ParticleSystem particleWallJump; //Wall jump particles
    public ParticleSystem particleLand; //Big landing particles 


    private bool limitVelOnWallJump = false; //For limit wall jump distance with low fps
    private float limitVelOnWallJumpCooldown = 0.0f;

    public PhysicsMaterial2D slippery, friction;

    [Header("Events")]
    [Space]

    public UnityEvent OnFallEvent;
    public UnityEvent OnLandEvent;

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    private void Awake()
    {
        inDeathZone = false;

        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        cc = GetComponent<CapsuleCollider2D>();
        colliderSize = cc.size;

       // animator = GetComponent<Animator>();
        sources = transform.GetComponents<AudioSource>();

        if (OnFallEvent == null)
            OnFallEvent = new UnityEvent();

        if (OnLandEvent == null)
            OnLandEvent = new UnityEvent();


        // Singleton design pattern
        if (controller != null && controller != this)
        {
        }
        else
        {
            controller = this;
            instance = gameObject;
            //DontDestroyOnLoad(gameObject);
        }
    }

    private void SlopeCheck()
    {
        Vector2 checkPos = transform.position - new Vector3(0.0f, colliderSize.y / 2);
        SlopeCheckHorizontal(checkPos);
        SlopeCheckVertical(checkPos);
    }

    private void SlopeCheckHorizontal(Vector2 checkPos)
    {
        RaycastHit2D front = Physics2D.Raycast(checkPos, transform.right, slopeCheckDistance, m_WhatIsGround);
        RaycastHit2D back = Physics2D.Raycast(checkPos, -transform.right, slopeCheckDistance, m_WhatIsGround);
        if (front)
        {
            isOnSlope = true;
            slopeSideAngle = Vector2.Angle(front.normal, Vector2.up);
        }
        else if (back)
        {
            isOnSlope = true;
            slopeSideAngle = Vector2.Angle(back.normal, Vector2.up);
        }
        else
        {
            isOnSlope = false;
            slopeSideAngle = 0.0f;
        }
    }

    private void SlopeCheckVertical(Vector2 checkPos)
    {
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, m_WhatIsGround);
        if (hit)
        {
            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;
            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);

            if (slopeDownAngle != slopeDownAngleOld)
            {
                isOnSlope = true;
            }

            slopeDownAngleOld = slopeDownAngle;
        }

        if (slopeDownAngle > maxSlopeAngle || slopeSideAngle > maxSlopeAngle)
        {
            canWalkOnSlope = false;
        }
        else
        {
            canWalkOnSlope = true;
        }
    }

    private void FixedUpdate()
    {
        CleanSounds();

        //if (changeScene.changingScene && changeScene.noFalling && m_Rigidbody2D.velocity.y < 0f)
        //{
        //    m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, 0f);
        //}

        if (!GetComponent<Rigidbody2D>().simulated)
        {
            notSimulated += Time.fixedDeltaTime;
        }
        if (notSimulated >= 0.3f)
        {
            GetComponent<Rigidbody2D>().simulated = true;
            notSimulated = 0f;
        }
       // isStill = (m_Rigidbody2D.velocity.x < 0.0001f && m_Rigidbody2D.velocity.x > -0.0001f && !animator.GetBool("IsSattacking"));
        isStill = (m_Rigidbody2D.velocity.x < 0.0001f && m_Rigidbody2D.velocity.x > -0.0001f);

        if (isDashing)
        {
            GetComponent<Attack>().DoDashDamage(0.0f);
        }

        if (!dead)
        {
           // animator.SetBool("IsDead", false);
        }

        if (controller == null)
        {
            controller = this;
            instance = gameObject;
        }

        GameObject rp = GameObject.FindGameObjectWithTag("Reset Point");
        if (rp != null)
            reset_point = rp.transform;

        SlopeCheck();

        if (lastOnLand == 0.0f)
        {
            lastOnLandLocation = transform.position;
        }

        lastOnLand = Mathf.Clamp(lastOnLand + Time.fixedDeltaTime, 0, 20f);

        bool wasGrounded = m_Grounded;
        m_Grounded = false;

     //   bool identifiedGround = false;

        if (isWallSliding && canWallSlide)
        {
            Collider2D[] lowColliders = Physics2D.OverlapCircleAll(m_LowGroundCheck.position, 0.01f, m_WhatIsGround);
            for (int i = 0; i < lowColliders.Length; i++)
            {
                if (lowColliders[i].gameObject != gameObject && (lowColliders[i].gameObject.tag == "Ground" || lowColliders[i].gameObject.tag == "GroundNoSlide"))
                {
                    canWallSlide = false;
                   // animator.SetBool("IsJumping", false);
                }
            }
        }

        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        // This can be done using layers instead but Sample Assets will not overwrite your project settings.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject.tag == "Enemy")
            {
                m_Grounded = true;
                lastOnLand = 0f;
                OnLandEvent.Invoke();

                if (colliders[i].gameObject.transform.position.x > this.transform.position.x)
                {
                    m_Rigidbody2D.AddForce(new Vector2(-10f, 5f));
                }
                else
                {
                    m_Rigidbody2D.AddForce(new Vector2(10f, 5f));
                }

            }
            if (colliders[i].gameObject != gameObject && (colliders[i].gameObject.tag == "obstacle"))
            {
                m_Grounded = true;
                canDoubleJump = false;

                if (!wasGrounded && jumpCooldown <= 0.1f)
                    jumpCooldown = 0.05f;

                if (!wasGrounded && !holdingJump && !(m_Rigidbody2D.velocity.y > 0f))
                {
                    if (!m_IsWall && !isDashing)
                    {
                        //particleJumpDown.Play();
                    }

                    if (m_Rigidbody2D.velocity.y <= 0f)
                    {
                        limitVelOnWallJump = false;
                        limitVelOnWallJumpCooldown = 0f;
                    }
                }
            }
            else if (colliders[i].gameObject != gameObject && (colliders[i].gameObject.tag == "Ground" || colliders[i].gameObject.tag == "Wall" || colliders[i].gameObject.tag == "Breakable Wall" || colliders[i].gameObject.tag == "GroundNoSlide"))
            {
                m_Grounded = true;
                lastOnLand = 0f;
                canDoubleJump = false;


                if (!wasGrounded && jumpCooldown <= 0.1f)
                {
                    jumpCooldown = 0.05f;
                }

                if (!wasGrounded && !holdingJump && !(m_Rigidbody2D.velocity.y > 0f))
                {
                    if (!m_IsWall && !isDashing)
                    {
                        //particleJumpDown.Play();
                    }

                    if (m_Rigidbody2D.velocity.y <= 0f)
                    {
                        limitVelOnWallJump = false;
                        limitVelOnWallJumpCooldown = 0f;
                    }
                }

                // push you up if you jump from below to rock platform
                if (!wasGrounded && !holdingJump && m_Rigidbody2D.velocity.y > 0f)
                {
                    if (colliders[i].gameObject.GetComponent<PlatformEffector2D>() != null)
                    {
                        transform.position = new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z);
                    }

                }
            }
        }

        // check left position
        Collider2D[] leftColliders = Physics2D.OverlapCircleAll(m_LeftGroundCheck.position, k_GroundedRadius, m_WhatIsGround);
        m_leftGrounded = false;
        for (int i = 0; i < leftColliders.Length; i++)
        {
            if (leftColliders[i].gameObject != gameObject && (leftColliders[i].gameObject.tag == "Ground" || leftColliders[i].gameObject.tag == "Wall"))
            {
                m_leftGrounded = true;
                break;
            }
        }

        // check right position
        Collider2D[] rightColliders = Physics2D.OverlapCircleAll(m_RightGroundCheck.position, k_GroundedRadius, m_WhatIsGround);
        m_rightGrounded = false;
        for (int i = 0; i < rightColliders.Length; i++)
        {
            if (rightColliders[i].gameObject != gameObject && (rightColliders[i].gameObject.tag == "Ground" || rightColliders[i].gameObject.tag == "Wall"))
            {
                m_rightGrounded = true;
                break;
            }
        }

        // if left, right, and middle are all grounded, you are solidly grounded. update reset point pos
        if (m_leftGrounded && m_rightGrounded && m_Grounded && !inDeathZone)
        {
            reset_point.position = new Vector3(m_GroundCheck.position.x, m_GroundCheck.position.y + 2f, m_GroundCheck.position.z);
        }

        m_IsWall = false;
        m_IsFarWall = false;

        // Trick game into thinking you're grounded if your y velocity isn't changing
        //if (m_Rigidbody2D.velocity.y < 0.001f && m_Rigidbody2D.velocity.y > -0.001f && !(changeScene.changingScene && changeScene.noFalling))
        //{
        //    if (!animator.GetBool("IsSattacking"))
        //        notFallingFor = Mathf.Clamp(notFallingFor + Time.fixedDeltaTime, 0, 1f);

        //    if (notFallingFor >= 0.02f)
        //    {
        //        m_Grounded = true;
        //        wasGrounded = true;
        //        isJumping = false;
        //        isJumpingDJ = false;
        //        lastOnLand = 0f;
        //        wallSlidingFor = 0f;
        //        animator.SetBool("IsJumping", false);
        //    }
        //}
        //else
        //{
        //    notFallingFor = 0f;
        //}

        if (!m_Grounded)
        {
            Collider2D[] collidersWall = Physics2D.OverlapCircleAll(m_WallCheck.position, 0.1f, m_WhatIsGround);
            for (int i = 0; i < collidersWall.Length; i++)
            {
                if (collidersWall[i].gameObject != null && collidersWall[i].gameObject.tag != "GroundNoSlide")
                {
                    isDashing = false;
                    if (collidersWall[i].gameObject.GetComponent<PlatformEffector2D>() == null)
                        m_IsWall = true;
                }
            }

            beenOnLand = 0f;

            Collider2D[] collidersFarWall = Physics2D.OverlapCircleAll(m_FarWallCheck.position, 0.01f, m_WhatIsGround);
            for (int i = 0; i < collidersFarWall.Length; i++)
            {
                if (collidersFarWall[i].gameObject != null && collidersFarWall[i].gameObject.tag != "GroundNoSlide")
                {
                    if (collidersFarWall[i].gameObject.GetComponent<PlatformEffector2D>() == null)
                        m_IsFarWall = true;
                }
            }

            if (!(m_Rigidbody2D.velocity.y < 3f && m_Rigidbody2D.velocity.y > -3f))
            {
                if (!isWallSliding)
                {
                    OnFallEvent.Invoke();
                }
            }

            prevVelocityX = m_Rigidbody2D.velocity.x;
        }
        else
        {
            if (!wasGrounded)
                canWallSlide = true;

            if (beenOnLand >= 0.1f)
            {
                canWallGrip = true;
                if (!isWallSliding)
                {
                    limitVelOnWallJump = false;
                    limitVelOnWallJumpCooldown = 0.0f;
                }
            }

            if (beenOnLand < 5f)
                beenOnLand += Time.fixedDeltaTime;
            if (!(m_Rigidbody2D.velocity.y > 0f))
            {
                OnLandEvent.Invoke();
                isJumping = false;
                isJumpingDJ = false;
                jumpTime = 0f;
            }
            if (jumpCooldown > 0f)
                jumpCooldown -= Time.fixedDeltaTime;
        }

        if (!m_IsWall)
        {
         //   animator.SetBool("IsWallSliding", false);
        }

        if (limitVelOnWallJump)
        {
            limitVelOnWallJumpCooldown += Time.fixedDeltaTime;
            if (limitVelOnWallJumpCooldown >= 0.2f)
            {
                limitVelOnWallJump = false;
                limitVelOnWallJumpCooldown = 0f;
            }
        }

    }

    public void Move(float move, bool jump, bool dash, bool releaseJump)
    {
        if (isJumping && !canDoubleJump && doubleJump_Unlocked)
        {
            canDoubleJump = true;
        }

        if (releaseJump && canDoubleJump)
        {
            if (isJumping)
                isJumpingDJ = true;
            isJumping = false;
            jumpTime = 0;
        }

        if (canMove)
        {
            if (dash && canDash && !isWallSliding)
            {
                StartCoroutine(DashCooldown());
            }

            if (speedBoost)
            {
                if (isOnSlope && m_Grounded && !isJumping && !isJumpingDJ && canWalkOnSlope)
                {
                    m_Rigidbody2D.velocity = new Vector2(transform.localScale.x * m_DashForce * -slopeNormalPerp.x, m_DashForce * -slopeNormalPerp.y * (m_FacingRight ? 1 : -1));
                }
                else
                {
                    m_Rigidbody2D.velocity = new Vector2(transform.localScale.x * m_DashForce, 0.0f);
                }

            }
            //only control the player if grounded or airControl is turned on
            else if (m_Grounded || m_AirControl)
            {
                if (m_Rigidbody2D.velocity.y < -limitFallSpeed)
                    m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, -limitFallSpeed);

                // Move the character by finding the target velocity
                Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);

                if (isOnSlope && m_Grounded && !isJumping && !isJumpingDJ && canWalkOnSlope)
                {
                    targetVelocity.Set(move * 10f * -slopeNormalPerp.x, move * 10f * -slopeNormalPerp.y, 0.0f);
                }

                if (limitVelOnWallJump && limitVelOnWallJumpCooldown <= 0.2f)
                {
                    float newX = Mathf.Lerp(m_Rigidbody2D.velocity.x, move * 12f, 0.05f);
                    if (limitVelOnWallJumpCooldown >= 0.1f)
                        targetVelocity.x = Mathf.Lerp(newX, targetVelocity.x, limitVelOnWallJumpCooldown);
                    else
                        targetVelocity.x = newX;
                }

                

                // And then smoothing it out and applying it to the character
                if (move == 0.0 && m_Rigidbody2D.velocity.x != 0.0f)
                {
                    if (canWalkOnSlope)
                        GetComponent<CapsuleCollider2D>().sharedMaterial = friction;
                    m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref velocity, m_MovementSmoothing * 2.5f);
                }
                else
                {
                    GetComponent<CapsuleCollider2D>().sharedMaterial = slippery;
                    m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref velocity, m_MovementSmoothing);
                }

                // If the input is moving the player right and the player is facing left   && !animator.GetBool("IsWallSliding")
                if (move > 0 && !m_FacingRight && canWallSlide && !(limitVelOnWallJump && limitVelOnWallJumpCooldown <= 0.4))
                {
                    // ... flip the player.
                    Flip();
                }
                // Otherwise if the input is moving the player left and the player is facing right...
                //&& !animator.GetBool("IsWallSliding") 
                else if (move < 0 && m_FacingRight && canWallSlide && !(limitVelOnWallJump && limitVelOnWallJumpCooldown <= 0.4))
                {
                    // ... flip the player.
                    Flip();
                }
            }

            // If the player should jump...
            if (lastOnLand < 0.15f && jump && !isJumping && !canDoubleJump && slopeDownAngle <= maxSlopeAngle) // incorporates coyote time with lastOnLand
            {
                // Add a vertical force to the player.
               // animator.SetBool("JumpUp", true);
               // PlaySound(audioJump);
               // animator.SetBool("IsJumping", true);

                m_Grounded = false;
                if (!isJumping)
                {
                   // particleJumpUp.Play();
                    holdingJump = true;
                }
                isJumping = true;
                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, 0);
                m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce * .7f)); //force added during a jump
                if (doubleJump_Unlocked) { canDoubleJump = true; }
            }
            else if (!m_Grounded && jump && canDoubleJump && !isWallSliding && !(wallSlide_Unlocked && m_IsFarWall && canWallSlide) && !isJumping)
            {
                if (doubleJump_Unlocked) { canDoubleJump = false; }
                holdingJump = true;
                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, 0);

                if (isJumpingDJ)
                    m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce * .7f));
                else
                    m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce / 1.2f));

                if (limitVelOnWallJump && ((move > 0 && !m_FacingRight) || (move < 0 && m_FacingRight)))
                {
                    limitVelOnWallJump = false;
                    limitVelOnWallJumpCooldown = 0.0f;
                }
               // animator.SetBool("IsDoubleJumping", true);
            }

            else if (m_IsFarWall && !m_Grounded && wallSlide_Unlocked && canWallSlide) // looks like this is where wall sliding is managed
            {
                if (jump)
                {
                    if (m_FarWallCheck.localPosition.x > 0)
                    {
                        isWallSliding = false;
                      //  animator.SetBool("IsWallSliding", true);
                        Flip();
                    }

                    isJumping = false;
                    isJumpingDJ = false;
                }

                // Fixes being unable to jump during wall slide
                if (isWallSliding)
                {
                    isJumping = false;
                    isJumpingDJ = false;
                }

                if (m_IsWall && !oldWallSlidding && m_Rigidbody2D.velocity.y < 0 || isDashing)
                {
                    isWallSliding = true;
                    m_WallCheck.localPosition = new Vector3(-m_WallCheck.localPosition.x, m_WallCheck.localPosition.y, 0);

                    // If the input is moving the player right and the player is facing right...
                    if (move > 0 && m_FacingRight)
                    {
                        // ... flip the player.
                        Flip();
                        m_FarWallCheck.localPosition = new Vector3(-m_FarWallCheck.localPosition.x, m_FarWallCheck.localPosition.y, 0);
                    //    animator.SetBool("IsWallSliding", true);
                    }
                    // Otherwise if the input is moving the player left and the player is facing left...
                    else if (move < 0 && !m_FacingRight)
                    {
                        // ... flip the player.
                        Flip();
                        m_FarWallCheck.localPosition = new Vector3(-m_FarWallCheck.localPosition.x, m_FarWallCheck.localPosition.y, 0);
                        //animator.SetBool("IsWallSliding", true);
                    }
                    StartCoroutine(WaitToCheck(0.1f));
                    // if (doubleJump_Unlocked) { canDoubleJump = true; }
                }
                isDashing = false;

                if (isWallSliding)
                {
                    if (move * transform.localScale.x > 0.1f)
                    {
                        StartCoroutine(WaitToEndSliding());
                    }
                    else if (move == 0)
                    { // previously cancelled slide but that is not necessary - however this else statement still is lol
                        // StartCoroutine(WaitToEndSliding());
                        // isWallSliding = false;
                        // animator.SetBool("IsWallSliding", false);
                        // oldWallSlidding = false;
                        // m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
                    }
                    else if (!jump)
                    {
                        wallSlidingFor += Time.fixedDeltaTime;
                        // so this is where the wall sliding happens - aidan
                        oldWallSlidding = true;
                        if (wallSlidingFor <= 1.0f && canWallGrip)
                            wallSlideSpeed = 0.5f;
                        else
                        {
                            wallSlideSpeed = Mathf.Lerp(wallSlideSpeed, -1f, 0.01f);
                            canWallGrip = false;
                        }
                        m_Rigidbody2D.velocity = new Vector2(-transform.localScale.x * 2, wallSlideSpeed);
                    }
                }

                if (jump && !isJumping)
                {
                   // animator.SetBool("IsJumping", true);
                    //PlaySound(audioJump);
                   // animator.SetBool("JumpUp", true);
                    if (!isJumping)
                    {
                       // particleWallJump.Play();
                       // particleJumpUp.Play();
                        holdingJump = true;
                    }
                    isJumping = true;
                    jumpTime = 0f;

                    float x_vel = m_FacingRight ? 10f : -10f;
                    m_Rigidbody2D.velocity = new Vector2(0f, 0f);
                    m_Rigidbody2D.AddForce(new Vector2(x_vel * 100f, m_JumpForce * .7f));

                    limitVelOnWallJump = true;
                    limitVelOnWallJumpCooldown = 0.0f;

                    if (doubleJump_Unlocked) { canDoubleJump = true; }
                    isWallSliding = false;
                 //   animator.SetBool("IsWallSliding", false);
                    oldWallSlidding = false;
                    m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
                    m_FarWallCheck.localPosition = new Vector3(Mathf.Abs(m_FarWallCheck.localPosition.x), m_FarWallCheck.localPosition.y, 0);
                }
                else if (dash && canDash)
                {
                    isWallSliding = false;
                    //animator.SetBool("IsWallSliding", false);
                    oldWallSlidding = false;
                    m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
                    m_FarWallCheck.localPosition = new Vector3(Mathf.Abs(m_FarWallCheck.localPosition.x), m_FarWallCheck.localPosition.y, 0);
                    StartCoroutine(DashCooldown());
                }
            }
            else if (isWallSliding && !m_IsWall && !m_IsFarWall && canCheck)
            {
                isWallSliding = false;
                //animator.SetBool("IsWallSliding", false);
                oldWallSlidding = false;
                m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
                m_FarWallCheck.localPosition = new Vector3(Mathf.Abs(m_FarWallCheck.localPosition.x), m_FarWallCheck.localPosition.y, 0);
            }
        }
        //&& !animator.GetBool("IsBowAttacking") && !animator.GetBool("BowReleased")
        else if (!dead && !resetting  && !InventoryNavigator.selected) // fix being stuck unable to move
        {
            cantMove += 0.01f;
            if (cantMove > stunDuration)
            {
                canMove = true;
                cantMove = 0f;
            }
        }

        if (!canMove)
            GetComponent<CapsuleCollider2D>().sharedMaterial = friction;

        if (isJumping || isJumpingDJ) // this code is absolutely gross but necessary
        {
            jumpTime += Time.fixedDeltaTime;
        }

        if (releaseJump)
        {
            holdingJump = false;
        }

        //hold jump distance extentions
        if (holdingJump)
        {
            if (isJumping || isJumpingDJ)
            {
                m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce / 800f / jumpTime));
            }
        }

    }


    private void Flip()
    {
        // Switch the way the player is labelled as facing.
        m_FacingRight = !m_FacingRight;

        // Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;

        // Change camera target offset
        Vector3 newLocalPos = camTarget.localPosition;
        newLocalPos.x *= -1;
        camTarget.localPosition = newLocalPos;
    }

    public void ApplyDamage(float damage, Vector3 position, float knockBack)
    {
        ApplyDamage(damage, position, knockBack, false);
    }

    public void ApplyDamage(float damage, Vector3 position, float knockBack, bool bypassFlash)
    {
        if (!invincible && !resetting && !dead)
        {
            StartCoroutine(MakeInvincible(iFrames));
            // animator.SetBool("Hit", true);
            // GetComponent<TimeStop>().StopTime(0.05f, 100, 0.1f);
            int integerDamageValue = (int)damage;
            // this.GetComponent<health>().playerHealth -= (integerDamageValue);
            this.GetComponent<health>().takeAwayHeart();
            Vector2 damageDir = Vector3.Normalize(transform.position - position) * 60f;
            damageDir.y /= 2f; // reduces vertical knockback

            m_Rigidbody2D.velocity = Vector2.zero;
            m_Rigidbody2D.AddForce(damageDir * knockBack);
            if (this.GetComponent<health>().playerHealth <= 0 && !dead)
            {
                //StartCoroutine(WaitToDead());
            }
            else
            {
                //if (!bypassFlash)
                //    GetComponent<SimpleFlash>().Flash(iFrames, 3);
                StartCoroutine(Stun(stunDuration));
              //  StartCoroutine(MakeInvincible(iFrames));
            }
        }
    }

    IEnumerator DashCooldown()
    {
        isDashing = true;
        speedBoost = true;
        //animator.SetBool("IsDashing", true);
        canDash = false;
        yield return new WaitForSeconds(0.1f);
        speedBoost = false;
        yield return new WaitForSeconds(0.3f);
        isDashing = false;
        GetComponent<Attack>().ignoredEnemies.Clear();
        yield return new WaitForSeconds(1.6f);
        canDash = true;
    }

    IEnumerator Stun(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        if (!resetting)
        {
            canMove = true;
        }
    }

    public void Invincible(float time)
    {
        StartCoroutine(MakeInvincible(time));
    }

    IEnumerator MakeInvincible(float time)
    {
        invincible = true;

        yield return new WaitForSeconds(time);
        if (!resetting)
        {
            invincible = false;
        }
    }
    IEnumerator WaitToMove(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

    IEnumerator WaitToCheck(float time)
    {
        canCheck = false;
        yield return new WaitForSeconds(time);
        canCheck = true;
    }

    IEnumerator WaitToEndSliding()
    {
        yield return new WaitForSeconds(0.1f);
        // if (doubleJump_Unlocked) { canDoubleJump = true; }
        isWallSliding = false;
        //animator.SetBool("IsWallSliding", false);
        oldWallSlidding = false;
        // wallSlidingFor = 0.0f;
        m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
        m_FarWallCheck.localPosition = new Vector3(Mathf.Abs(m_FarWallCheck.localPosition.x), m_FarWallCheck.localPosition.y, 0);
    }

    IEnumerator WaitToDead()
    {
        //animator.SetBool("IsDead", true);
        //AudioManager.instance.FadeOutCurrent();
        canMove = false;
        invincible = true;
        dead = true;
        GetComponent<Attack>().enabled = false;
        yield return new WaitForSecondsRealtime(0.4f);
        m_Rigidbody2D.velocity = new Vector2(0, m_Rigidbody2D.velocity.y);
        //GameObject.Find("Crossfade").GetComponent<Animator>().SetTrigger("start");
        //StartCoroutine(am.PitchDown());
        yield return new WaitForSecondsRealtime(0.8f);
      //  AudioManager.instance.PauseCurrent();
        yield return new WaitForSecondsRealtime(0.2f);
        SceneManager.LoadScene(GetComponent<Spawnpoint>().scene);
        if (GetComponent<Spawnpoint>().scene == "1stScene")
        {
           // initialFall = false;
        }
        // SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        dead = false;
        //animator.SetBool("IsDead", false);
        //canMove = true;
        invincible = false;
        // transform.position = GameObject.Find("PlayerCheck").transform.position;
        transform.position = GetComponent<Spawnpoint>().position;
        // TODO may want to change these depending on if we have health boost effects
        GetComponent<health>().playerHealth = 5;
        GetComponent<health>().numberOfHearts = 5;
        GetComponent<Attack>().enabled = true;
        yield return new WaitForSecondsRealtime(1f);
       // AudioManager.instance.UnPauseCurrent();
        canMove = true;
    }

    public void GoToResetPoint()
    {
        if (this.GetComponent<health>().playerHealth > 0)
        {
            if (resetting || dead)
            {
                return;
            }
            StartCoroutine(ResetPoint());
        }
    }

    IEnumerator ResetPoint()
    {
       // animator.SetBool("IsDead", true);
        m_Rigidbody2D.velocity = new Vector2(0, m_Rigidbody2D.velocity.y);
        resetting = true;
        StartCoroutine(WaitToMove(1));
        GameObject.Find("Crossfade").GetComponent<Animator>().SetTrigger("start");
        yield return new WaitForSecondsRealtime(1f);
        GameObject.Find("Crossfade").GetComponent<Animator>().SetTrigger("stop");
       // animator.SetBool("IsDead", false);
        transform.position = reset_point.position;
        FindObjectOfType<CameraFollow>().Snap(transform.position);
        m_Rigidbody2D.velocity = Vector2.zero;
        resetting = false;
        yield return new WaitForSecondsRealtime(0.31f);
        StartCoroutine(MakeInvincible(3f));
        GetComponent<SimpleFlash>().Flash(3f, 7);
    }

    public void LandParticles()
    {
        particleLand.Play();
    }

    public void PlaySound(AudioClip clip)
    {
        foreach (AudioSource source in sources)
        {
            if (source.clip == clip && source.isPlaying)
            {
                if (source.time < 0.2f && source.isPlaying) return;
                else source.Stop();
            }
        }
        for (int index = sources.Length - 1; index >= 0; index--)
        {
            if (!sources[index].isPlaying)
            {
                sources[index].clip = clip;
                sources[index].loop = false;
                sources[index].Play();
                return;
            }
        }
    }

    void CleanSounds()
    {
        foreach (AudioSource source in sources)
        {
            if (!source.isPlaying)
            {
                source.Stop();
                source.clip = null;
                source.time = 0;
            }
        }
    }

}
