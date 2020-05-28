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
        public Line frameLine;
        Vector3d toPanel;
        List<Point3d> panelCurvepts = new List<Point3d>();
        NurbsCurve panelCurve;
        Line panelLine;
        Line gridLine;
        double panelFrameDist = 300;
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
            SetFrameLine();
            CheckGeometry();
        }
        private void CheckGeometry()
        {
            RhinoDoc.ActiveDoc.Objects.AddCurve(panelCurve);
            RhinoDoc.ActiveDoc.Objects.AddLine(panelLine);
            RhinoDoc.ActiveDoc.Objects.AddLine(frameLine);
            RhinoDoc.ActiveDoc.Objects.AddLine(new Line(closestPoint, toPanel * -panelFrameDist));
            RhinoDoc.ActiveDoc.Objects.AddPoint(closestPoint);
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
        private void SetFrameLine()
        {
            double maxDist = double.MinValue;
            bool allBelow = true;
            foreach (Point3d p in panelCurvepts)
            {
                Point3d p2 = panelLine.ClosestPoint(p, true);
                
                Vector3d perp = p - p2;
                if (Vector3d.VectorAngle(perp, toPanel) > Math.PI / 2)
                {
                    allBelow = false;
                    if (perp.Length > maxDist)
                    {
                        maxDist = perp.Length;
                        closestPoint = p;
                    }
                }
                if (allBelow)
                    closestPoint = panelCurvepts.First();
            }
            var intersects = Rhino.Geometry.Intersect.Intersection.CurveLine(panelLine.ToNurbsCurve(), new Line(closestPoint, toPanel), RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            if (intersects.Count > 0)
            {
                Point3d start = intersects[0].PointA;
                Vector3d shift = closestPoint - start;
                throughPoint = closestPoint + shift;
                Transform transform = Transform.Translation(shift - toPanel * panelFrameDist);
                frameLine = panelLine;
                frameLine.Transform(transform);
            }
        }
    }
}
