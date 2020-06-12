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
        public Point3d frameGridNode;
        Point3d firstNode;
        Vector3d toPanel;
        public Line Stub = new Line();
        double minStub = 295;
        double maxStub = 1005;
        double stubTop = 200;
        public double firstNodeToPanel;
        public bool LengthCompliance = true;
        public bool NoMeshPoint = false;
        MeshNode meshnode;
        public StubMember(Point3d g1, MeshNode m1, List<Line> framing)
        {
            if (!m1.pointset)
            {
                NoMeshPoint = true;
                
                return;
            }
            meshnode = m1;
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
            if (axisNodes.Count==0)
            {
                stubEnd = mesh1 - toPanel * 300 ;
            }
            else
            {
                double maxDist = double.MinValue;
                double minDist = double.MaxValue;
                foreach (Point3d p in axisNodes)
                {
                    if (mesh1.DistanceTo(p) > maxDist)
                    {
                        maxDist = mesh1.DistanceTo(p);
                        stubEnd = p;
                    }
                    if (mesh1.DistanceTo(p) < minDist)
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
            }
                
            frameGridNode = stubEnd;
            stubEnd = stubEnd - toPanel * stubTop;
            setStub();
        }
        public void Update(Point3d newend)
        {
            stubEnd = newend;
            setStub();
        }
        private void setStub()
        {
            if (meshnode.isGhost)
                Stub = new Line(frameGridNode, stubEnd);
            else
                Stub = new Line(stubEnd, mesh1);
        }
    }
}
