using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class PanelFrame
    {
        Plane localPlane;
        double xdim;
        double ydim;
        Parameters parameters;
        public Mesh CavePanels;
        //DummyMeshNodes
        NurbsCurve boundaryMesh;
        bool meshBoundarySet;
        List<List<Point3d>> missingMeshNodeGrid = new List<List<Point3d>>();

        int number;
        int familyCount;
        public bool FailedFrame = false;

        List<List<Point3d>> nodeGrid = new List<List<Point3d>>();
        List<List<Point3d>> frameGrid = new List<List<Point3d>>();
        public List<StubMember> internalStub  = new List<StubMember>();
        public List<FrameMember> subFrame = new List<FrameMember>();
        public List<Line> frameLines = new List<Line>();
        public List<Point3d> frameCorners = new List<Point3d>();
        public List<Line> DummyGSALines = new List<Line>();
        
        public Mesh GSAmesh;
        public List<List<MeshNode>> meshnodes = new List<List<MeshNode>>();
        public List<StubMember> cornerStub = new List<StubMember>();
        public PanelFrame(Plane local, double x, Parameters param, Mesh p, int num, int groupNum, double iydim)
        {
            localPlane = local;
            xdim = x;
            ydim = param.yCell;
            parameters = param;
            CavePanels = p;
            number = num;
            familyCount = groupNum;
            SetLocalPlane();
            //OrientedBox.CheckPlane(localPlane);
            
            SetPointGrid();
            SetMeshNodes();
            FindMeshNodes();
            SetSubFrame();
            SetStubsFrameNodes();
            
            SetFrameLines();
            MakeDummyGSALines();
            CheckGeometry();
        }
        private void CheckGeometry()
        {
            
            //CaveTools.CheckLines(subFrame.Select(x => x.frameLine).ToList());
            CaveTools.CheckLines(internalStub.Select(x => x.Stub).ToList());
            //CaveTools.CheckLines(cornerStub.Select(x => x.Stub).ToList());
            CaveTools.CheckLines(DummyGSALines);
            CaveTools.CheckLines(frameLines);
            //RhinoDoc.ActiveDoc.Objects.AddMesh(GSAmesh);
        }
        private void SetLocalPlane()
        {
            ydim -= parameters.cellGap;
            
            //if (number == 0)
            //{
            //    xdim -= parameters.cellGap / 2;
            //    localPlane.Origin = localPlane.Origin + localPlane.XAxis * 5 + localPlane.YAxis * parameters.cellGap / 2;
            //}
            //else if (number == familyCount - 1)
            //{
            //    xdim -= parameters.cellGap/2 - 2;
            //    localPlane.Origin = localPlane.Origin + localPlane.XAxis * (parameters.cellGap / 2 -5) + localPlane.YAxis * parameters.cellGap / 2;
            //}
            
            //standard case
            xdim -= parameters.cellGap;
            localPlane.Origin = localPlane.Origin + localPlane.XAxis * parameters.cellGap / 2 + localPlane.YAxis * parameters.cellGap / 2;
            
        }
        private void SetPointGrid()
        {
            int numx = 3;
            int numy = 3;
            if (xdim < parameters.xCell / 2) numx = 2;
            if (ydim < parameters.yCell / 2) numy = 2;
            double xspace = xdim / (numx - 1);
            double yspace = ydim / (numy - 1);
            for (int i = 0; i < numx; i++)
            {
                List<Point3d> row = new List<Point3d>();
                for (int j = 0; j < numy; j++)
                {
                    Vector3d shift = i * xspace * localPlane.XAxis + j * yspace * localPlane.YAxis;
                    row.Add(localPlane.Origin + shift);

                }
                nodeGrid.Add(row);
                //CaveTools.CheckPoints(row);
            }
        }
       
        private void SetMeshNodes()
        {
            foreach (List<Point3d> pts in nodeGrid)
            {
                List<MeshNode> nodes = new List<MeshNode>();
                for (int p = 0; p < pts.Count; p++)
                    nodes.Add(new MeshNode());
                meshnodes.Add(nodes);
            }
        }
        private void GetMissingNodeGrid()
        {
            if (meshBoundarySet)
                return;
            meshBoundarySet = true;
            Polyline[] outlines = CavePanels.GetOutlines(localPlane);
            boundaryMesh = NurbsCurve.Create(false,2, outlines[0]);
            Curve[] offsets = boundaryMesh.ToNurbsCurve().Offset(localPlane, parameters.cellGap / -2.0,RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, CurveOffsetCornerStyle.Smooth);
            List<Curve> simple = new List<Curve>();
            foreach (Curve o in offsets)
            {
                Curve s = o.Simplify(CurveSimplifyOptions.RebuildLines, 50, 0.5);
                simple.Add(s);
                RhinoDoc.ActiveDoc.Objects.AddCurve(s);
            }
                

            for (int c = 0; c < nodeGrid.Count; c++)
            {
                List<Point3d> pts = new List<Point3d>();
                for (int d = 0; d < nodeGrid[c].Count; d++)
                {
                    //find closest point in grid plane
                    List<Point3d> intersectPoints = new List<Point3d>();
                    //foreach (Curve o in simple)
                    //{
                    //    Plane PlaneY = new Plane(nodeGrid[c][d], localPlane.XAxis);
                    //    Plane PlaneX = new Plane(nodeGrid[c][d], localPlane.YAxis);
                    //    var interX = Rhino.Geometry.Intersect.Intersection.CurvePlane(o, PlaneX, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    //    var interY = Rhino.Geometry.Intersect.Intersection.CurvePlane(o, PlaneY, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    //    if (interX!=null)
                    //        intersectPoints.Add(interX[0].PointA);
                    //    if (interY != null)
                    //        intersectPoints.Add(interY[0].PointA);
                    //}
                    double minDist = double.MaxValue;
                    Point3d closest = new Point3d();
                    if (intersectPoints.Count == 0)
                    {
                        double t = 0;
                        foreach (Curve o in simple)
                        {
                            o.ClosestPoint(nodeGrid[c][d], out t);
                            intersectPoints.Add(o.PointAt(t));
                        }
                    }
                    foreach (Point3d p in intersectPoints)
                    {
                        if (p.DistanceTo(nodeGrid[c][d]) < minDist)
                        {
                            minDist = p.DistanceTo(nodeGrid[c][d]);
                            closest = p;
                        }
                    }

                    
                    pts.Add(closest);
                }
                missingMeshNodeGrid.Add(pts);
            }
                    
        }
        private void FindMeshNodes()
        {
            for (int c = 0; c < nodeGrid.Count; c++)
            {
                List<Curve> row = new List<Curve>();
                for (int d = 0; d < nodeGrid[c].Count; d++)
                {
                    Ray3d ray = new Ray3d(nodeGrid[c][d], localPlane.ZAxis);
                    double t = Rhino.Geometry.Intersect.Intersection.MeshRay(CavePanels, ray);
                    if (t >= 0)
                    {
                        meshnodes[c][d].point = ray.PointAt(t);
                        meshnodes[c][d].pointset = true;
                    }
                    else
                    {
                        Polyline[] edges = CavePanels.GetNakedEdges();
                        double minDist = double.MaxValue;
                        Point3d closest = new Point3d();
                        Line drop = new Line(nodeGrid[c][d], localPlane.Normal, 1000);
                        Point3d meshPt = new Point3d();
                        foreach(Polyline pl in edges)
                        {
                            foreach(Point3d pt in pl)
                            {
                                Point3d temp = drop.ClosestPoint(pt,false);
                                if (temp.DistanceTo(pt) < minDist)
                                {
                                    minDist = temp.DistanceTo(pt);
                                    closest = temp;
                                    meshPt = pt;
                                }
                            }
                            
                        }
                        
                        RhinoDoc.ActiveDoc.Objects.AddPoint(closest);
                        
                        meshnodes[c][d].point = closest;
                        meshnodes[c][d].pointset = true;
                        RhinoDoc.ActiveDoc.Objects.AddPoint(meshnodes[c][d].point);
                    }
                    
                }
            }
            MakeGSAMesh();
        }
        private void MakeGSAMesh()
        {
            GSAmesh = new Mesh();
            for (int i = 0; i < meshnodes.Count; i++)
            {
                for (int j = 0; j < meshnodes[i].Count; j++)
                {
                    if (i < meshnodes.Count - 1 && j < meshnodes[i].Count - 1)
                    {
                        GSAmesh.Append(meshFrom4Nodes(new List<MeshNode> { meshnodes[i][j], meshnodes[i][j + 1], meshnodes[i + 1][j + 1], meshnodes[i + 1][j] }));

                    }
                }
            }
            
        }
        private void MakeDummyGSALines()
        {
            for (int i = 0; i < meshnodes.Count; i++)
            {
                for (int j = 0; j < meshnodes[i].Count; j++)
                {
                    Line line = new Line();
                    if (i < meshnodes.Count - 1 && j < meshnodes[i].Count - 1)
                    {
                        //meshnodes[i][j], meshnodes[i][j + 1], meshnodes[i + 1][j + 1]
                       
                        if (FromMeshNodes(meshnodes[i][j], meshnodes[i][j + 1], ref line))
                            DummyGSALines.Add(line);
                        if (FromMeshNodes(meshnodes[i][j + 1], meshnodes[i + 1][j + 1], ref line))
                            DummyGSALines.Add(line);
                        
                        
                        if (FromMeshNodes(meshnodes[i][j], meshnodes[i + 1][j + 1], ref line))
                            DummyGSALines.Add(line);
                        else
                        {
                            if (FromMeshNodes(meshnodes[i][j + 1], meshnodes[i + 1][j], ref line))
                                DummyGSALines.Add(line);
                        }
                    }
                    if(i == meshnodes.Count - 1 && j < meshnodes[i].Count - 1)
                    {
                        if (FromMeshNodes(meshnodes[i][j], meshnodes[i][j + 1], ref line))
                            DummyGSALines.Add(line);
                    }
                    if(j == 0 && i < meshnodes.Count - 1)
                        if (FromMeshNodes(meshnodes[i][j], meshnodes[i+1][j], ref line))
                            DummyGSALines.Add(line);
                }
            }
        }
        private bool FromMeshNodes(MeshNode start, MeshNode end, ref Line line)
        {
            if (start.pointset && end.pointset)
            {
                line = new Line(start.point, end.point);
                return true;
            }
            return false;
        }
        private Mesh meshFrom4Nodes(List<MeshNode> nodes)
        {
            //max for possible nodes in order
            Mesh mesh = new Mesh();
            foreach (MeshNode mn in nodes)
            {
                if (mn.pointset)
                    mesh.Vertices.Add(mn.point);
            }
            if (mesh.Vertices.Count == 4)
            {
                
                mesh.Faces.AddFace(2,0,1);
                mesh.Faces.AddFace(0,2,3);
            }
            if (mesh.Vertices.Count == 3)
            {
                mesh.Faces.AddFace(2,0,1);
            }
            return mesh;
        }
       
        private void SetSubFrame()
        {
            for (int c = 0; c < nodeGrid.Count; c++)
            {
                for (int d = 0; d < nodeGrid[c].Count; d++)
                {
                    if (d < nodeGrid[c].Count - 1)
                    {
                        FrameMember frameMember = new FrameMember(nodeGrid[c][d], nodeGrid[c][d + 1], meshnodes[c][d], meshnodes[c][d + 1], CavePanels);
                        if (frameMember.frameLine.Length > 0)
                            subFrame.Add(frameMember);
                           
                    }

                    if (c < nodeGrid.Count - 1)
                    {
                        FrameMember frameMember = new FrameMember(nodeGrid[c][d], nodeGrid[c + 1][d], meshnodes[c][d], meshnodes[c + 1][d], CavePanels);
                        if (frameMember.frameLine.Length > 0)
                            subFrame.Add(frameMember);
                    }
                            
                }
            }
            
        }
        private void SetFrameLines()
        {
            for (int c = 0; c < nodeGrid.Count; c++)
            {
                for (int d = 0; d < nodeGrid[c].Count; d++)
                {
                    if (frameGrid[c][d].X == 0)
                        continue;
                    if (d < nodeGrid[c].Count - 1)
                    {
                        if (frameGrid[c][d + 1].X == 0) 
                            continue;
                        Line frame = new Line(frameGrid[c][d], frameGrid[c][d + 1]);

                        frameLines.Add(frame);

                    }

                    if (c < nodeGrid.Count - 1)
                    {
                        if (frameGrid[c + 1][d].X ==0 )
                            continue;
                        Line frame = new Line(frameGrid[c][d], frameGrid[c + 1][d]);
                        frameLines.Add(frame);
                    }

                }
            }
        }
        
        private void SetStubsFrameNodes()
        {
            frameGrid = new List<List<Point3d>>();
            for (int c = 0; c < nodeGrid.Count; c++)
            {
                List<Point3d> pts = new List<Point3d>();
                for (int d = 0; d < nodeGrid[c].Count; d++)
                {
                    StubMember stubMaker = new StubMember(nodeGrid[c][d], meshnodes[c][d], subFrame.Select(x=>x.frameLine).ToList());
                    pts.Add(stubMaker.frameGridNode);

                    if (c == 0 || c == nodeGrid.Count - 1)
                    {
                        if (d == 0 || d == nodeGrid[c].Count - 1)
                            cornerStub.Add(stubMaker);
                    }
   
                    else
                        internalStub.Add(stubMaker);

                }
                frameGrid.Add(pts);
            }
        }
        
       
    }
}
