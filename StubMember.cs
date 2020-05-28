using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class StubMember
    {
        Point3d grid1;
        Point3d mesh1;
        List<Line> frameMembers = new List<Line>();
        List<Point3d> axisNodes = new List<Point3d>();
        public Point3d stubEnd;
        Point3d firstNode;
        Vector3d toPanel;
        public Line Stub = new Line();
        double minStub = 295;
        double maxStub = 1005;
        double stubTop = 200;
        public double firstNodeToPanel;
        public bool LengthCompliance = true;
        public StubMember(Point3d g1, MeshNode m1, List<Line> framing)
        {
            if (!m1.pointset)
                return;
            grid1 = g1;
            mesh1 = m1.point;
            toPanel = mesh1 - g1;
            toPanel.Unitize();
            frameMembers = framing;
            FindNodesOnAxis();
            FindStubEnd();
            //RhinoDoc.ActiveDoc.Objects.AddLine(Stub);
        }
        private void FindNodesOnAxis()
        {
            //push grid up for cases where mesh is too close
            Line axis = new Line(mesh1, grid1 - toPanel * 5000);
            
            double ta = 0;
            double tb = 0;
            foreach(Line f in frameMembers)
            {
                if (Rhino.Geometry.Intersect.Intersection.LineLine(axis, f, out ta, out tb, 50, true))
                {
                    axisNodes.Add(axis.PointAt(ta));
                   
                }
                    
            }
        }
        private void FindStubEnd()
        {
            double maxDist = double.MinValue;
            double minDist = double.MaxValue;
            foreach(Point3d p in axisNodes)
            {
                if(mesh1.DistanceTo(p) > maxDist)
                {
                    maxDist = mesh1.DistanceTo(p);
                    stubEnd = p;
                }
                if(mesh1.DistanceTo(p) < minDist)
                {
                    minDist = mesh1.DistanceTo(p);
                    firstNode = p;
                }
            }
            firstNodeToPanel = minDist;
            if (firstNodeToPanel >= minStub && firstNodeToPanel <= maxStub)
                LengthCompliance = true;
            else
                LengthCompliance = false;
            if (stubEnd.X == 0 && stubEnd.Y==0)
                return;
            stubEnd = stubEnd - toPanel * stubTop;
            Stub = new Line(mesh1, stubEnd);
        }
    }
}
