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
        }
        private void MeshToPanels(Mesh mesh, Orientation orientation)
        {
            OrientedBox orientedBox = CaveTools.FindOrientedBox(bayBoundary.ReferencePlane, mesh);
            Plane orientationPlane = orientedBox.PlaneSelection(orientation);

            int unitsX = (int)Math.Ceiling(orientedBox.xDim / parameters.xCell);
            double dimXSpecial = orientedBox.xDim - (unitsX - 1) * parameters.xCell;

            if (dimXSpecial < parameters.cellMin)
            {
                unitsX -= 1;
                dimXSpecial = orientedBox.xDim - (unitsX - 1) * parameters.xCell;
            }

            for (int x = 0; x < unitsX; x++)
            {
                double xPanel = parameters.xCell;
                Point3d p1 = orientationPlane.Origin + orientationPlane.XAxis * x * parameters.xCell;
                Point3d p2 = orientationPlane.Origin + orientationPlane.XAxis * (x + 1) * parameters.xCell;
                if (x == unitsX - 1)
                {
                    xPanel = dimXSpecial;
                    p2 = orientationPlane.Origin + orientationPlane.XAxis * orientedBox.xDim;
                }
                    
                Plane cut1 = new Plane(p1, orientationPlane.XAxis);
                Plane cut2 = new Plane(p2, orientationPlane.XAxis * -1);
                Mesh panel = SelectClosestPanel(CaveTools.splitTwoPlanes(cut1, cut2, mesh), orientationPlane);
                RhinoDoc.ActiveDoc.Objects.AddMesh(panel);

                Plane local = new Plane(p1, orientationPlane.XAxis, orientationPlane.YAxis);
                PanelFrame panelFrame = new PanelFrame(local, xPanel,parameters, panel,x,unitsX);
            }
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
