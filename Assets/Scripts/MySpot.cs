using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Delaunay;
using Delaunay.Geo;

public class MySpot : MonoBehaviour
{
    public enum LightType
    {
        PointType = 0, 
        SpotType = 1,
    }

    public LightType lightType;
    public float radius = 10;
    public int minVPLSize = 50;
    public int vplSzie = 256;
    public float intensityScale = 10f;
    public GameObject preLight;

    public bool doOffset = true;
    public float rayOffset = 2.0f;

    public bool ifRotate = false;
    public float rotateAngle = 10.0f;

    public float visualLightScale = 1.5f;
    private List<Vector3> realPointPos = new List<Vector3>();
    private List<GameObject> pointLightObjects = new List<GameObject>();
    private Dictionary<GameObject, Light> pointLights = new Dictionary<GameObject, Light>();
    private List<Vector3> surfacePoints = new List<Vector3>();

    private bool isInit = false;
    public void Initialize()
    {
        if (preLight)
        {
            Vector3 normal = transform.forward;
            List<Vector2> localSp = UniformCircle.calculatePoint(radius, vplSzie, doOffset, true);
            for (int i = 0; i < vplSzie; i++)
            {
                generateNewLight(localSp[i]);
            }
        }
        isInit = true;
    }

    Vector3 ToCircleSurface(Vector2 pos2D, Vector3 normal, float r)
    {
        if (doOffset)
        {
            pos2D = new Vector2(pos2D.x - r, pos2D.y - r);
        }
        Matrix4x4 ltw = transform.localToWorldMatrix;
        Vector3 w = ltw.MultiplyPoint(new Vector3(pos2D.x, pos2D.y, 0));
        return w + normal * Mathf.Sqrt(r * r - pos2D.sqrMagnitude);
    }

    Vector2 To2DDomain(Vector3 pos, float r)
    {
        Matrix4x4 wtl = transform.worldToLocalMatrix;
        Vector3 localPos = wtl.MultiplyPoint(pos);
        localPos = localPos.normalized * r;
        Vector2 pos2D = new Vector2(localPos.x, localPos.y);
        if (doOffset)
        {
            pos2D = new Vector2(pos2D.x + r, pos2D.y + r);
        }
        return pos2D;
    }

    bool CheckValid(int index)
    {
        // GameObject obj = pointLightObjects[index];
        Vector3 objPos = realPointPos[index];
        Vector3 rayOriToLight = objPos - transform.position;
        if (Vector3.Dot(transform.forward, rayOriToLight) <= 0)
            return false;
        RaycastHit hit;
        if (Physics.Raycast(objPos, -rayOriToLight, out hit, rayOriToLight.magnitude))
        {
            return false;
        }
        return true;
    }

    void CheckValid()
    {
        for (int i = 0; i < pointLightObjects.Count; i++)
        {
            if (!CheckValid(i))
            {
                removeLight(i);
                i = i - 1;
            }
        }
    }

    bool updateObjectPotition(GameObject obj, Vector3 ori, Vector2 localPos, bool isNew)
    {
        RaycastHit hit;
        Vector3 normal = transform.forward;
        Vector3 surfacePoint = ToCircleSurface(localPos, normal, radius);
        Vector3 rayDir = surfacePoint - transform.position;
        if (Physics.Raycast(ori, rayDir, out hit, 1000))
        {
            ori = hit.point - rayOffset * rayDir;
            obj.transform.position = ori;
            if (isNew)
            {
                realPointPos.Add(hit.point);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    void removeLight(int index)
    {
        GameObject obj = pointLightObjects[index];
        realPointPos.RemoveAt(index);
        pointLights.Remove(obj);
        pointLightObjects.RemoveAt(index);
        Destroy(obj);
    }

    void generateNewLight(Vector2 localPos)
    {
        GameObject tmp = GameObject.Instantiate(preLight);
        // surfacePoints.Add(surfacePoint);
        if (updateObjectPotition(tmp, transform.position, localPos, true))
        {
            pointLightObjects.Add(tmp);
            pointLights[tmp] = tmp.transform.GetComponent<Light>();
        }
        else
        {
            Destroy(tmp);
        }
    }

    List<Vector2> LightPosTo2DSpace()
    {
        List<Vector2> spcae2Dpos = new List<Vector2>();
        for (int i = 0; i < pointLightObjects.Count; i++)
        {
            spcae2Dpos.Add(To2DDomain(pointLightObjects[i].transform.position, radius));
        }
        return spcae2Dpos;
    }

    void UpdatePointLight()
    {
        CheckValid();
        int updateIndex = -1;
        List<Vector2> posList2D = LightPosTo2DSpace();
        //紀錄是否被成功加入
        List<bool> removeIndex = new List<bool>();
        List<Vector2> localSp = VPLUtil.recalculateVPL(posList2D, radius, doOffset, out updateIndex, out removeIndex);

        int oft = 0;
        for (int i = 0; i < removeIndex.Count; i++) 
        {
            if (!removeIndex[i])
            {
                removeLight(i - oft);
                oft++;
            }
        }
        // updateLightObjectAndRemove(localSp, updateIndex);
        if (updateIndex != -1)
        {
            //print("AAA    " + pointLightObjects[updateIndex].transform.position);
            if(!updateObjectPotition(pointLightObjects[updateIndex], transform.position, localSp[updateIndex], false))
            {
                removeLight(updateIndex);
                localSp.RemoveAt(updateIndex);
            }
            //print("BBB    " + pointLightObjects[updateIndex].transform.position);
        }

        List<Vector2> newVP = UniformCircle.calculatePoint(radius, vplSzie - pointLightObjects.Count, doOffset, true);
        foreach (Vector2 item in newVP)
        {
            localSp.Add(item);
            generateNewLight(item);
        }

        // print(updateIndex + "   " + pointLightObjects.Count + " " + pointLights.Count + " " + localSp.Count);
        List<float> intensity = VPLUtil.getLightIntensity(localSp, radius, doOffset);
        if (intensity != null)
        {
            for (int i = 0; i < intensity.Count; i++)
            {
                pointLights[pointLightObjects[i]].intensity = intensity[i] * intensityScale;
            }
        }
        else
        {
            for (int i = 0; i < pointLightObjects.Count; i++)
            {
                pointLights[pointLightObjects[i]].intensity = 0.5f * intensityScale;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isInit)
        {
            UpdatePointLight();
            if (ifRotate)
            {
                transform.RotateAround(transform.position, transform.up, rotateAngle * Time.deltaTime);
            }
        }
    }

    private void OnDrawGizmos()
    {
        foreach (Vector3 pos in realPointPos)
        {
            Gizmos.DrawWireSphere(pos, visualLightScale);
        }
        //List<Vector2> posList2D = LightPosTo2DSpace();

        //Gizmos.color = Color.red;
        //for (int i = 0; i < posList2D.Count; i++)
        //{
        //    Gizmos.DrawSphere(posList2D[i], 0.2f);
        //}
        //float tmpRadius = radius;
        //if (doOffset)
        //    tmpRadius *= 2;
        //Voronoi voronoi = new Voronoi(posList2D, null, new Rect(0, 0, tmpRadius, tmpRadius));
        //List<LineSegment> m_edges = voronoi.VoronoiDiagram();
        //Gizmos.color = Color.white;
        //for (int i = 0; i < m_edges.Count; i++)
        //{
        //    Vector2 left = (Vector2)m_edges[i].p0;
        //    Vector2 right = (Vector2)m_edges[i].p1;
        //    Gizmos.DrawLine((Vector3)left, (Vector3)right);
        //}

        //Gizmos.color = Color.magenta;
        //List<LineSegment> m_delaunayTriangulation = voronoi.DelaunayTriangulation();
        //for (int i = 0; i < m_delaunayTriangulation.Count; i++)
        //{
        //    Vector2 left = (Vector2)m_delaunayTriangulation[i].p0;
        //    Vector2 right = (Vector2)m_delaunayTriangulation[i].p1;
        //    Gizmos.DrawLine((Vector3)left, (Vector3)right);
        //}
        //List<Triangle> triangles = voronoi._triangles;
        //for (int i = 0; i < triangles.Count; i++)
        //{
        //    Triangle tri = triangles[i];
        //    Gizmos.DrawLine((Vector3)tri.sites[0].Coord, (Vector3)tri.sites[1].Coord);
        //    Gizmos.DrawLine((Vector3)tri.sites[1].Coord, (Vector3)tri.sites[2].Coord);
        //    Gizmos.DrawLine((Vector3)tri.sites[2].Coord, (Vector3)tri.sites[0].Coord);
        //    List<Vector2> vvv = new List<Vector2>();
        //    vvv.Add(tri.sites[0].Coord);
        //    vvv.Add(tri.sites[1].Coord);
        //    vvv.Add(tri.sites[2].Coord);
        //    Polygon area = new Polygon(vvv);
        //    print(area.Area());
        //}
        //surfacePoints.Clear();
        //Vector3 normal = transform.forward;
        //foreach (Vector2 item in posList2D)
        //{
        //    surfacePoints.Add(ToCircleSurface(item, normal, radius));
        //}
        //foreach (Vector3 item in surfacePoints)
        //{
        //    print(item);
        //    Gizmos.DrawLine(transform.position, item);
        //}
    }
}
