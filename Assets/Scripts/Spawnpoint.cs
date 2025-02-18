using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Spawnpoint : MonoBehaviour
{
    // Start is called before the first frame update
    public string scene;
    public Vector2 position;
    public List<int> statuesUsed;

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetSpawnpoint(Transform statue, int ID)
    {
        scene = SceneManager.GetActiveScene().name;
        position = statue.position;
        if (!statuesUsed.Contains(ID))
            statuesUsed.Add(ID);
    }
}
