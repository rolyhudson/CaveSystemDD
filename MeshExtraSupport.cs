using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class MeshExtraSupport
    {
        Plane localPlane;
        Polyline[] outlines;
        Curve[] offsets;
        Mesh panel;
        Parameters parameters;
        List<Line> gridPlanar = new List<Line>();
        List<Point3d> interpts = new List<Point3d>();
        public List<Point3d> meshpts = new List<Point3d>();
        List<List<Point3d>> planarPointGrid = new List<List<Point3d>>();
        public List<Line> Stubs = new List<Line>();
        List<Line> subFrame = new List<Line>();
        public void SetExtraSupport(Plane plane, Mesh mesh, List<Line> grid, List<List<Point3d>> pointGrid, List<Line> frame, Parameters p)
        {
            localPlane = plane;
            panel = mesh;
            gridPlanar = grid;
            planarPointGrid = pointGrid;
            subFrame = frame;
            parameters = p;
            SetUpBoundary();
            if (offsets == null) return;
            GetIntersectionPts();
            if (interpts.Count == 0)
            {
                CheckGeometry();
                return;
            }
            RemoveCloseIntersects();
            FindIntersectsToKeep();
            setStubs();
            //CheckGeometry();
        }
        private void CheckGeometry()
        {
            //CaveTools.CheckLines(gridPlanar);
            CaveTools.CheckLines(Stubs);
            //CaveTools.CheckPoints(interpts);
            // CaveTools.CheckPoints(meshpts);
            foreach (Curve curve in offsets)
                RhinoDoc.ActiveDoc.Objects.AddCurve(curve);
        }
        private void SetUpBoundary()
        {
            outlines = panel.GetOutlines(localPlane);
            if (outlines == null || outlines.Length == 0)
                return;
            int reduct = outlines[0].ReduceSegments(20);
            
            NurbsCurve boundaryMesh = NurbsCurve.Create(false, 1, outlines[0]);
            //RhinoDoc.ActiveDoc.Objects.AddCurve(boundaryMesh);
            offsets = boundaryMesh.ToNurbsCurve().Offset(localPlane, parameters.cellGap / -2.0 + 1 , RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, CurveOffsetCornerStyle.Sharp);
        }
        private void GetIntersectionPts()
        {
            foreach (Line g in gridPlanar)
            {
                foreach (Curve curve in offsets)
                {
                    
                    var inters = Rhino.Geometry.Intersect.Intersection.CurveLine(curve, g, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    foreach (var inter in inters)
                    {
                        interpts.Add(inter.PointA);
                    }
                        
                }
            }
        }
        private void RemoveCloseIntersects()
        {
            List<Point3d> cleaned = new List<Point3d>();
            cleaned.Add(interpts[0]);
            for (int i = 1; i < interpts.Count; i++)
            { 
                Point3d closest = CaveTools.ClosestPoint(interpts[i], cleaned);
                if (closest.DistanceTo(interpts[i]) > parameters.cellGap / 4)
                    cleaned.Add(interpts[i]);
            }
            interpts = cleaned;
        }
        private void FindIntersectsToKeep()
        {
           
            
            for (int i = 0; i < interpts.Count; i++)
            {
                Point3d closest = CaveTools.ClosestPoint(interpts[i], planarPointGrid.SelectMany(x=>x).ToList());
                if (closest.DistanceTo(interpts[i]) > parameters.cellGap / 4)
                {
                    Ray3d ray = new Ray3d(interpts[i], localPlane.ZAxis);
                    double t = Rhino.Geometry.Intersect.Intersection.MeshRay(panel, ray);
                    if (t >= 0)
                    {
                        meshpts.Add(ray.PointAt(t));
                        
                    }
                }
                    
            }
            
        }
        private void setStubs()
        {
            foreach(Point3d p in meshpts)
            {
                Line line = new Line(p, localPlane.ZAxis * -1000);
                double a = 0;
                double b = 0;
                foreach(Line frame in subFrame)
                {
                    var intersect = Rhino.Geometry.Intersect.Intersection.LineLine(line, frame, out a, out b,5,true);
                    if (line.PointAt(a).DistanceTo(frame.PointAt(b)) < parameters.cellGap / 4)
                        Stubs.Add(new Line(frame.PointAt(b),p));
                }
            }
        }
        
    }
}
