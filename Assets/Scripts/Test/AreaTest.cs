using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Delaunay.Geo;

[ExecuteInEditMode]
public class AreaTest : MonoBehaviour
{
    public List<Transform> trans = new List<Transform>();

    void Update()
    {
        if (trans.Count > 2)
        {
            List<Vector2> vertices = new List<Vector2>();
            foreach (Transform tran in trans)
            {
                vertices.Add(new Vector2(tran.position.x, tran.position.y));
            }
            Polygon voronoiRegion = new Polygon(vertices);
            print(voronoiRegion.Area());
        }
    }
}
