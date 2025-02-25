using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class health : MonoBehaviour
{
    public int playerHealth;

    public int numberOfHearts;

    public GameObject[] hearts;
    //public Sprite fullHeart;
    //public Sprite emptyHeart;
    // Start is called before the first frame update

    public Player player;
    public Attack attack;
    void Start()
    {
        //hearts = GameObject.FindGameObjectsWithTag("Heart");
        //Array.Sort(hearts, CompareObNames);
    }

    // Update is called once per frame
    void Update()
    {
        

        if (playerHealth > numberOfHearts)
        {
            playerHealth = numberOfHearts;
        }

        //for (int i = 0; i < hearts.Length; i++)
        //{

        //    if (i < playerHealth)
        //    {
        //        //if they have at least three health, then the other two must be full
        //        hearts[i].GetComponent<Image>().sprite = fullHeart;
        //    }
        //    else
        //    {
        //        //hearts more then the heath they have need to be empty
        //        //hearts[i].GetComponent<Image>().sprite = emptyHeart;
        //    }

        //    if (i < numberOfHearts)
        //    {
        //        hearts[i].GetComponent<Image>().enabled = true;
        //    }
        //    else
        //    {
        //        hearts[i].GetComponent<Image>().enabled = false;
        //    }
        //}
    }

    public void takeAwayHeart()
    {
        playerHealth -= 1;
        player.doubleJump_Unlocked = false;
        if(playerHealth <= 1)
        {
            attack.dmgValue = 10;
        }
        //print(playerHealth);
        hearts[playerHealth].GetComponent<Image>().enabled = false;
        if(playerHealth <= 0)
        {
            SceneManager.LoadScene("Main_Menu");
        }
    }

    int CompareObNames(GameObject x, GameObject y)
    {
        return x.name.CompareTo(y.name);
    }
}
