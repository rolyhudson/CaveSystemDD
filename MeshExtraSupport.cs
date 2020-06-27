using Rhino;
using Rhino.DocObjects;
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
        Curve offset;
        Parameters parameters;
        List<Line> gridPlanar = new List<Line>();
        List<Point3d> interpts = new List<Point3d>();
        public List<Point3d> meshpts = new List<Point3d>();
        List<List<Point3d>> planarPointGrid = new List<List<Point3d>>();
        public List<Line> Stubs = new List<Line>();
        List<Line> subFrame = new List<Line>();
        PanelFrame panelFrame;
        public void SetExtraSupport(PanelFrame panelF, List<Line> grid, List<List<MeshNode>> meshnodes, List<Line> frame, Parameters p)
        {
            panelFrame = panelF;
            gridPlanar = grid;
            SetPlanarPoints(meshnodes);
            subFrame = frame;
            parameters = p;
            offset = CaveTools.OffsetBoundary(panelFrame, parameters.cellGap / 2.0 - 1);
            //RhinoDoc.ActiveDoc.Objects.AddCurve(offset);
            if (offset == null)
            {
                SetWarningGeometry();
                return;
            }
                

            GetIntersectionPts();

            if (interpts.Count == 0)
            {
                SetWarningGeometry();
                return;
            }

            RemoveCloseIntersects();
            FindIntersectsToKeep();
            setStubs();
            //CheckGeometry();
        }
        private void SetPlanarPoints(List<List<MeshNode>> meshnodes)
        {
            foreach(List<MeshNode> row in meshnodes)
            {
                List<Point3d> pts = new List<Point3d>();
                foreach (MeshNode meshNode in row)
                {
                    if(!meshNode.isGhost)
                        pts.Add(panelFrame.localPlane.ClosestPoint(meshNode.point));

                }
                planarPointGrid.Add(pts);
            }
        }
        private void SetWarningGeometry()
        {
            var c = CaveTools.GetPlanarPanelBoundary(panelFrame);
            parameters.problemPanels.Add(c);
            RhinoDoc.ActiveDoc.Objects.AddCurve(c);
            return;
        }
        private void SetUpBoundary()
        {
            //outlines = panel.GetOutlines(localPlane);
            //if (outlines == null || outlines.Length == 0)
            //    return;
            //int reduct = outlines[0].ReduceSegments(4);

            //NurbsCurve boundaryMesh = NurbsCurve.Create(false, 1, outlines[0]);
            //AreaMassProperties amp = AreaMassProperties.Compute(boundaryMesh);
            ////RhinoDoc.ActiveDoc.Objects.AddCurve(boundaryMesh);
            //offsets = boundaryMesh.Offset(CaveTools.averagePoint(boundaryMesh.Points.Select(x=>x.Location).ToList()), localPlane.Normal, parameters.cellGap / 2.0 - 1 , 5, CurveOffsetCornerStyle.Sharp);
            ////if (offsets != null && offsets.Length >= 1)

            //AreaMassProperties amp2 = AreaMassProperties.Compute(offsets[0]);
            //if(amp2.Area > amp.Area)
            //    offsets = boundaryMesh.Offset(CaveTools.averagePoint(boundaryMesh.Points.Select(x => x.Location).ToList()), localPlane.Normal, -(parameters.cellGap / 2.0 - 1), 5, CurveOffsetCornerStyle.Sharp);
            

            //RhinoDoc.ActiveDoc.Objects.AddCurve(curve);
        }
        private void GetIntersectionPts()
        {
            foreach (Line g in gridPlanar)
            {
                var inters = Rhino.Geometry.Intersect.Intersection.CurveLine(offset, g, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                foreach (var inter in inters)
                {
                    interpts.Add(inter.PointA);
                }
            }
        }
        private void RemoveCloseIntersects()
        {
            //compare to each other and keep those within min spacing
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
           //compare to point grid to remove if too close
            
            for (int i = 0; i < interpts.Count; i++)
            {
                Point3d closest = CaveTools.ClosestPoint(interpts[i], planarPointGrid.SelectMany(x=>x).ToList());
                if (closest.DistanceTo(interpts[i]) > parameters.cellGap / 4)
                {
                    Ray3d ray = new Ray3d(interpts[i], panelFrame.localPlane.ZAxis);
                    double t = Rhino.Geometry.Intersect.Intersection.MeshRay(panelFrame.CavePanels, ray);
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
                Line line = new Line(p, panelFrame.localPlane.ZAxis * -1000);
                double a = 0;
                double b = 0;
                foreach(Line frame in subFrame)
                {
                    var intersect = Rhino.Geometry.Intersect.Intersection.LineLine(line, frame, out a, out b,5,true);
                    if (line.PointAt(a).DistanceTo(frame.PointAt(b)) < 5)
                        Stubs.Add(new Line(frame.PointAt(b),p));
                }
            }
        }
        
    }
}
