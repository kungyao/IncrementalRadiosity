using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        GameObject.Instantiate(sibenik);
        mySpot.Initialize();
    }
}
