using System.Collections.Generic;
using Delaunay.Utils;
using UnityEngine;

namespace Delaunay
{
    public sealed class Triangle: IDisposable
	{
		private List<Site> _sites;
		public List<Site> sites {
			get { return this._sites; }
		}
		
		public Triangle (Site a, Site b, Site c)
		{
			_sites = new List<Site> () { a, b, c };
		}
		
		public void Dispose ()
		{
			_sites.Clear ();
			_sites = null;
		}

        public bool HasSite(int siteIndex)
        {
            foreach (Site site in sites)
            {
                if (site._siteIndex == siteIndex)
                {
                    return true;
                }
            }
            return false;
        }

        public float Area()
        {
            int index, nextIndex;
            int n = sites.Count;
            Vector2 point, next;
            float signedDoubleArea = 0; // Losing lots of precision?
            for (index = 0; index < n; ++index)
            {
                nextIndex = (index + 1) % n;
                point = sites[index].Coord;
                next = sites[nextIndex].Coord;
                signedDoubleArea += point.x * next.y - next.x * point.y;
            }
            return Mathf.Abs(signedDoubleArea * 0.5f);
        }

        // from geom
        public static bool LineLineIntersection(Vector2 p0, Vector2 v0, Vector2 p1, Vector2 v1, out float m0, out float m1)
        {
            var det = (v0.x * v1.y - v0.y * v1.x);

            if (Mathf.Abs(det) < 0.001f)
            {
                m0 = float.NaN;
                m1 = float.NaN;

                return false;
            }
            else
            {
                m0 = ((p0.y - p1.y) * v1.x - (p0.x - p1.x) * v1.y) / det;

                if (Mathf.Abs(v1.x) >= 0.001f)
                {
                    m1 = (p0.x + m0 * v0.x - p1.x) / v1.x;
                }
                else
                {
                    m1 = (p0.y + m0 * v0.y - p1.y) / v1.y;
                }

                return true;
            }
        }

        // from geom
        public static Vector2 RotateRightAngle(Vector2 v)
        {
            var x = v.x;
            v.x = -v.y;
            v.y = x;

            return v;
        }

        // from geom
        public Vector2 CircumcircleCenter(out bool isReal)
        {
            Vector2 c0 = sites[0].Coord;
            Vector2 c1 = sites[1].Coord;
            Vector2 c2 = sites[2].Coord;
            var mp0 = 0.5f * (c0 + c1);
            var mp1 = 0.5f * (c1 + c2);

            var v0 = RotateRightAngle(c0 - c1);
            var v1 = RotateRightAngle(c1 - c2);

            float m0, m1;

            isReal = LineLineIntersection(mp0, v0, mp1, v1, out m0, out m1);

            return mp0 + m0 * v0;
        }
    }
}