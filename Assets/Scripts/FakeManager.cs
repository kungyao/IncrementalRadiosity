using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FakeManager : MonoBehaviour
{
    public MySpot mySpot;
    public GameObject sibenik;

    public void SetRotate()
    {
        mySpot.ifRotate = !mySpot.ifRotate;
    }
    public void LoadSibe()
    {
        SceneManager.LoadScene(1, LoadSceneMode.Additive);
        //GameObject.Instantiate(sibenik);
        //mySpot.Initialize();
    }
    public void LightInitialize()
    {
        mySpot.Initialize();
    }
}
