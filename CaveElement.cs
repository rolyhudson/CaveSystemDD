using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class CaveElement
    {
        Parameters parameters;
        public List<PanelFrame> panelFrames = new List<PanelFrame>();
        public SupportAssembly supportAssembly;
        OrientedBox orientedBox;
        Plane orientationPlane;
        public Orientation orientation;
        Plane BayXY = new Plane();
        Mesh mesh;
        public CaveElement(Mesh imesh, Plane bayPlane,Orientation iorientation, Parameters iparameters)
        {
            parameters = iparameters;
            orientation = iorientation;
            
            mesh = imesh;
            BayXY = bayPlane;
            if (mesh == null)
                return;

            orientedBox = CaveTools.FindOrientedBox(bayPlane, imesh, parameters.yCell);
            RhinoDoc.ActiveDoc.Objects.AddBrep(orientedBox.BoundingBox);
            orientationPlane = orientedBox.PlaneSelection(orientation);
            //OrientedBox.CheckPlane(orientationPlane);
            supportAssembly = new SupportAssembly(parameters, orientation, orientationPlane);

            MeshToPanels();
            supportAssembly.ConnectToEnvelope(panelFrames);
        }
        private void MeshToPanels()
        {
            
            double cellSize = parameters.zCell;
            double boxDim = orientedBox.zDim;

            if (orientation == Orientation.Ceiling) 
            {
                cellSize = parameters.xCell;
                boxDim = orientedBox.xDim;
            }


            int panelNum = 0;
            double cumulativeDim = 0;
            Point3d p1 = orientationPlane.Origin;
            Point3d p2 = orientationPlane.Origin + orientationPlane.XAxis * cellSize;
            double xPanel = cellSize;
            double yPanel = parameters.yCell;
            if (orientation == Orientation.SideWest || orientation == Orientation.SideEast)
                yPanel = orientedBox.xDim;
            bool lastPanel = false;
            double lastPanelX = 2000;
            
            while (cumulativeDim < boxDim)
            {
                Plane cut1 = new Plane(p1, orientationPlane.XAxis);
                Plane cut2 = new Plane(p2, orientationPlane.XAxis * -1);
                Mesh panel = SelectClosestPanel(CaveTools.splitTwoPlanes(cut1, cut2, mesh), orientationPlane);
 
                Plane local = new Plane(p1, orientationPlane.XAxis, orientationPlane.YAxis);
                //area check
                double panelArea = 0;
                panel = FindPanelByArea(cut1,ref cut2, xPanel, ref panelArea);
                bool updateLastFrame = false;
                if (lastPanel)
                {
                    xPanel = boxDim - orientationPlane.Origin.DistanceTo(p1);
                    if (xPanel < parameters.cellMin)
                    {
                        //try and combine with previous
                        if(panelFrames[panelNum-1].panelArea + panelArea < 9e6 && xPanel + panelFrames[panelNum-1].xdim < parameters.xCell + parameters.cellMin)
                        {
                            p1 = panelFrames[panelNum - 1].refPlane.Origin;
                            local = new Plane(p1, orientationPlane.XAxis, orientationPlane.YAxis);
                            cut1 = new Plane(p1, orientationPlane.XAxis);
                            cut2 = new Plane(p2, orientationPlane.XAxis * -1);
                            
                            panel = FindPanelByArea(cut1, ref cut2, xPanel, ref panelArea);
                            OrientedBox panelBox = CaveTools.FindOrientedBox(BayXY, panel, parameters.yCell);
                            xPanel = panelBox.xDim;
                            //RhinoDoc.ActiveDoc.Objects.AddMesh(panel);
                            updateLastFrame = true;
                        }
                        else
                            lastPanelX = parameters.cellMin;
                    }
                }
                else
                {
                    xPanel = cut1.Origin.DistanceTo(cut2.Origin);
                }
                
                cumulativeDim += xPanel;
                if (updateLastFrame)
                    panelFrames.RemoveAt(panelNum - 1);
                
                    
                panelFrames.Add(new PanelFrame(local, xPanel, parameters, panel, panelNum, yPanel));
                if (lastPanel)
                    break;
                //set p1 and p2 for next panel
                p1 = cut2.Origin;
                p2 = p1 + orientationPlane.XAxis * cellSize;
                if (orientationPlane.Origin.DistanceTo(p2) > boxDim)
                    lastPanel = true;

                panelNum++;
            }
            foreach (PanelFrame panelFrame in panelFrames)
                panelFrame.CheckGeometry();
        }
        private Mesh FindPanelByArea(Plane start,ref Plane end,double xdim, ref double area)
        {
            Mesh panel = SelectClosestPanel(CaveTools.splitTwoPlanes(start, end, mesh), orientationPlane);
            AreaMassProperties amp = AreaMassProperties.Compute(panel);
            if (amp.Area < 9e6)
                return panel;
            int attempt = 10;
            double dist = xdim /2;
            
            while (attempt > 0)
            {
                //adjust plane
                if (amp.Area > 9e6)
                    end.Origin = end.Origin + end.Normal * dist;
                else
                    end.Origin = end.Origin - end.Normal * dist;
                panel = SelectClosestPanel(CaveTools.splitTwoPlanes(start, end, mesh), orientationPlane);
                amp = AreaMassProperties.Compute(panel);
                attempt--;
                dist = dist / 2;
            }
            area = amp.Area;
            return panel;
        }
        private Mesh SelectClosestPanel(Mesh m, Plane plane)
        {
            if (m.DisjointMeshCount > 1)
            {
                double minDist = double.MaxValue;
                Mesh closest = new Mesh();
                foreach (Mesh d in m.SplitDisjointPieces())
                {
                    Point3d centroid = CaveTools.averagePoint(d);
                    if (centroid.DistanceTo(plane.ClosestPoint(centroid)) < minDist)
                    {
                        minDist = centroid.DistanceTo(plane.ClosestPoint(centroid));
                        closest = d;
                    }
                }
                return closest;
            }
            else
                return m;
        }

    }
}

