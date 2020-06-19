using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Delaunay;
using Delaunay.Geo;
// using System.Numerics;

public class MySpot : MonoBehaviour
{
    public enum LightType
    {
        PointType = 0, 
        SpotType = 1,
    }
    public class LightPath
    {
        public enum Type {
            None = 0,
            Reflection = 1,
            Refraction = 2,
        }
        public class HitPoint
        {
            public Vector3 pos = Vector3.zero;
            public Type type = Type.None;
            public HitPoint(Vector3 p, Type t)
            {
                pos = p;
                type = t;
            }
        };
        public List<HitPoint> hit = new List<HitPoint>();
        public HitPoint Front
        {
            get
            {
                if (hit.Count == 0)
                    return null;
                return hit[0];
            }
        }
        public HitPoint Back
        {
            get
            {
                if (hit.Count == 0)
                    return null;
                return hit[hit.Count - 1];
            }
        }
        public int Size
        {
            get
            {
                return hit.Count;
            }
        }
        public HitPoint this[int index]
        {
            get
            {
                if (index >= hit.Count || index < 0) 
                {
                    print("Index Out Of Range!");
                    return null;
                }

                return hit[index];
            }
        }
        public LightPath(Vector3 firstHit)
        {
            hit.Add(new HitPoint(firstHit, Type.None));
        }
        public void Add(Vector3 p, Type t)
        {
            hit.Add(new HitPoint(p, t));
        }
        public void Clear()
        {
            hit.Clear();
        }
    };

    public LightType lightType;
    public float radius = 10;
    public int vplSzie = 256;
    public int resampleVPLs = 10;
    public float intensityScale = 10f;
    public GameObject preLight;

    public bool doOffset = true;
    public float rayOffset = 2.0f;

    public bool ifRotate = false;
    public float rotateAngle = 10.0f;

    public float visualLightScale = 1.5f;
    private List<LightPath> realPointPos = new List<LightPath>();
    private List<GameObject> pointLightObjects = new List<GameObject>();
    private Dictionary<GameObject, Light> pointLights = new Dictionary<GameObject, Light>();

    private bool isInit = false;
    public void Initialize()
    {
        if (preLight)
        {
            realPointPos = new List<LightPath>();
            if(pointLightObjects != null)
            {
                foreach (GameObject obj in pointLightObjects)
                    Destroy(obj);
                pointLightObjects = new List<GameObject>();
            }
            pointLights = new Dictionary<GameObject, Light>();
            Vector3 normal = transform.forward;
            //List<Vector2> localSp = UniformCircle.calculatePoint(radius, vplSzie, doOffset, true);
            // 已經offset
            List<Vector2> localSp = UniformCircle.HaltonGenerator(radius, vplSzie, doOffset);
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
        Vector3 objPos = realPointPos[index].Front.pos;
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
    /**
     * @param normal    previous hit normal
     * @param pos       previous hit pos
     * @param type      previous hit material (reflection or refraction)
     */
    Vector3 updateObjectPotition_sup(Vector3 viewDir, Vector3 normal, Vector3 pos, LightPath.Type type, LightPath path)
    {
        Vector3 o = Vector3.zero;
        if (type == LightPath.Type.Reflection) o = RotationFromNormal.VectorRotateAroundAxis(-viewDir, normal, 180);
        else if (type == LightPath.Type.Refraction) o = RotationFromNormal.RefractRayDirection(viewDir, normal, 0.6666f, RotationFromNormal.Space.AirTo);
        if (Physics.Raycast(pos, o, out RaycastHit hit, 1000))
        {
            LightPath.Type tp = LightPath.Type.None;
            if (hit.transform.tag == "ReflectionMaterial")
            {
                tp = LightPath.Type.Reflection;
            }
            else if (hit.transform.tag == "RefractionMaterial")
            {
                tp = LightPath.Type.Refraction;
            }
            Vector3 finalLightPos = hit.point - rayOffset * viewDir;
            path.Add(finalLightPos, tp);
            if (tp != LightPath.Type.None)
            {
                return updateObjectPotition_sup(o, hit.normal, finalLightPos, tp, path);
            }
            return finalLightPos;
        }
        Debug.Log("Reflection/Refraction ray do not hit anything !");
        return pos - rayOffset * viewDir;
    }
    bool updateObjectPotition(GameObject obj, Vector3 ori, Vector2 localPos, bool isNew, int index = -1)
    {
        Vector3 normal = transform.forward;
        Vector3 surfacePoint = ToCircleSurface(localPos, normal, radius);
        Vector3 rayDir = surfacePoint - transform.position;
        if (Physics.Raycast(ori, rayDir, out RaycastHit hit, 1000))
        {
            Vector3 finalLightPos = hit.point - rayOffset * rayDir;
            LightPath lightPath = null;
            if (isNew)
            {
                lightPath = new LightPath(finalLightPos);
                realPointPos.Add(lightPath);
            }
            else
            {
                lightPath = realPointPos[index];
                lightPath.Clear();
                lightPath.Add(finalLightPos, LightPath.Type.None);
            }
            if (hit.transform.tag == "ReflectionMaterial")
            {
                finalLightPos = updateObjectPotition_sup(rayDir, hit.normal, hit.point, LightPath.Type.Reflection, lightPath);
            }
            else if (hit.transform.tag == "RefractionMaterial")
            {
                finalLightPos = updateObjectPotition_sup(rayDir, hit.normal, hit.point, LightPath.Type.Refraction, lightPath);
            }
            obj.transform.position = finalLightPos;
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

    bool generateNewLight(Vector2 localPos)
    {
        GameObject tmp = GameObject.Instantiate(preLight);
        // surfacePoints.Add(surfacePoint);
        if (updateObjectPotition(tmp, transform.position, localPos, true))
        {
            pointLightObjects.Add(tmp);
            pointLights[tmp] = tmp.transform.GetComponent<Light>();
            return true;
        }
        else
        {
            Destroy(tmp);
            return false;
        }
    }

    List<Vector2> LightPosTo2DSpace()
    {
        List<Vector2> spcae2Dpos = new List<Vector2>();
        for (int i = 0; i < realPointPos.Count; i++)
        {
            spcae2Dpos.Add(To2DDomain(realPointPos[i].Front.pos, radius));
        }
        return spcae2Dpos;
    }

    void UpdatePointLight()
    {
        // 檢查並刪除不合法光
        CheckValid();
        List<Vector2> posList2D = LightPosTo2DSpace();
        List<Vector2> localSp = VPLUtil.recalculateVPL(posList2D, radius, doOffset, vplSzie - pointLightObjects.Count, resampleVPLs, out List<int> updateIndex);

        //print("A  " + updateIndex.Count + "   " + pointLightObjects.Count + " " + pointLights.Count + " " + localSp.Count);
        if (updateIndex.Count != 0)
        {
            int offset = 0;
            foreach (int index in updateIndex)
            {
                int ind = index - offset;
                if (ind >= pointLightObjects.Count)
                {
                    if (!generateNewLight(localSp[ind]))
                    {
                        localSp.RemoveAt(ind);
                        offset++;
                    }
                }
                else if (!updateObjectPotition(pointLightObjects[ind], transform.position, localSp[ind], false, ind))
                {
                    removeLight(ind);
                    localSp.RemoveAt(ind);
                    offset++;
                }
            }
        }
        //print("B  " + updateIndex.Count + "   " + pointLightObjects.Count + " " + pointLights.Count + " " + localSp.Count);
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
        Gizmos.color = Color.white;
        foreach (LightPath path in realPointPos)
        {
            for(int i = 0; i < path.Size; i++)
            {
                if (i != 0)
                {
                    // set color
                    if (path[i].type == LightPath.Type.Reflection)
                    {
                        Gizmos.color = Color.blue;
                    }
                    else if (path[i].type == LightPath.Type.Refraction)
                    {
                        Gizmos.color = Color.red;
                    }
                    Gizmos.DrawLine(path[i - 1].pos, path[i].pos);
                    Gizmos.color = Color.white;
                }
                // draw hit point
                Gizmos.DrawWireSphere(path[i].pos, visualLightScale);
            }
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
        //    //print(area.Area());
        //}
        //List<Vector3> surfacePoints = new List<Vector3>();
        //Vector3 normal = transform.forward;
        //foreach (Vector2 item in posList2D)
        //{
        //    surfacePoints.Add(ToCircleSurface(item, normal, radius));
        //}
        //foreach (Vector3 item in surfacePoints)
        //{
        //    //print(item);
        //    Gizmos.DrawLine(transform.position, item);
        //}
    }
}
