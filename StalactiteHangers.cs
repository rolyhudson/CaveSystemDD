using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Accord.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.MachineLearning;

namespace CaveSystem2020
{
    class StalactiteHangers
    {
        public List<Line> hangers = new List<Line>();
        List<Mesh> stalactites = new List<Mesh>();
        List<Point3d> minima = new List<Point3d>();
        public StalactiteHangers()
        {
            GetMeshes();
            foreach (Mesh mesh in stalactites)
                minima.AddRange(FindLocalMinima(mesh));
            foreach (Point3d p in minima)
                RhinoDoc.ActiveDoc.Objects.AddPoint(p);
        }
        private void GetMeshes()
        {
            RhinoObject[] objs = RhinoDoc.ActiveDoc.Objects.FindByLayer("stalactites");
            if (objs == null)
                return;
            foreach (RhinoObject obj in objs)
            {
                if (obj.ObjectType == ObjectType.Mesh)
                {
                    Mesh m = obj.Geometry as Mesh;
                    foreach (Mesh part in m.SplitDisjointPieces())
                    {
                        stalactites.Add(part);
                    }
                    
                }
            }
        }
        private List<Point3d> FindLocalMinima(Mesh mesh)
        {
            List<Point3d> meshMinima = new List<Point3d>();
            KDTree<double> tree = SetKDTree(mesh);
            foreach (Point3d s in mesh.TopologyVertices)
            {
                double[] query = new double[] { s.X, s.Y, s.Z };
                var neighbours = tree.Nearest(query,neighbors: 20);
                bool isMin = true;
                foreach(var n in neighbours)
                {
                    if(n.Node.Position[2] < s.Z)
                    {
                        isMin = false;
                        break;
                    }

                }
                 if(isMin)
                    meshMinima.Add(s);
            }
            if (meshMinima.Count > 1)
            {
                meshMinima = KMeansReduce(meshMinima, mesh);
            }
            return meshMinima;
        }
        private List<Point3d> KMeansReduce(List<Point3d> points, Mesh m)
        {
            List<Point3d> reduced = new List<Point3d>();
            Polyline[] outlines = m.GetOutlines(Plane.WorldXY);
            double area = 0;
            foreach(Polyline pl in outlines)
            {
                AreaMassProperties amp = AreaMassProperties.Compute(pl.ToNurbsCurve());
                area += amp.Area;
            }
            int nClusters = (int)(area / 1000000);
            List<double[]> pts2d = new List<double[]>();
            if (nClusters == 0)
            {
                double minZ = points.Min(x => x.Z);
                reduced.Add(points.Find(p => p.Z == minZ));
                return reduced;
            }
            foreach(Point3d p in points)
            {
                pts2d.Add(new double[] {p.X,p.Y });
            }
            if (points.Count < nClusters)
                return points;

            KMeans kmeans = new KMeans(k: nClusters);

            // Compute and retrieve the data centroids
            var clusters = kmeans.Learn(pts2d.ToArray());

            // Use the centroids to parition all the data
            int[] labels = clusters.Decide(pts2d.ToArray());

            return reduced;
        }
        private static KDTree<double> SetKDTree(Mesh mesh)
        {
            List<double[]> points = new List<double[]>();
            List<Point3d> vertices = new List<Point3d>();
            foreach (Point3d s in mesh.TopologyVertices)
            {
                double[] pt = new double[] { s.X, s.Y, s.Z };
                points.Add(pt);
                vertices.Add(s);
            }
            // To create a tree from a set of points, we use
            KDTree<double> tree = KDTree.FromData<double>(points.ToArray());

            return tree;
        }
        public void AddStalactiteSupport(CaveElement caveElement)
        {
            //find points in bay
            Plane pln1 = new Plane(caveElement.BayXY.Origin - caveElement.BayXY.YAxis, caveElement.BayXY.YAxis);
            Plane pln2 = new Plane(caveElement.BayXY.Origin + caveElement.BayXY.YAxis * 3000, caveElement.BayXY.YAxis * -1);
            List<Point3d> bayMinima = CaveTools.PointsInsidePlane(minima, pln1, pln2);
            foreach (PanelFrame panelFrame in caveElement.panelFrames)
            {
                SetHangers(panelFrame, bayMinima);
            }
        }
        private void SetHangers(PanelFrame panelFrame, List<Point3d> bayMinima)
        {
            foreach (Point3d p in bayMinima)
            {
                if (panelFrame.UnitBoundary.Contains(p, panelFrame.localPlane, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) == PointContainment.Inside)
                {
                    Point3d planePt = panelFrame.localPlane.ClosestPoint(p);
                    
                    List<Line> frameLinesX = new List<Line>();
                    List<Line> frameLinesY = new List<Line>();
                    Plane pln = new Plane(planePt, panelFrame.localPlane.YAxis);
                    panelFrame.SetFrameLines(ref panelFrame.nodeGrid, ref frameLinesX, ref frameLinesY);
                    Dictionary<Point3d, double> pointDist = new Dictionary<Point3d, double>();
                    double t = 0;
                    bool shiftStalac = false;
                    foreach(Line frame  in frameLinesY)
                    {
                        Rhino.Geometry.Intersect.Intersection.LinePlane(frame, pln, out t);
                        if(t >= 0 && t <=1)
                        {

                            Point3d interPt = frame.PointAt(t);
                            if (interPt.DistanceTo(frame.To) < 300)
                            {
                                shiftStalac = true;
                                interPt = frame.To;
                            }
                                
                            if (interPt.DistanceTo(frame.From) < 300)
                            {
                                shiftStalac = true;
                                interPt = frame.From;
                            }

                            pointDist.Add(interPt, interPt.DistanceTo(planePt));
                        }
                        
                    }
                    if (pointDist.Count < 2)
                    {
                        Line vertical = new Line(p, planePt);
                        panelFrame.stalactiteVertical.Add(vertical);
                        RhinoDoc.ActiveDoc.Objects.AddLine(vertical);
                        continue;
                    }
                    var myList = pointDist.ToList();

                    myList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
                    Point3d s = myList[0].Key;
                    Point3d e = myList[1].Key;
                    Line startLine = new Line(s, panelFrame.localPlane.ZAxis * 100000);
                    Line endLine = new Line(e, panelFrame.localPlane.ZAxis * 100000);
                    
                    Point3d Start = new Point3d();
                    Point3d End = new Point3d();
                    double p1 = 0;
                    double p2 = 0;
                    foreach (Line frame in panelFrame.frameLinesY)
                    {
                        bool inter1 = Rhino.Geometry.Intersect.Intersection.LineLine(startLine, frame, out p1, out p2);
                        if(startLine.PointAt(p1).DistanceTo(frame.PointAt(p2)) < 5)
                        {
                            if(p2>=0&& p2<=1 && p1 >= 0 && p1 <= 1)
                                Start = frame.PointAt(p2);
                            
                        }
                        
                    }
                    foreach (Line frame in panelFrame.frameLinesY)
                    {
                        bool inter2 = Rhino.Geometry.Intersect.Intersection.LineLine(endLine, frame, out p1, out p2);
                        if (endLine.PointAt(p1).DistanceTo(frame.PointAt(p2)) < 5)
                        {
                             if(p2>=0 && p2<=1 && p1 >= 0 && p1 <= 1)
                                    End = frame.PointAt(p2);
                            
                        }
                    }
                    if(End.Z ==0 || Start.Z == 0)
                    {
                        Line vertical = new Line(p, planePt);
                        panelFrame.stalactiteVertical.Add(vertical);
                        RhinoDoc.ActiveDoc.Objects.AddLine(vertical);
                    }
                    else
                    {
                        Line sFrame = new Line(Start, End);
                        //if(shiftStalac)

                        Line vLine = new Line(p, panelFrame.localPlane.ZAxis * 100000);
                        bool inter3 = Rhino.Geometry.Intersect.Intersection.LineLine(vLine, sFrame, out p1, out p2);


                        Line vertical = new Line(p, sFrame.PointAt(p2));
                        panelFrame.stalactiteSubFrame.Add(sFrame);
                        panelFrame.stalactiteVertical.Add(vertical);
                        RhinoDoc.ActiveDoc.Objects.AddLine(vertical);
                        RhinoDoc.ActiveDoc.Objects.AddLine(sFrame);
                    }
                    
                    
                }
            }
        }
    }
}
