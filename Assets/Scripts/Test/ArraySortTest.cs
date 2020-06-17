using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArraySortTest : MonoBehaviour
{
    void Start()
    {
        List<int> tmp = new List<int>();
        for(int i = 0; i < 10; i++)
        {
            tmp.Add(i);
        }
        tmp.Sort((a, b) => b.CompareTo(a));
        for (int i = 0; i < 10; i++)
        {
            print(tmp[i]);
        }
    }
}
