using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class FrameMember
    {
        Point3d grid1;
        Point3d grid2;
        Point3d mesh1;
        Point3d mesh2;
        Point3d closestPoint;
        Point3d throughPoint;
        Mesh panel;
        public Line frameLine = new Line();
        Vector3d toPanel;
        List<Point3d> panelCurvepts = new List<Point3d>();
        public NurbsCurve panelCurve;
        Line panelLine;
        Line gridLine;
        public Line shiftLine;
        double panelFrameDist = 300;
        double minAngle = 0.610865;
        double maxAngle = 2.53073;
        public bool AngleCompliance = true;
        public FrameMember(Point3d g1, Point3d g2, MeshNode m1, MeshNode m2, Mesh mesh)
        {
            grid1 = g1;
            grid2 = g2;
            if(!m1.pointset || !m2.pointset)
            {
                return;
            }
            mesh1 = m1.point;
            mesh2 = m2.point;
            panel = mesh;
            toPanel = mesh1 - g1;
            toPanel.Unitize();
            panelLine = new Line(mesh1, mesh2);
            gridLine = new Line(grid1, grid2);
            FindMeshCurve();
            SetFrameLine(panelLine);
            CheckAngle();
            CheckGeometry();
        }
        private void CheckGeometry()
        {
            //RhinoDoc.ActiveDoc.Objects.AddCurve(panelCurve);
            //RhinoDoc.ActiveDoc.Objects.AddLine(panelLine);
            //RhinoDoc.ActiveDoc.Objects.AddLine(frameLine);
            //RhinoDoc.ActiveDoc.Objects.AddLine(shiftLine);
            //RhinoDoc.ActiveDoc.Objects.AddPoint(closestPoint);
        }
        private void CheckAngle()
        {
            double angle = Vector3d.VectorAngle(toPanel, frameLine.Direction);
            if (angle >= minAngle && angle <= maxAngle)
                AngleCompliance = true;
            else
            {
                AngleCompliance = false;
                Point3d toMove = new Point3d();
                if (Vector3d.VectorAngle(frameLine.Direction, toPanel) > Math.PI / 2)
                {
                    //shift start point
                    toMove = frameLine.From;
                    FixAngle(ref toMove, angle);
                    frameLine.From = toMove;
                }
                else
                {
                    //shift end point
                    toMove = frameLine.To;
                    FixAngle(ref toMove, angle);
                    frameLine.To = toMove;
                }
                double checkangle = Vector3d.VectorAngle(toPanel, frameLine.Direction);
                SetFrameLine(frameLine);
            }
                
        }
        private void FixAngle(ref Point3d move,double angle)
        {
            double d = gridLine.Length / Math.Tan(minAngle);
            double current = Math.Abs(frameLine.Length * Math.Cos(angle));
            double diff = current - d;
            move = move - toPanel * diff;
            
        }
        private void FindMeshCurve()
        {
            for(int i = 0; i <= 50; i++)
            {
                Point3d p = gridLine.PointAt(1.0 / 50 * i);
                Ray3d ray = new Ray3d(p, toPanel);
                double t = Rhino.Geometry.Intersect.Intersection.MeshRay(panel, ray);
                if (t >= 0)
                {
                    panelCurvepts.Add(ray.PointAt(t));
                }
            }
            panelCurve = NurbsCurve.Create(false, 1, panelCurvepts);
        }
        private void SetFrameLine(Line baseline)
        {
            //move away from cave
            Transform transform = Transform.Translation(toPanel * - 5000);
            baseline.Transform(transform);
            double minDist = double.MaxValue;
            Vector3d shift = new Vector3d();
            foreach (Point3d p in panelCurvepts)
            {
                Point3d p2 = baseline.ClosestPoint(p, false);
                
                Vector3d perp = p - p2;
                if (perp.Length < minDist)
                {
                    minDist = perp.Length;
                    closestPoint = p;
                    shift = perp;

                }
            }

            var intersects = Rhino.Geometry.Intersect.Intersection.CurveLine(baseline.ToNurbsCurve(), new Line(closestPoint, toPanel), RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            if (intersects.Count > 0)
            {
                Point3d start = intersects[0].PointA;
                Vector3d shiftHyp = closestPoint - start;
                double angle = Vector3d.VectorAngle(shift, shiftHyp);
                double hyp = Math.Abs(panelFrameDist / Math.Cos(angle));
                double delta = shiftHyp.Length - hyp;
                shiftHyp.Unitize();
                Transform transform1 = Transform.Translation(shiftHyp * delta);
                frameLine = baseline;
                frameLine.Transform(transform1);
                shift.Unitize();
                shiftLine = new Line(closestPoint, shift * -panelFrameDist);
            }
        }
    }
}
