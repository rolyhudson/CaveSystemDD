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
            //orientedBox = CaveTools.FindOrientedBox(bayBoundary.ReferencePlane, mesh);
            orientedBox = CaveTools.FindOrientedBox(bayPlane, imesh);
            orientationPlane = orientedBox.PlaneSelection(orientation);
            supportAssembly = new SupportAssembly(parameters, orientation, orientationPlane);

            MeshToPanels();
            supportAssembly.ConnectToEnvelope(panelFrames);
        }
        private void MeshToPanels()
        {
            
            double cellSize = parameters.xCell;
            double boxDim = orientedBox.xDim;

            if (orientation == Orientation.SideNear || orientation == Orientation.SideFar)
            {
                cellSize = parameters.zCell;
                boxDim = orientedBox.zDim;
            }

            int unitsX = (int)Math.Ceiling(boxDim / cellSize);
            //RhinoDoc.ActiveDoc.Objects.AddBrep(orientedBox.BoundingBox);
            double dimXSpecial = boxDim - (unitsX - 1) * cellSize;

            if (dimXSpecial < parameters.cellMin)
            {
                unitsX -= 1;
                dimXSpecial = boxDim - (unitsX - 1) * cellSize;
            }
            int panelNum = 0;
            for (int x = 0; x < unitsX; x++)
            {
                double xPanel = cellSize;
                Point3d p1 = orientationPlane.Origin + orientationPlane.XAxis * x * cellSize;
                Point3d p2 = orientationPlane.Origin + orientationPlane.XAxis * (x + 1) * cellSize;
                if (x == unitsX - 1)
                {
                    xPanel = dimXSpecial;
                    p2 = orientationPlane.Origin + orientationPlane.XAxis * boxDim;
                }

                Plane cut1 = new Plane(p1, orientationPlane.XAxis);
                Plane cut2 = new Plane(p2, orientationPlane.XAxis * -1);

                Mesh panel = SelectClosestPanel(CaveTools.splitTwoPlanes(cut1, cut2, mesh), orientationPlane);
                //RhinoDoc.ActiveDoc.Objects.AddMesh(panel);
                OrientedBox panelBox = CaveTools.FindOrientedBox(BayXY,panel);
                Plane local = new Plane(p1, orientationPlane.XAxis, orientationPlane.YAxis);
                //area check
                panel = FindPanelByArea(cut1,ref cut2, xPanel);

                panelFrames.Add(new PanelFrame(local, xPanel, parameters, panel, panelNum, unitsX,orientedBox.yDim));
                panelNum++;
            }
            
        }
        private Mesh FindPanelByArea(Plane start,ref Plane end,double xdim)
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

