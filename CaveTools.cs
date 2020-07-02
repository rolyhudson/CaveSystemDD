using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class CaveTools
    {
        static Random randomGen = new Random();
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
        private static List<Point3d> getVertexPoints( Mesh m)
        {
            List<Point3d> pts = new List<Point3d>();
            foreach(var v in m.Vertices)
                pts.Add(v);
            return pts;
        }
        public static bool MeshInsidePlanes(Plane pln1, Plane pln2, Mesh m)
        {
            List<Point3d> pts = getVertexPoints(m);
            if (pointsInsidePlane(pts, pln1) && pointsInsidePlane(pts, pln2))
                return true;
            else
                return false;
        }
        public static bool pointsInsidePlane(List<Point3d> pts, Plane pln)
        {
            foreach (Point3d p in pts)
            {
                if (!pointInsidePlane(p, pln)) return false;
            }
            return true;
        }
        public static List<Point3d> PointsInsidePlane(List<Point3d> pts, Plane pln1, Plane pln2)
        {
            List<Point3d> inside = new List<Point3d>();
            foreach (Point3d p in pts)
            {
                if (pointInsidePlane(p, pln1) && pointInsidePlane(p, pln2))
                    inside.Add(p);
            }
            return inside;
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
        public static Brep makeCuboid(Plane pln, double width, double depth, double height)
        {
            Mesh cell = new Mesh();
            Box box = new Box(pln, new Interval(-width / 2, width / 2), new Interval(-depth / 2, depth / 2), new Interval(-height / 2, height / 2));
            //cell = Mesh.CreateFromBox(box, 1, 1, 1);
            //return cell;
            return box.ToBrep();
        }
        public static Color getRandomColour()
        {

            KnownColor[] names = (KnownColor[])Enum.GetValues(typeof(KnownColor));
            KnownColor randomColorName = names[randomGen.Next(names.Length)];
            Color randomColor = Color.FromKnownColor(randomColorName);
            return randomColor;
        }
        public static Point3d averagePoint(Mesh m)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            foreach (Point3d p in m.Vertices)
            {
                x += p.X;
                y += p.Y;
                z += p.Z;
            }
            Point3d centroid = new Point3d(x / m.Vertices.Count, y / m.Vertices.Count, z / m.Vertices.Count);
            return centroid;
        }
        public static Point3d averagePoint(List<Point3d> points)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            foreach (Point3d p in points)
            {
                x += p.X;
                y += p.Y;
                z += p.Z;
            }
            Point3d centroid = new Point3d(x / points.Count, y / points.Count, z / points.Count);
            return centroid;
        }
        public static Vector3d averageVector(Mesh m)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            foreach (var v in m.Normals)
            {
                x += v.X;
                y += v.Y;
                z += v.Z;
            }
            Vector3d centroid = new Vector3d(x / m.Normals.Count, y / m.Normals.Count, z / m.Normals.Count);
            return centroid;
        }
        public static Brep findBBoxGivenPlane(Plane pln, Mesh m, Point3d XMinPt, Point3d XMaxPt)
        {
            List<Point3d> pts = new List<Point3d>();
            Point3d remapped = new Point3d();
            foreach (Point3d p in m.Vertices)
            {
                
                pln.RemapToPlaneSpace(p, out remapped);

                pts.Add(remapped);
            }

            pln.RemapToPlaneSpace(XMinPt, out remapped);
            pts.Add(remapped);
            pln.RemapToPlaneSpace(XMaxPt, out remapped);
            pts.Add(remapped);

            BoundingBox bBox = new BoundingBox(pts);
            Brep brep = bBox.ToBrep();
            Transform xform = Transform.PlaneToPlane(Plane.WorldXY, pln);
            brep.Transform(xform);
            return brep;
        }
        public static OrientedBox FindOrientedBox(Plane pln, Mesh m, double width)
        {
            if (m.Faces.Count == 0)
                return null;
            
            Plane xminPlane = new Plane(pln.Origin, pln.YAxis);
            Plane xmaxPlane = new Plane(pln.Origin + pln.YAxis * width, pln.YAxis);
            Point3d xmin = xminPlane.ClosestPoint(m.Vertices[0]);
            Point3d xmax = xmaxPlane.ClosestPoint(m.Vertices[0]);
            Brep box = findBBoxGivenPlane(pln, m,xmin,xmax);
            OrientedBox orientedBox = new OrientedBox(box, pln);
            
            return orientedBox;
        }
        public static OrientedBox FindRefOrientedBox(Plane pln, Mesh m)
        {
            if (m.Faces.Count == 0)
                return null;

            
            Point3d xmin = m.Vertices[0];
            Point3d xmax = (Point3d)m.Vertices[0] + pln.Normal *1000;
            Brep box = findBBoxGivenPlane(pln, m, xmin, xmax);
            OrientedBox orientedBox = new OrientedBox(box, pln);
            //RhinoDoc.ActiveDoc.Objects.AddBrep(box);
            return orientedBox;
        }
        public static Point3d ClosestProjected(List<Brep> breps, Point3d testPoint,Vector3d direction)
        {
            var points = Rhino.Geometry.Intersect.Intersection.ProjectPointsToBreps(breps, new List<Point3d>() { testPoint }, direction, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            if (points == null)
                return new Point3d();
            double distMin = double.MaxValue;
            Point3d closest = new Point3d();
            foreach(var p in points)
            {
                
                if (p.DistanceTo(testPoint) < distMin)
                {
                    distMin = p.DistanceTo(testPoint);
                    closest = p;
                }
            }
            return closest;
        }
        public static Point3d ClosestPoint(List<Curve> curves, Point3d testPoint, Plane plane)
        {

            double tMin = double.MaxValue;
            double aMin = double.MaxValue;
            
            Point3d closest = new Point3d();
            double t = 0;
            foreach (var c in curves)
            {
                c.ClosestPoint(testPoint, out t);
                Point3d temp = c.PointAt(t);
                Plane testPlane = new Plane(temp, plane.Normal);
                //RhinoDoc.ActiveDoc.Objects.AddLine(new Line(temp,testPoint));
                Line testLine = new Line(testPoint, plane.Normal * -5000);
                Rhino.Geometry.Intersect.Intersection.LinePlane(testLine, testPlane, out t);
                if (t < 0) continue;
                Point3d planePt = testLine.PointAt(t);
                if (t < tMin && planePt.DistanceTo(temp) < 300 && planePt.DistanceTo(testPoint) < 4000)
                {
                    tMin = t;
                    
                    closest = testLine.PointAt(t);
                    
                }
            }
            return closest;
        }
        public static Point3d ClosestPoint(Point3d test, List<Point3d> points)
        {
            double minDist = double.MaxValue;
            Point3d closest = new Point3d();
            foreach(Point3d p in points)
            {
                if(p.DistanceTo(test)< minDist)
                {
                    minDist = p.DistanceTo(test);
                    closest = p;
                }
            }
            return closest;
        }
        public static NurbsCurve GetPlanarPanelBoundary(PanelFrame panelFrame)
        {
            Polyline[] outlines = panelFrame.CavePanels.GetOutlines(panelFrame.localPlane);
            if (outlines == null || outlines.Length == 0)
                return null;
            int reduct = outlines[0].ReduceSegments(4);

            NurbsCurve boundary = NurbsCurve.Create(false, 1, outlines[0]);
            return boundary;
        }
        public static Curve OffsetBoundary(PanelFrame panelFrame, double dist)
        {
            //panelFrame.parameters.cellGap / 2.0 - 1
            NurbsCurve boundary = GetPlanarPanelBoundary(panelFrame);
            AreaMassProperties amp = AreaMassProperties.Compute(boundary);
            //RhinoDoc.ActiveDoc.Objects.AddCurve(boundary);
            Curve[] offsets = boundary.Offset(CaveTools.averagePoint(boundary.Points.Select(x => x.Location).ToList()), panelFrame.localPlane.Normal, dist, 5, CurveOffsetCornerStyle.Sharp);
            //if (offsets != null && offsets.Length >= 1)

            AreaMassProperties amp2 = AreaMassProperties.Compute(offsets[0]);
            if (amp2 == null || amp2.Area > amp.Area )
                offsets = boundary.Offset(CaveTools.averagePoint(boundary.Points.Select(x => x.Location).ToList()), panelFrame.localPlane.Normal, -(dist), 5, CurveOffsetCornerStyle.Sharp);
            amp2 = AreaMassProperties.Compute(offsets[0]);
            if (amp2 == null || amp2.Area < 10000)
                offsets = boundary.Offset(panelFrame.localPlane, -(dist), 5, CurveOffsetCornerStyle.Sharp);
            if (offsets == null) return null;
            return offsets[0];
        }
        
        public static void CheckPoints(List<Point3d> points)
        {
            foreach (Point3d p in points)
            {
                RhinoDoc.ActiveDoc.Objects.AddPoint(p);
            }
        }
        public static void CheckLines(List<Line> lines)
        {
            foreach (Line p in lines)
            {
                RhinoDoc.ActiveDoc.Objects.AddLine(p);
            }
        }
    }
}
