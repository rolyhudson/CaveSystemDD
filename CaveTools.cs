using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class CaveTools
    {
        public static Mesh splitTwoPlanes(Plane p1, Plane p2, Mesh m)
        {
            var m1 = split(m, p1);

            var m2 = split(m1, p2);

            return m2;
        }
        public static Mesh split(Mesh m, Plane p)
        {
            Mesh splitMesh = new Mesh();

            foreach (MeshFace f in m.Faces)
            {
                var pts = getFacePoints(f, m);
                if (pointsInsidePlane(pts, p))
                {
                    addPointsAndFace(pts, ref splitMesh);
                }
                else
                {
                    faceSplit(pts, p, ref splitMesh);
                }
            }
            splitMesh.Vertices.CullUnused();
            splitMesh.Vertices.CombineIdentical(true, true);
            //splitMesh.ExtractNonManifoldEdges(false);
            splitMesh.Faces.CullDegenerateFaces();
            splitMesh.Faces.ExtractDuplicateFaces();

            return splitMesh;
        }
        private static List<Point3d> getFacePoints(MeshFace f, Mesh m)
        {
            List<Point3d> pts = new List<Point3d>();
            pts.Add(m.Vertices[f.A]);
            pts.Add(m.Vertices[f.B]);
            pts.Add(m.Vertices[f.C]);
            pts.Add(m.Vertices[f.D]);
            return pts;
        }
        private static bool pointsInsidePlane(List<Point3d> pts, Plane pln)
        {
            foreach (Point3d p in pts)
            {
                if (!pointInsidePlane(p, pln)) return false;
            }
            return true;
        }
        public static bool pointInsidePlane(Point3d p, Plane pln)
        {
            Vector3d v = p - pln.Origin;
            if (Vector3d.VectorAngle(v, pln.Normal) <= Math.PI / 2) return true;
            else return false;
        }
        private static void addPointsAndFace(List<Point3d> pts, ref Mesh m)
        {
            int vcount = m.Vertices.Count;
            m.Vertices.AddVertices(pts);
            if (pts.Count == 3)
            {
                m.Faces.AddFace(vcount, vcount + 1, vcount + 2);
            }
            else
            {
                m.Faces.AddFace(vcount, vcount + 1, vcount + 2, vcount + 3);
            }
        }
        private static void faceSplit(List<Point3d> pts, Plane pln, ref Mesh meshToAppend)
        {
            List<Point3d> newPts = new List<Point3d>();
            for (int p = 0; p < pts.Count; p++)
            {
                if (pointInsidePlane(pts[p], pln))
                {
                    newPts.Add(pts[p]);

                }
                double t = 0;
                Line l = new Line();
                if (p == pts.Count - 1) l = new Line(pts[p], pts[0]);
                else l = new Line(pts[p], pts[p + 1]);

                if (Rhino.Geometry.Intersect.Intersection.LinePlane(l, pln, out t))
                {
                    if (t > 0 && t < 1) newPts.Add(l.PointAt(t));
                }

            }
            if (newPts.Count == 4 || newPts.Count == 3) addPointsAndFace(newPts, ref meshToAppend);
            if (newPts.Count == 5) add5PointsAndFaces(newPts, ref meshToAppend);
        }
        private static void add5PointsAndFaces(List<Point3d> pts, ref Mesh m)
        {
            int vcount = m.Vertices.Count;
            m.Vertices.AddVertices(pts);
            m.Faces.AddFace(vcount, vcount + 1, vcount + 2, vcount + 3);
            m.Faces.AddFace(vcount, vcount + 3, vcount + 4);
        }
        
    }
}
