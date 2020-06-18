using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Delaunay;
using Delaunay.Geo;

public class UniformCircle
{
    static private Vector2 calculatePoint(float radius, bool doOffset, bool isUniform)
    {
        float angle = Random.Range(0.0f, 1.0f) * Mathf.PI * 2;
        float r = (isUniform ? Mathf.Sqrt(Random.Range(0.0f, 1.0f)) : Random.Range(0.0f, 1.0f)) * radius;
        return new Vector2(
            r * Mathf.Cos(angle) + (doOffset ? radius : 0),
            r * Mathf.Sin(angle) + (doOffset ? radius : 0));
    }
    static public List<Vector2> calculatePoint(float radius, int samples, bool doOffset, bool isUniform)
    {
        //Dictionary<Vector2, int> dict = new Dictionary<Vector2, int>();
        List<Vector2> sp = new List<Vector2>();
        for (int i = 0; i < samples; i++) 
        {
            Vector2 tmp = calculatePoint(radius, doOffset, isUniform);
            //while (dict.ContainsKey(tmp))
            //{
            //    tmp = calculatePoint(radius, doOffset, isUniform);
            //}
            sp.Add(tmp);
            //dict[tmp] = 1;
        }
        return sp;
    }
    static private float Halton(int index, int b)
    {
        float result = 0;
        float f = 1.0f / (float)b;
        float i = index;
        while (i > 0)
        {
            result = result + f * (i % b);
            i = Mathf.Floor(i / b);
            f = f / b;
        }
        return result;
    }
    static public List<Vector2> HaltonGenerator(int samples, int basex = 0, int basey = 0)
    {
        List<Vector2> points = new List<Vector2>();
        // 2, 3 Halton Sequence by default
        if (basex == 0)
            basex = 2;
        if (basey == 0)
            basey = 3;
        int index = 20;
        for (int i = 0; i < samples; i++)
        {
            Vector2 p = new Vector2(Halton(index, basex) * 2 - 1, Halton(index, basey) * 2 - 1);
            if (p.magnitude < 1.0f)
                points.Add(p);
            else 
                i--;
            index++;
        }
        return points;
    }
}

class MyTri {
    public Vector2 center = Vector2.zero;
    public float area = 0;
    public MyTri(Vector2 _center, float _area)
    {
        center = _center;
        area = _area;
    }
};
class MyEdge {
    public float dis = 0;
    public int index = 0;
    public MyEdge(float _dis, int _index)
    {
        dis = _dis;
        index = _index;
    }
};
public class VPLUtil{
    // 如果有做位移 radius要乘2 !!!!!!!!!!!!
    static public List<Vector2> recalculateVPL(List<Vector2> localSps, float radius, bool doOffset, out int updateIndex, out List<bool> removeIndex)
    {
        //float maxDisToRemove = radius / localSps.Count;
        float maxDisToRemove = float.MaxValue;

        if (doOffset)
            radius *= 2;

        Voronoi v = new Voronoi(localSps, null, new Rect(0, 0, radius, radius), out removeIndex);

        List<Edge> edges = v._edges;
        int shotestEdgeIndex = -1;
        // for remove
        float minDis = maxDisToRemove;
        for (int i = 0; i < edges.Count; i++)
        {
            Edge edge = edges[i];
            float dis = edge.SitesDistance();
            if (dis < minDis)
            {
                minDis = dis;
                shotestEdgeIndex = i;
            }
        }

        updateIndex = -1;
        if (shotestEdgeIndex != -1)
        {
            Edge edge = edges[shotestEdgeIndex];
            // 1 left / 2 right
            int shortestIndex = -1;
            minDis = float.MaxValue;
            for (int i = 0; i < edge.rightSite.edges.Count; i++)
            {
                if (Edge.isSameEdge(edge, edge.rightSite.edges[i]))
                    continue;
                float dis = edge.rightSite.edges[i].SitesDistance();
                if (dis < minDis)
                {
                    minDis = dis;
                    shortestIndex = 1;
                }
            }
            for (int i = 0; i < edge.leftSite.edges.Count; i++)
            {
                if (Edge.isSameEdge(edge, edge.leftSite.edges[i]))
                    continue;
                float dis = edge.leftSite.edges[i].SitesDistance();
                if (dis < minDis)
                {
                    minDis = dis;
                    shortestIndex = 2;
                }
            }

            if (shortestIndex == 1)
            {
                shortestIndex = (int)edge.rightSite._siteIndex;
            }
            else
            {
                shortestIndex = (int)edge.leftSite._siteIndex;
            }

            updateIndex = shortestIndex;

            // for add
            int maxAreaTriangle = -1;
            float maxArea = 0.0f;
            Vector2 newVPL = Vector2.zero;
            List<Triangle> triangles = v._triangles;
            bool isReal = true;
            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle tri = triangles[i];
                if (tri.HasSite(shortestIndex))
                    continue;
                float tmpArea = tri.Area();
                if (tmpArea > maxArea)
                {
                    newVPL = triangles[i].CircumcircleCenter(out isReal);
                    if (!isReal)
                        continue;
                    maxArea = tmpArea;
                    maxAreaTriangle = i;
                }
            }

            List<Site> siteList = v._sites._sites;
            List<Vector2> newPoint = new List<Vector2>();
            for (int i = 0; i < siteList.Count; i++)
            {
                if (i == shortestIndex)
                {
                    newPoint.Add(newVPL);
                    //Debug.Log(siteList[i] + " " + newVPL);
                }
                else
                {
                    newPoint.Add(siteList[i].Coord);
                }
            }
            return newPoint;
        }
        else
        {
            return localSps;
        }
    }

    // 如果有做位移 radius要乘2 !!!!!!!!!!!!
    // @param samples       不合法的vpl，需要被增加回去的數量
    // @param resamples     撇除不合法vpl，每次固定要被移除的量，根據voronoi面積決定
    static public List<Vector2> recalculateVPL(List<Vector2> localSps, float radius, bool doOffset, int samples, int resamples, out List<int> updateIndex)
    {
        if (doOffset)
            radius *= 2;

        Voronoi v = new Voronoi(localSps, null, new Rect(0, 0, radius, radius), out List<bool> removeIndex);
        List<int> emptyIndex = new List<int>();
        for (int i = 0; i < removeIndex.Count; i++) 
        {
            if (removeIndex[i])
                emptyIndex.Add(i);
        }
        for (int i = 0; i < samples; i++)
        {
            emptyIndex.Add(localSps.Count + i);
        }
        emptyIndex.Sort((a, b) => a.CompareTo(b));

        List<int> resampleIndex = new List<int>();
        if (resamples > 0)
        {
            List<Edge> edges = v._edges;
            List<MyEdge> myEdges = new List<MyEdge>();
            // for remove
            for (int i = 0; i < edges.Count; i++)
            {
                myEdges.Add(new MyEdge(edges[i].SitesDistance(), i));
            }
            // 從小到大
            myEdges.Sort((a, b) => a.dis.CompareTo(b.dis));

            for(int i = 0; i < resamples; i++)
            {
                Edge edge = edges[myEdges[i].index];
                // 1 left / 2 right
                int shortestIndex = -1;
                float minDis = float.MaxValue;
                for (int j = 0; j < edge.rightSite.edges.Count; j++)
                {
                    if (Edge.isSameEdge(edge, edge.rightSite.edges[j]))
                        continue;
                    float dis = edge.rightSite.edges[j].SitesDistance();
                    if (dis < minDis)
                    {
                        minDis = dis;
                        shortestIndex = 1;
                    }
                }
                for (int j = 0; j < edge.leftSite.edges.Count; j++)
                {
                    if (Edge.isSameEdge(edge, edge.leftSite.edges[j]))
                        continue;
                    float dis = edge.leftSite.edges[j].SitesDistance();
                    if (dis < minDis)
                    {
                        minDis = dis;
                        shortestIndex = 2;
                    }
                }

                if (shortestIndex == 1)
                {
                    shortestIndex = (int)edge.rightSite._siteIndex;
                }
                else
                {
                    shortestIndex = (int)edge.leftSite._siteIndex;
                }

                resampleIndex.Add(shortestIndex);
            }
            resampleIndex.Sort((a, b) => a.CompareTo(b));

            List<Vector2> tmpNewPoint = new List<Vector2>();
            for (int i = 0; i < v._sites._sites.Count; i++)
            {
                tmpNewPoint.Add(v._sites._sites[i].Coord);
            }
            int oft = 0;
            for (int i = 0; i < resampleIndex.Count; i++)
            {
                if (i != 0)
                {
                    if (resampleIndex[i] == resampleIndex[i - 1])
                    {
                        resampleIndex.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
                if (resampleIndex[i] - oft >= tmpNewPoint.Count) 
                {
                    resampleIndex.RemoveAt(i);
                    i--;
                    continue;
                }
                tmpNewPoint.RemoveAt(resampleIndex[i] - oft);
                oft++;
            }
            v = new Voronoi(tmpNewPoint, null, new Rect(0, 0, radius, radius), out List<bool> nouse);
        }

        updateIndex = new List<int>();
        updateIndex.AddRange(emptyIndex);
        updateIndex.AddRange(resampleIndex);
        if (updateIndex.Count != 0)
        {
            updateIndex.Sort((a, b) => a.CompareTo(b));
            List<MyTri> maxAreaTriangles = new List<MyTri>();
            List<Triangle> triangles = v._triangles;
            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle tri = triangles[i];
                Vector2 tmpVPL = triangles[i].CircumcircleCenter(out bool isReal);
                if (!isReal)
                    continue;
                maxAreaTriangles.Add(new MyTri(tmpVPL, tri.Area()));
            }

            maxAreaTriangles.Sort((t1, t2) => t2.area.CompareTo(t1.area));

            List<Site> siteList = v._sites._sites;
            List<Vector2> newPoint = new List<Vector2>();
            for (int i = 0; i < siteList.Count; i++) 
            {
                newPoint.Add(siteList[i].Coord);
            }
            for (int i = 0; i < resampleIndex.Count; i++) 
            {
                newPoint.Insert(resampleIndex[i], maxAreaTriangles[i].center);
            }
            int j = resampleIndex.Count;
            for (int i = 0; i < emptyIndex.Count; i++)
            {
                newPoint.Insert(emptyIndex[i], maxAreaTriangles[j + i].center);
            }
            return newPoint;
        }
        return localSps;
    }
    static public List<float> getLightIntensity(List<Vector2> localSp, float radius, bool doOffset)
    {
        if (doOffset)
            radius *= 2;

        Voronoi voronoi = new Voronoi(localSp, null, new Rect(0, 0, radius, radius), out List<bool> removeIndex);
        List<Site> siteList = voronoi._sites._sites;
        List<float> areas = new List<float>();
        float totalArea = 0;
        foreach (Site site in siteList)
        {
            List<Vector2> vertices = new List<Vector2>();
            for (int i = 0; i < site.edges.Count; i++) 
            {
                Edge edge = site.edges[i];
                if (edge.visible)
                {
                    LineSegment lineSeg = edge.VoronoiEdge();
                    if (lineSeg.p0 == null || lineSeg.p1 == null)
                        continue;
                    vertices.Add(lineSeg.p0.Value);
                }
            }
            Polygon voronoiRegion = new Polygon(vertices);
            float tmpArea = voronoiRegion.Area();
            areas.Add(tmpArea);
            totalArea += tmpArea;
        }

        if (totalArea == 0)
        {
            return null;
        }
        
        List<float> intensityList = new List<float>();
        for (int i = 0, j = 0; i < removeIndex.Count; i++) 
        {
            if (removeIndex[i])
            {
                intensityList.Add(0);
            }
            else
            {
                intensityList.Add(areas[j] / totalArea);
                j++;
            }
        }
        return intensityList;
    }
}
