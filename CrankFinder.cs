using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class CrankFinder
    {
        Plane CrankPlane;
        List<Point3d> GridRow = new List<Point3d>();
        Line FitLine;
        Plane local;
        List<Point3d> crankAxesStart = new List<Point3d>();
        List<Point3d> curveLimits = new List<Point3d>();
        Polyline panelCurve = new Polyline();
        Point3d throughtPoint;
        Point3d closestPoint = new Point3d();
        Point3d start;
        Point3d crankPoint;
        Point3d end;
        List<Plane> limitPlanes = new List<Plane>();
        double panelFrameDist = 300;
        public CrankFinder(Plane crankPln, Mesh panel, List<Point3d> gridRow,Plane localPlane)
        {
            CrankPlane = crankPln;
            local = localPlane;
            GridRow = gridRow;
            intersectAndJoinSegments(panel);
            limitPlanes.Add(new Plane(GridRow[0], local.YAxis));
            limitPlanes.Add(new Plane(GridRow.Last(), local.YAxis));
            ExtendPanelCurve();
            TrimPanelCurve();
            FindFitLine();
            
            SetCrankAxes();
            CheckGeometry();
        }
        private void CheckGeometry()
        {
            RhinoDoc.ActiveDoc.Objects.AddPoint(closestPoint);
            //RhinoDoc.ActiveDoc.Objects.AddPoint(start);
            //RhinoDoc.ActiveDoc.Objects.AddLine(new Line(start, closestPoint));
            RhinoDoc.ActiveDoc.Objects.AddLine(FitLine);
            RhinoDoc.ActiveDoc.Objects.AddPolyline(panelCurve);
            RhinoDoc.ActiveDoc.Objects.AddLine(new Line(closestPoint,local.Normal*-panelFrameDist));
            
        }
        
        private void SetCrankAxes()
        {
            Vector3d span = GridRow.Last() - GridRow.First();
            Point3d mid = GridRow.First() + span * 0.5;
            span.Unitize();
            Point3d point = mid - span * panelFrameDist;
            crankAxesStart.Add(mid - span * panelFrameDist);
            crankAxesStart.Add(mid + span * panelFrameDist);
        }
        private void FindFitLine()
        {
            if (panelCurve != null)
            {
                //combine points
                List<Point3d> points = new List<Point3d>();
                foreach(var p in panelCurve)
                {
                    points.Add(p);
                }
                //combine points fit
                if (Line.TryFitLineToPoints(points, out FitLine))
                {
                    ExtendFitLine();
                    RhinoDoc.ActiveDoc.Objects.AddLine(FitLine);
                    double maxDist = double.MinValue;
                    foreach (Point3d p in points)
                    {
                        Vector3d perp = p - FitLine.ClosestPoint(p, false);
                        if (Vector3d.VectorAngle(perp, local.Normal) > Math.PI / 2)
                        {
                            if (perp.Length > maxDist)
                            {
                                maxDist = perp.Length;
                                closestPoint = p;
                            }
                        }
                    }
                    var intersects = Rhino.Geometry.Intersect.Intersection.CurveLine(FitLine.ToNurbsCurve(), new Line(closestPoint, local.Normal), RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    if (intersects.Count > 0)
                    {
                        Point3d start = intersects[0].PointA;
                        Vector3d shift = closestPoint - start;
                        throughtPoint = closestPoint + shift; 
                        Transform transform = Transform.Translation(shift - local.Normal * panelFrameDist);
                        FitLine.Transform(transform);
                    }
                    
                }
            }
        }
        private void ExtendFitLine()
        {
            List<Point3d> endpts = new List<Point3d>();
            foreach(Plane limit in limitPlanes)
            {
                double d = 0;
                Rhino.Geometry.Intersect.Intersection.LinePlane(FitLine, limit, out d);
                endpts.Add(FitLine.PointAt(d));
            }
            FitLine = new Line(endpts.First(), endpts.Last());
        }
        private void ExtendPanelCurve()
        {
            foreach (Plane limit in limitPlanes)
            {
                var intersects = Rhino.Geometry.Intersect.Intersection.CurvePlane(panelCurve.ToNurbsCurve(), limit, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                if (intersects == null)
                {
                    //closest point on plane
                    Point3d p1 = panelCurve.First();
                    Point3d p2 = panelCurve.Last();
                    if (p1.DistanceTo(limit.ClosestPoint(p1)) < p2.DistanceTo(limit.ClosestPoint(p2)))
                        panelCurve.Insert(0, limit.ClosestPoint(p1));
                    //curveLimits.Add(limit.ClosestPoint(p1) - local.Normal * panelFrameDist);
                    else
                        panelCurve.Add(limit.ClosestPoint(p2));
                    //curveLimits.Add(limit.ClosestPoint(p2) - local.Normal * panelFrameDist);
                }
            }
        }
        private void TrimPanelCurve()
        {
            List<double> t = new List<double>();
            foreach (Plane limit in limitPlanes)
            {
                var intersects = Rhino.Geometry.Intersect.Intersection.CurvePlane(panelCurve.ToNurbsCurve(), limit, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                if (intersects != null)
                {
                    t.Add(intersects[0].ParameterA);
                }
            }
            Interval interval = new Interval(t.Min(), t.Max());
            panelCurve = panelCurve.Trim(interval);
        }
        private void intersectAndJoinSegments(Mesh panel)
        {
            Polyline[] intersects = Rhino.Geometry.Intersect.Intersection.MeshPlane(panel, CrankPlane);
            if (intersects.Length == 1)
                panelCurve = intersects[0];
            else
            {
                List<Curve> curves = new List<Curve>();
                foreach(Polyline pl in intersects)
                {
                    curves.Add(pl.ToNurbsCurve());
                }
                Curve[] joined = Curve.JoinCurves(curves, panelFrameDist * 2, false);
                if (joined.Length > 0)
                {
                    foreach(var p in joined[0].ToNurbsCurve().Points)
                    {
                        panelCurve.Add(p.Location);
                    }
                }
            }
            
        }
        private List<Point3d> ToPointList(Polyline polyline)
        {
            List<Point3d> pts = new List<Point3d>();
            foreach (Point3d p in polyline)
                pts.Add(p);

            return pts;
        }
    }
}
