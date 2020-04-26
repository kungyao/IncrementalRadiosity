using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GenerateCircle : MonoBehaviour
{
    public int sampleSize = 1000;
    public float radius = 1.0f;
    public float cubeSize = 1.0f;
    public bool isUniform = false;
    public bool showCircle = false;
    public bool doOffset = true;

    private List<Vector2> localSps = new List<Vector2>();

    private void Start()
    {
        localSps = UniformCircle.calculatePoint(radius, sampleSize, doOffset, isUniform);
    }

    private void Update()
    {
        // do reproject to list localSp
        // localSps = UniformCircle.calculatePoint(radius, sampleSize, doOffset, isUniform);
        //int index = -1;
        //localSps = VPLUtil.recalculateVPL(localSps, radius, doOffset, out index);
    }

    private void OnDrawGizmos()
    {
        if (showCircle)
        {
            foreach (Vector2 sp in localSps)
            {
                //print(sp);
                Gizmos.DrawCube(transform.position + new Vector3(sp.x, sp.y, 0), Vector3.one * cubeSize);
            }
        }
    }
}
