using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    //public Animator animator;
    public static bool paused = false;
   // public GameObject pauseOverlay;

    public float runSpeed = 40f;

    float horizontalMove = 0f;
    bool jump = false;
    bool releaseJump = false;
    bool dash = false;

    public bool dash_Unlocked = false;

    // Update is called once per frame
    void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown("joystick button 7")))
        {
            //TogglePause();
        }

        if (!paused)
        {
            if (Input.GetAxisRaw("Horizontal") > 0.3 || Input.GetAxisRaw("Horizontal") < -0.3)
                horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;
            else
                horizontalMove = 0f;

            if (Player.controller.dead || Player.controller.resetting)
            {
               // animator.SetBool("IsDead", true);
                horizontalMove = 0f;
            }

            if (!Player.controller.canMove)
                horizontalMove = 0f;

            //animator.SetFloat("Speed", Mathf.Abs(horizontalMove));

            if (Input.GetButtonDown("Jump"))
            {
                if (((Player.controller.m_IsFarWall || Player.controller.isWallSliding) && Player.controller.wallSlide_Unlocked) || Player.controller.beenOnLand >= 0.05f || Player.controller.lastOnLand < 0.15f || Player.controller.canDoubleJump)
                    jump = true;

                // allows player with double jump to jump after walking off ledge
                if (!jump && !Player.controller.isJumping && !Player.controller.isJumpingDJ && Player.controller.doubleJump_Unlocked && Player.controller.lastOnLand > 0.01f)
                {
                    jump = true;
                    Player.controller.isJumpingDJ = true;
                    Player.controller.canDoubleJump = true;
                }

                // fixes multi double jumping on rock platforms
                if (Player.controller.isJumpingDJ && !Player.controller.canDoubleJump)
                    jump = false;
            }

            if (Input.GetButton("Jump") && Player.controller.m_Grounded)
            {
                if (Player.controller.jumpCooldown <= 0f && !Player.controller.isJumping && !Player.controller.isJumpingDJ)
                    jump = true;

            }

            if (Input.GetButtonUp("Jump"))
            {
                releaseJump = true;
                Player.controller.jumpCooldown = 0f;
                Player.controller.m_Rigidbody2D.velocity = new Vector2(Player.controller.m_Rigidbody2D.velocity.x, 0);
            }

            if ((Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetAxisRaw("LeftTrigger") > 0.3) && dash_Unlocked == true)
            {
                dash = true;
            }

         
        }
    }

    public void OnFall()
    {
        //animator.SetBool("IsJumping", true);
    }

    public void OnLanding()
    {
        // this might have caused more sprite flickering
        if (!Player.controller.isJumping && !Player.controller.isJumpingDJ)
        {
            //animator.SetBool("IsJumping", false);
        }
    }

    //public void TogglePause()
    //{
    //    CanvasToggle canv = FindObjectOfType<CanvasToggle>();

    //    if (!paused)
    //    {
    //       // pauseOverlay.SetActive(true);
    //        paused = true;
    //        Time.timeScale = 0;
    //        Cursor.visible = true;
    //        canv.Pause();
            
    //    }
    //    else
    //    {
    //       // pauseOverlay.SetActive(false);
    //        paused = false;
    //        Time.timeScale = 1;
    //        Cursor.visible = false;
    //        canv.UnPause();
            
    //    }
    //}
    void FixedUpdate()
    {
        // Move our character
        Player.controller.Move(horizontalMove * Time.fixedDeltaTime, jump, dash, releaseJump);
        //if (animator.GetBool("IsJumping") && Player.controller.m_Grounded && !Player.controller.isJumping && !Player.controller.isJumpingDJ)
        //{
        //    animator.SetBool("IsJumping", false);
        //}
        jump = false;
        dash = false;
        releaseJump = false;
    }
}
