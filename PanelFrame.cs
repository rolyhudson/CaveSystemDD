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
        Mesh Panel;
        Plane FitPlane;
        int number;
        int familyCount;

        List<List<Point3d>> nodeGrid = new List<List<Point3d>>();
        List<Line> hangers = new List<Line>();
        List<Line> frame = new List<Line>();
        Mesh GSAmesh;
        public List<List<MeshNode>> meshnodes = new List<List<MeshNode>>();
        public PanelFrame(Plane local, double x, Parameters param, Mesh p,int num, int groupNum)
        {
            localPlane = local;
            xdim = x;
            ydim = param.yCell;
            parameters = param;
            Panel = p;
            number = num;
            familyCount = groupNum;
            SetLocalPlane();
            SetPointGrid();
            SetMeshNodes();
            FindMeshNodes();
            
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
                CheckPoints(row);
            }
        }
        private void CheckPoints(List<Point3d> points)
        {
            foreach(Point3d p in points)
            {
                RhinoDoc.ActiveDoc.Objects.AddPoint(p);
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
                    Line edge = new Line(nodeGrid[c][d], Vector3d.ZAxis*-100000);
                    int[] faceIds;
                    Point3d[] points = Rhino.Geometry.Intersect.Intersection.MeshLine(Panel, edge, out faceIds);
                    if (points.Length > 0)
                    {
                        Point3d closest = new Point3d();
                        double minD = Double.MaxValue;
                        foreach (Point3d p in points)
                        {
                            if (p.DistanceTo(nodeGrid[c][d]) < minD)
                            {
                                closest = p;
                                minD = p.DistanceTo(nodeGrid[c][d]);
                            }
                        }
                        meshnodes[c][d].point = closest;
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
            RhinoDoc.ActiveDoc.Objects.AddMesh(GSAmesh);
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
    }
}
