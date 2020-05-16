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
        List<CaveElement> caveElements = new List<CaveElement>();
        public OrientedBox bayBoundary;
        Parameters parameters;
        List<PanelFrame> panelFrames = new List<PanelFrame>();
        List<Line> envelopeConnect = new List<Line>();
        List<Line> bridges = new List<Line>();
        List<Line> frameConnects = new List<Line>();
        public BayController(Mesh mesh, OrientedBox obox, Parameters iparameters)
        {
            slice = mesh;
            bayBoundary = obox;
            parameters = iparameters;
            SliceElements();
        }
        private void SliceElements()
        {
            CaveElement ceiling = new CaveElement(midSection(), bayBoundary.ReferencePlane, Orientation.Ceiling,parameters);
            
            //MeshToPanels(FarSide(), Orientation.SideFar);
            //MeshToPanels(NearSide(), Orientation.SideNear);
            CaveTools.CheckLines(bridges);
            CaveTools.CheckLines(frameConnects);
            CaveTools.CheckLines(envelopeConnect);
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
    }

    }
