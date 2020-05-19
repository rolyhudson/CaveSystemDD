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
        Plane FramePlane;
        int number;
        int familyCount;
        public bool FailedFrame = false;

        List<List<Point3d>> nodeGrid = new List<List<Point3d>>();
        List<List<Point3d>> frameGrid = new List<List<Point3d>>();
        public List<Line> internalStub  = new List<Line>();
        public List<Line> subFrame = new List<Line>();
        public List<Point3d> frameCorners = new List<Point3d>();
        public Mesh GSAmesh;
        public List<List<MeshNode>> meshnodes = new List<List<MeshNode>>();
        public List<Line> cornerStub = new List<Line>();
        public PanelFrame(Plane local, double x, Parameters param, Mesh p, int num, int groupNum)
        {
            localPlane = local;
            xdim = x;
            ydim = param.yCell;
            parameters = param;
            CavePanels = p;
            number = num;
            familyCount = groupNum;
            SetLocalPlane();
            SetPointGrid();
            SetMeshNodes();
            FindMeshNodes();
            SetFramePlane();
            SetFrameGrid();
            SetFrameLines();
            SetHangerLines();
            CheckGeometry();
        }
        private void CheckGeometry()
        {
            if (!FailedFrame)
            {
                CaveTools.CheckLines(subFrame);
                CaveTools.CheckLines(internalStub);
                CaveTools.CheckLines(cornerStub);
            }
            RhinoDoc.ActiveDoc.Objects.AddMesh(GSAmesh);
        }
        private void SetLocalPlane()
        {
            ydim -= parameters.cellGap;
            //first and last are different
            //allowing for 2mm to ensure intersections
            if (number == 0)
            {
                xdim -= parameters.cellGap / 2;
                localPlane.Origin = localPlane.Origin + localPlane.XAxis * 5 + localPlane.YAxis * parameters.cellGap / 2;
            }
            else if (number == familyCount - 1)
            {
                xdim -= parameters.cellGap/2 - 2;
                localPlane.Origin = localPlane.Origin + localPlane.XAxis * (parameters.cellGap / 2 -5) + localPlane.YAxis * parameters.cellGap / 2;
            }
            else
            {
                //standard case
                xdim -= parameters.cellGap;
                localPlane.Origin = localPlane.Origin + localPlane.XAxis * parameters.cellGap / 2 + localPlane.YAxis * parameters.cellGap / 2;
            }
        }
        private void SetPointGrid()
        {
            int numx = 3;
            int numy = 3;
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
                    else meshnodes[c][d].pointset = false;
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
                mesh.Faces.AddFace(0, 1, 2);
                mesh.Faces.AddFace(2, 0, 3);
            }
            if (mesh.Vertices.Count == 3)
            {
                mesh.Faces.AddFace(0, 1, 2);
            }
            return mesh;
        }
        private void SetFramePlane()
        {
            //List<Point3d> points = meshnodes.SelectMany(d => d.Select(m => m.point)).ToList();
            List<Point3d> points = new List<Point3d>();
            foreach (Point3d p in CavePanels.Vertices)
                points.Add(p);
            Plane.FitPlaneToPoints(points, out FramePlane);
            if (Vector3d.VectorAngle(FramePlane.Normal,localPlane.Normal) < Math.PI/2)
                FramePlane.Flip();
            //find closest point above plane
            double maxDist = double.MinValue;
            Point3d closest = new Point3d();
            foreach (Point3d p in points)
            {
                if (CaveTools.pointInsidePlane(p, FramePlane))
                {
                    double dist = FramePlane.ClosestPoint(p).DistanceTo(p);
                    if (dist > maxDist)
                    {
                        maxDist = dist;
                        closest = p;
                    }
                }
            }
            FramePlane.Origin = closest + FramePlane.Normal * parameters.FramePlaneMesh;
            //OrientedBox.CheckPlane(FramePlane);
        }
        private void SetFrameGrid()
        {
            frameGrid = new List<List<Point3d>>();
            frameCorners = new List<Point3d>();
            for (int c = 0; c < nodeGrid.Count; c++)
            {
                List<Point3d> points = new List<Point3d>();
                for (int d = 0; d < nodeGrid[c].Count; d++)
                {
                    double t = 0;
                    Point3d p = new Point3d();
                    Line line = new Line(nodeGrid[c][d], localPlane.ZAxis);

                    if (Rhino.Geometry.Intersect.Intersection.LinePlane(line, FramePlane, out t))
                    {
                        p = line.PointAt(t);
                        points.Add(p);
                        if (c == 0 || c == nodeGrid.Count - 1)
                        {
                            if (d == 0 || d == nodeGrid[c].Count - 1)
                                frameCorners.Add(p);
                        }
                    }
                    else
                        //missed intersection this is a failed frame
                        FailedFrame = true;
                }
                frameGrid.Add(points);
            }
            CheckPlane();
        }
        private void CheckPlane()
        {
            double totalDist = 0;
            int count = 0;
            for (int c = 0; c < meshnodes.Count; c++)
            {
                for (int d = 0; d < meshnodes[c].Count; d++)
                {
                    if (meshnodes[c][d].pointset)
                    {
                        totalDist += meshnodes[c][d].point.DistanceTo(frameGrid[c][d]);
                        count++;
                    }
                }
            }
            //if we already have a frame plane matching local
            if (Vector3d.VectorAngle(localPlane.ZAxis,FramePlane.ZAxis)< 0.0174533) return;

            if(totalDist/count> parameters.xCell/2)
                FailedFrame = true;

            if(FailedFrame)
            {
                HorizontalPlane();
                SetFrameGrid();
            }
                

        }
        private void SetFrameLines()
        {
            try
            {
                for (int c = 0; c < frameGrid.Count; c++)
                {
                    for (int d = 0; d < frameGrid[c].Count; d++)
                    {
                        if (d < frameGrid[c].Count - 1)
                        {
                            if (c == 0 || c == frameGrid[c].Count - 1)
                                subFrame.Add(new Line(frameGrid[c][d], frameGrid[c][d + 1]));
                        }

                        if (c < frameGrid.Count - 1)
                            subFrame.Add(new Line(frameGrid[c][d], frameGrid[c + 1][d]));
                    }
                }
            }
            catch
            {
                FailedFrame = true;
            }
            
        }
        private void HorizontalPlane()
        {
            double minDist = double.MaxValue;
            Point3d closest = new Point3d();
            foreach(Point3d v in CavePanels.Vertices)
            {
                if(v.DistanceTo(localPlane.ClosestPoint(v))<minDist)
                {
                    minDist = v.DistanceTo(localPlane.ClosestPoint(v));
                    closest = v;
                }
            }

            FramePlane = localPlane;
            FramePlane.Origin = closest + localPlane.ZAxis * - parameters.FramePlaneMesh;
            FailedFrame = false;
        }
        private void SetHangerLines()
        {
            for (int c = 0; c < meshnodes.Count; c++)
            {
                for (int d = 0; d < meshnodes[c].Count; d++)
                {
                    if (meshnodes[c][d].pointset)
                    {
                        if (c == 0 || c == nodeGrid.Count - 1)
                        {
                            if (d == 0 || d == nodeGrid[c].Count - 1)
                               cornerStub.Add(new Line(frameGrid[c][d], meshnodes[c][d].point));
                        }
                        else
                        {
                            internalStub.Add(new Line(frameGrid[c][d], meshnodes[c][d].point));
                        }
                    } 
                }
            }
        }
    }
}
