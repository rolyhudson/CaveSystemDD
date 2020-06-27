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
        public Plane localPlane;
        public Plane refPlane;
        public double xdim;
        double ydim;
        Parameters parameters;
        public Mesh CavePanels;

        public bool FailedFrame = false;

        List<List<Point3d>> nodeGrid = new List<List<Point3d>>();
        public List<List<Point3d>> frameGrid = new List<List<Point3d>>();
        public List<StubMember> internalStub  = new List<StubMember>();
        public List<FrameMember> subFrame = new List<FrameMember>();
        public List<Line> frameLinesX = new List<Line>();
        public List<Line> frameLinesY = new List<Line>();
        
        public List<Point3d> extraMeshNodes = new List<Point3d>();
        public List<Line> DummyGSALines = new List<Line>();
        
        public Mesh GSAmesh;
        public List<List<MeshNode>> meshnodes = new List<List<MeshNode>>();
        public List<StubMember> cornerStub = new List<StubMember>();
        public MeshExtraSupport meshExtraSupport = new MeshExtraSupport();
        public double panelArea = 0;
        bool ghostNodesFound =false;
        public bool clashFixAttempted = false;
        public PanelFrame(Plane local, double panelXDim, Parameters param, Mesh p, int num, double panelYDim)
        {
            if (p == null)
            {
                FailedFrame = true;
                return;
            }
            if (p.Faces.Count == 0)
            {
                FailedFrame = true;
                return;
            }
            refPlane = local;
            xdim = panelXDim;
            ydim = panelYDim;
            parameters = param;
            CavePanels = p;

            //familyCount = groupNum;
            SetLocalPlane();
            //OrientedBox.CheckPlane(localPlane);
            SetPanelArea();
            SetPointGrid();
            SetMeshNodes();
            FindMeshNodes();
            SetSubFrame();
            SetStubsFrameNodes();

            SetFrameLines(ref frameGrid, ref frameLinesX, ref frameLinesY);
            if (ghostNodesFound)
                GetMissingMeshStubs();
            MakeGSAMesh();
            MakeDummyGSALines();
            
        }
        private void SetPanelArea()
        {
            AreaMassProperties amp = AreaMassProperties.Compute(CavePanels);
            panelArea = amp.Area;
        }
        public void CheckGeometry()
        {
            
            //CaveTools.CheckLines(subFrame.Select(x => x.frameLine).ToList());
            CaveTools.CheckLines(internalStub.Select(x => x.Stub).ToList());
            CaveTools.CheckLines(meshExtraSupport.Stubs.Select(x => x).ToList());
            //CaveTools.CheckLines(cornerStub.Select(x => x.Stub).ToList());
            //CaveTools.CheckLines(DummyGSALines);
            CaveTools.CheckLines(frameLinesX);
            CaveTools.CheckLines(frameLinesY);
            RhinoDoc.ActiveDoc.Objects.AddMesh(GSAmesh);
        }
        private void SetLocalPlane()
        {
            ydim -= parameters.cellGap;
            //standard case
            xdim -= parameters.cellGap;
            localPlane = refPlane;
            localPlane.Origin = refPlane.Origin + refPlane.XAxis * parameters.cellGap / 2 + refPlane.YAxis * parameters.cellGap / 2;
            
        }
        private void GetMissingMeshStubs()
        {
            List<Line> planarGrid = new List<Line>();
            SetFrameLines(ref nodeGrid, ref planarGrid, ref planarGrid);
            List<Line> allframes = new List<Line>();
            allframes.AddRange(frameLinesX);
            allframes.AddRange(frameLinesY);
            meshExtraSupport.SetExtraSupport(this, planarGrid,meshnodes,allframes, parameters);
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
        
        private void FindMeshNodes()
        {
            NurbsCurve boundary = CaveTools.GetPlanarPanelBoundary(this);
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
                        //is it too close to edge
                        double p = 0;
                        boundary.ClosestPoint(nodeGrid[c][d], out p);
                        Point3d boundaryPt = boundary.PointAt(p);
                        double dist = nodeGrid[c][d].DistanceTo(boundaryPt);
                        if (dist < parameters.cellGap / 2 - 5)
                        {
                            ghostNodesFound = true;
                            meshnodes[c][d].isGhost = true;
                        }
                            
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
                        meshnodes[c][d].point = closest;
                        meshnodes[c][d].pointset = true;
                        meshnodes[c][d].isGhost = true;
                        ghostNodesFound = true;
                        //RhinoDoc.ActiveDoc.Objects.AddPoint(meshnodes[c][d].point);
                    }
                    
                }
            }
            
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
            //for (int i = 0; i < meshnodes.Count; i++)
            //{
            //    for (int j = 0; j < meshnodes[i].Count; j++)
            //    {
            //        Line line = new Line();
            //        if (i < meshnodes.Count - 1 && j < meshnodes[i].Count - 1)
            //        {
            //            //meshnodes[i][j], meshnodes[i][j + 1], meshnodes[i + 1][j + 1]
                       
            //            if (FromMeshNodes(meshnodes[i][j], meshnodes[i][j + 1], ref line))
            //                DummyGSALines.Add(line);
            //            if (FromMeshNodes(meshnodes[i][j + 1], meshnodes[i + 1][j + 1], ref line))
            //                DummyGSALines.Add(line);
                        
                        
            //            if (FromMeshNodes(meshnodes[i][j], meshnodes[i + 1][j + 1], ref line))
            //                DummyGSALines.Add(line);
            //            else
            //            {
            //                if (FromMeshNodes(meshnodes[i][j + 1], meshnodes[i + 1][j], ref line))
            //                    DummyGSALines.Add(line);
            //            }
            //        }
            //        if(i == meshnodes.Count - 1 && j < meshnodes[i].Count - 1)
            //        {
            //            if (FromMeshNodes(meshnodes[i][j], meshnodes[i][j + 1], ref line))
            //                DummyGSALines.Add(line);
            //        }
            //        if(j == 0 && i < meshnodes.Count - 1)
            //            if (FromMeshNodes(meshnodes[i][j], meshnodes[i+1][j], ref line))
            //                DummyGSALines.Add(line);
            //    }
            //}
            for(int i= 0;i <  GSAmesh.TopologyEdges.Count;i++)
                DummyGSALines.Add(GSAmesh.TopologyEdges.EdgeLine(i));
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
            GetMeshVertices(ref mesh, nodes);
            if (mesh.Vertices.Count == 5)
            {

                mesh.Faces.AddFace(2, 0, 1);
                mesh.Faces.AddFace(0, 2, 3);
                mesh.Faces.AddFace(0,3,4);
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
        private void GetMeshVertices(ref Mesh mesh, List<MeshNode> nodes)
        {
            List<Point3d> vertices = new List<Point3d>();
            foreach (MeshNode mn in nodes)
                if(!mn.isGhost) vertices.Add(mn.point);

            if (nodes.Exists(x => x.isGhost))
            {
                List<Point3d> ptsPlane = new List<Point3d>();
                foreach (MeshNode mn in nodes)
                    ptsPlane.Add(localPlane.ClosestPoint(mn.point));
                //close polyline
                ptsPlane.Add(localPlane.ClosestPoint(nodes[0].point));
                Polyline pl = new Polyline(ptsPlane);
                
                foreach(Point3d p in meshExtraSupport.meshpts)
                {
                    Point3d plnPt = localPlane.ClosestPoint(p);
                    Point3d closest = pl.ClosestPoint(plnPt);
                    if (closest.DistanceTo(plnPt) < parameters.cellGap / 4)
                    {
                        vertices.Add(p);
                    }
                }
                mesh.Vertices.AddVertices(SortVertices(vertices, pl.ToNurbsCurve()));
            }
            else
            {
                mesh.Vertices.AddVertices(vertices);
            }
        }
        private List<Point3d> SortVertices(List<Point3d> points, Curve curve)
        {
            Dictionary<Point3d, double> valuePairs = new Dictionary<Point3d, double>();
            List<double> ts = new List<double>();
            foreach(Point3d p in points)
            {
                double t = 0;
                curve.ClosestPoint(p, out t);
                ts.Add(t);
                valuePairs.Add(p, t);
            }
            var ordered = valuePairs.OrderBy(x => x.Value);
            return ordered.Select(x => x.Key).ToList();
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
        public void SetFrameLines(ref List<List<Point3d>> points,ref List<Line> linesC, ref List<Line> linesD)
        {
            for (int c = 0; c < nodeGrid.Count; c++)
            {
                for (int d = 0; d < nodeGrid[c].Count; d++)
                {
                    if (points[c][d].X == 0)
                        continue;
                    if (d < nodeGrid[c].Count - 1)
                    {
                        if (points[c][d + 1].X == 0) 
                            continue;
                        Line frame = new Line(points[c][d], points[c][d + 1]);
                        if (c == nodeGrid.Count - 1)
                            frame.Flip();
                        if(c ==1 && d ==1)
                            frame.Flip();
                        linesD.Add(frame);

                    }

                    if (c < nodeGrid.Count - 1)
                    {
                        if (points[c + 1][d].X ==0 )
                            continue;
                        Line frame = new Line(points[c][d], points[c + 1][d]);
                        if (d == nodeGrid.Count - 1)
                            frame.Flip();
                        if (c == 1 && d == 1)
                            frame.Flip();
                        linesC.Add(frame);
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
                        else
                            internalStub.Add(stubMaker);
                    }
   
                    else
                        internalStub.Add(stubMaker);

                }
                frameGrid.Add(pts);
            }
        }
        public Brep subFrameBoundary()
        {
            List<Curve> curves = new List<Curve>();
            if (internalStub.Count() == 0)
            {
                curves = new List<Curve>()
                {
                    cornerStub[0].Stub.ToNurbsCurve(),

                    cornerStub[1].Stub.ToNurbsCurve(),

                    cornerStub[3].Stub.ToNurbsCurve(),

                    cornerStub[2].Stub.ToNurbsCurve()

                };
            }
            else if (internalStub.Count() == 2 && frameGrid.Count == 2)
            {
                curves = new List<Curve>()
                {
                    cornerStub[0].Stub.ToNurbsCurve(),
                    internalStub[0].Stub.ToNurbsCurve(),
                    cornerStub[1].Stub.ToNurbsCurve(),
                   
                    cornerStub[3].Stub.ToNurbsCurve(),
                    internalStub[1].Stub.ToNurbsCurve(),
                    cornerStub[2].Stub.ToNurbsCurve()
                    
                };
            }
            else if (internalStub.Count() == 2 && frameGrid.Count == 3)
            {
                curves = new List<Curve>()
                {
                    cornerStub[0].Stub.ToNurbsCurve(),
                    
                    cornerStub[1].Stub.ToNurbsCurve(),
                    internalStub[1].Stub.ToNurbsCurve(),
                    cornerStub[3].Stub.ToNurbsCurve(),
                    
                    cornerStub[2].Stub.ToNurbsCurve(),
                    internalStub[0].Stub.ToNurbsCurve()
                };
            }
            
            else
            {
                curves = new List<Curve>()
                {
                    cornerStub[0].Stub.ToNurbsCurve(),
                    internalStub[0].Stub.ToNurbsCurve(),
                    cornerStub[1].Stub.ToNurbsCurve(),
                    internalStub[3].Stub.ToNurbsCurve(),
                    cornerStub[3].Stub.ToNurbsCurve(),
                    internalStub[4].Stub.ToNurbsCurve(),
                    cornerStub[2].Stub.ToNurbsCurve(),
                    internalStub[1].Stub.ToNurbsCurve()
                };
            }
            
            
            Brep[] brep = Brep.CreateFromLoft(curves, Point3d.Unset, Point3d.Unset, LoftType.Straight, true);
            ;
            return MakeFrameBox(brep);
        }
        private Brep MakeFrameBox(Brep[] frame)
        {
            Mesh m = new Mesh();
            Mesh f = new Mesh();
            for (int i = 0; i < meshnodes.Count; i++)
            {
                for (int j = 0; j < meshnodes[i].Count; j++)
                {
                    if (i < meshnodes.Count - 1 && j < meshnodes[i].Count - 1)
                    {
                        List<Point3d> points = new List<Point3d>() { meshnodes[i][j].point, meshnodes[i][j + 1].point, meshnodes[i + 1][j + 1].point, meshnodes[i + 1][j].point };
                        FourNodeMesh(ref m, points);

                        List<Point3d> pointsF = new List<Point3d>() { frameGrid[i][j], frameGrid[i][j + 1], frameGrid[i + 1][j + 1], frameGrid[i + 1][j] };
                        FourNodeMesh(ref f, pointsF);

                    }
                }
            }
            Brep front = Brep.CreateFromMesh(m,false);
            Brep back = Brep.CreateFromMesh(f, false);
            List<Brep> breps = new List<Brep>();
            breps.Add(front);
            breps.Add(back);
            foreach (Brep b in frame)
                breps.Add(b);
            Brep[] box = Brep.JoinBreps(breps,RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);

            return box[0];
        }
        private void FourNodeMesh(ref Mesh mesh, List<Point3d> nodes)
        {
            //max for possible nodes in order
            Mesh m = new Mesh();
            m.Vertices.AddVertices(nodes);
            m.Faces.AddFace(2, 0, 1);
            m.Faces.AddFace(0, 2, 3);
            mesh.Append(m);
        }
    }
}
