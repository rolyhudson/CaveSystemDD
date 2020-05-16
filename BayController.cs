using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class BayController
    {

        public Mesh slice = new Mesh();
        public OrientedBox bayBoundary;
        Parameters parameters;
        List<PanelFrame> panelFrames = new List<PanelFrame>();
        List<Line> envelopeConnect = new List<Line>();
        List<Line> bridges = new List<Line>();
        List<Line> frameConnects = new List<Line>();
        public BayController (Mesh mesh, OrientedBox obox,Parameters iparameters)
        {
            slice = mesh;
            bayBoundary = obox;
            parameters = iparameters;
            SliceElements();
        }
        private void SliceElements()
        {
            MeshToPanels(midSection(),Orientation.Ceiling);
            //MeshToPanels(FarSide(), Orientation.SideFar);
            //MeshToPanels(NearSide(), Orientation.SideNear);
            CaveTools.CheckLines(bridges);
            CaveTools.CheckLines(frameConnects);
            CaveTools.CheckLines(envelopeConnect);
        }
        private void MeshToPanels(Mesh mesh, Orientation orientation)
        {
            OrientedBox orientedBox = CaveTools.FindOrientedBox(bayBoundary.ReferencePlane, mesh);
            Plane orientationPlane = orientedBox.PlaneSelection(orientation);
            double cellSize = parameters.xCell;
            double boxDim = orientedBox.xDim;

            if(orientation == Orientation.SideNear || orientation == Orientation.SideFar)
            {
                cellSize = parameters.zCell;
                boxDim = orientedBox.zDim;
            }

            int unitsX = (int)Math.Ceiling(boxDim / cellSize);
            RhinoDoc.ActiveDoc.Objects.AddBrep(orientedBox.BoundingBox);
            double dimXSpecial = boxDim - (unitsX - 1) * cellSize;

            if (dimXSpecial < parameters.cellMin)
            {
                unitsX -= 1;
                dimXSpecial = boxDim - (unitsX - 1) * cellSize;
            }

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
                RhinoDoc.ActiveDoc.Objects.AddMesh(panel);

                Plane local = new Plane(p1, orientationPlane.XAxis, orientationPlane.YAxis);
                panelFrames.Add( new PanelFrame(local, xPanel,parameters, panel,x,unitsX));
            }
            ConnectToEnvelope(orientation, orientationPlane);
        }
        private void ConnectToEnvelope(Orientation orientation, Plane orientationPlane)
        {
            

            for (int i = 0;i< panelFrames.Count; i ++)
            {
                if(i< panelFrames.Count - 1)
                {
                    SetHangerAssembly(panelFrames[i].frameCorners[2], panelFrames[i + 1].frameCorners[0], orientation, orientationPlane);
                    SetHangerAssembly(panelFrames[i].frameCorners[3], panelFrames[i + 1].frameCorners[1], orientation, orientationPlane);
                }
                if (i == 0)
                {
                    SimpleHanger(panelFrames[i].frameCorners[0], orientation, orientationPlane);
                    SimpleHanger(panelFrames[i].frameCorners[1], orientation, orientationPlane);
                }
                if (i == panelFrames.Count - 1)
                {
                    SimpleHanger(panelFrames[i].frameCorners[2], orientation, orientationPlane);
                    SimpleHanger(panelFrames[i].frameCorners[3], orientation, orientationPlane);
                }
            }
        }
        private void SimpleHanger(Point3d p1, Orientation orientation, Plane orientationPlane)
        {
            List<Brep> breps = parameters.roofs;

            if (orientation == Orientation.SideNear || orientation == Orientation.SideFar)
                breps = parameters.walls;
            if (orientation == Orientation.Floor)
                breps = parameters.floors;

            Point3d envelope = CaveTools.ClosestProjected(breps, p1, orientationPlane.Normal * -1);
            envelopeConnect.Add(new Line(p1, envelope));
        }
        private void SetHangerAssembly(Point3d p1 ,Point3d p2, Orientation orientation, Plane orientationPlane)
        {
            double bridgeZ = Math.Max(p1.Z, p2.Z) + parameters.FrameBridge;
            Point3d b1 = new Point3d(p1.X, p1.Y, bridgeZ);
            Point3d b2 = new Point3d(p2.X, p2.Y, bridgeZ);
            Line bridge = new Line(b1, b2);
            bridges.Add(bridge);
            frameConnects.Add(new Line(b1, p1));
            frameConnects.Add(new Line(b2, p2));
            Point3d mid = bridge.PointAt(0.5);
            List<Brep> breps = parameters.roofs;

            if (orientation == Orientation.SideNear || orientation == Orientation.SideFar)
                breps = parameters.walls;
            if (orientation == Orientation.Floor)
                breps = parameters.floors;

            
            Point3d envelope = CaveTools.ClosestProjected(breps, mid, orientationPlane.Normal *-1);
            envelopeConnect.Add(new Line(mid, envelope));

            
        }
        private Mesh midSection()
        {
            Plane plane1 = OrientedBox.FaceOffsetPlane(bayBoundary.SideXmax, 2500);
            plane1.Flip();
            Plane plane2 = OrientedBox.FaceOffsetPlane(bayBoundary.SideXmin, 2000);
            plane2.Flip();
            Mesh ceiling = CaveTools.splitTwoPlanes(plane1, plane2, slice);
            //RhinoDoc.ActiveDoc.Objects.AddMesh(ceiling);
            return ceiling;
        }
        private Mesh FarSide()
        {
            Plane plane1 = OrientedBox.FaceOffsetPlane(bayBoundary.SideXmax, 2500);
            Mesh side = CaveTools.split(slice, plane1);
            //RhinoDoc.ActiveDoc.Objects.AddMesh(side);
            return side;
        }
        private Mesh NearSide()
        {
            Plane plane1 = OrientedBox.FaceOffsetPlane(bayBoundary.SideXmin, 2000);
            Mesh side = CaveTools.split(slice, plane1);
            //RhinoDoc.ActiveDoc.Objects.AddMesh(side);
            return side;
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
    public enum Orientation
    {
        Ceiling,
        Floor,
        SideNear,
        SideFar
    }
}
