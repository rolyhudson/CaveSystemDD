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
        public List<CaveElement> caveElements = new List<CaveElement>();
        public OrientedBox bayBoundary;
        public Plane ReferencePlane;
        Parameters parameters;

        public BayController(Plane iplane, Parameters iparameters)
        {
            //slice = mesh;
            //bayBoundary = obox;
            ReferencePlane = iplane;
            parameters = iparameters;
            SliceElements();
        }
        private void SliceElements()
        {
            List<string> meshLayers = new List<string>()
            {
                "CeilingMesh",
                "WALL-N",
                "WALL-S"
            };
            Orientation orientation = Orientation.Ceiling;
            foreach(string layer in meshLayers)
            {
                List<Mesh> meshes = SelectMeshes(layer);
                switch (layer)
                {
                    case "CeilingMesh":
                        orientation = Orientation.Ceiling;
                        break;
                    case "WALL-N":
                        orientation = Orientation.SideNear;
                        break;
                    case "WALL-S":
                        orientation = Orientation.SideFar;
                        break;
                    default:
                        orientation = Orientation.Ceiling;
                        break;
                }
                foreach (Mesh m in meshes)
                {
                    Brep minVol = CaveTools.findBBoxGivenPlane(ReferencePlane, m);
                    //RhinoDoc.ActiveDoc.Objects.AddBrep(minVol);
                    CaveElement element = new CaveElement(m, ReferencePlane, orientation, parameters);
                    caveElements.Add(element);
                }
            }
        }
        private List<Mesh> SelectMeshes(string layerName)
        {
            RhinoObject[] objs = RhinoDoc.ActiveDoc.Objects.FindByLayer(layerName );
            if (objs == null)
                return null;
            Plane pln1 = new Plane(ReferencePlane.Origin - ReferencePlane.YAxis * 10, ReferencePlane.YAxis);
            Plane pln2 = new Plane(ReferencePlane.Origin + ReferencePlane.YAxis * (parameters.yCell+10), ReferencePlane.YAxis * -1);
            List<Mesh> contained = new List<Mesh>();
            foreach (RhinoObject obj in objs)
            {
                if(obj.ObjectType == ObjectType.Mesh)
                {
                    Mesh m = obj.Geometry as Mesh;
                    if (CaveTools.MeshInsidePlanes(pln1, pln2, m))
                        contained.Add(m);
                }
            }
            return contained;
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
