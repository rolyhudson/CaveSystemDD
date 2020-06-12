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
                //"CeilingMesh",
                //"WALL-N",
                //"WALL-S",
                "WALL-E",
                "WALL-W"
            };
            Orientation orientation = Orientation.Ceiling;
            foreach(string layer in meshLayers)
            {
                List<Mesh> meshes = SelectMeshes(layer, ReferencePlane);
                switch (layer)
                {
                    case "CeilingMesh":
                        orientation = Orientation.Ceiling;
                        break;
                    case "WALL-N":
                        orientation = Orientation.SideNorth;
                        break;
                    case "WALL-S":
                        orientation = Orientation.SideSouth;
                        break;
                    case "WALL-E":
                        orientation = Orientation.SideEast;
                        break;
                    case "WALL-W":
                        orientation = Orientation.SideWest;
                        break;
                    default:
                        orientation = Orientation.Ceiling;
                        break;
                }
                if (layer == "WALL-E" || layer == "WALL-W")
                {
                    List<Mesh> splits = new List<Mesh>();
                    foreach (Mesh m in meshes)
                    {
                        OrientedBox oBox = CaveTools.FindOrientedBox(ReferencePlane, m, parameters.yCell);
                        splits.AddRange(WallSplit(oBox, m));
                    }
                    meshes = splits;
                }
                meshes = NearFragments(meshes);
                foreach (Mesh m in meshes)
                {

                    CaveElement element = new CaveElement(m, ReferencePlane, orientation, parameters);
                    caveElements.Add(element);
                }
            }
        }
        private List<Mesh> SelectMeshes(string layerName, Plane plane)
        {
            RhinoObject[] objs = RhinoDoc.ActiveDoc.Objects.FindByLayer(layerName );
            if (objs == null)
                return null;
            Plane pln1 = new Plane(plane.Origin - plane.YAxis * 10, plane.YAxis);
            Plane pln2 = new Plane(plane.Origin + plane.YAxis * (parameters.yCell+10), plane.YAxis * -1);
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
        private List<Mesh> NearFragments( List<Mesh> mainMeshes)
        {
            List<Mesh> frags = new List<Mesh>();
            Plane prev = new Plane(ReferencePlane);
            prev.Origin = prev.Origin - ReferencePlane.YAxis * parameters.yCell/2;
            frags.AddRange(SelectMeshes("fragments", prev));

            Plane next = new Plane(ReferencePlane);
            next.Origin = prev.Origin + ReferencePlane.YAxis * 1.5 * parameters.yCell;
            frags.AddRange(SelectMeshes("fragments", next));

            if (frags.Count == 0)
                return mainMeshes;
            List<Mesh> rejoined = new List<Mesh>();
            foreach (Mesh m in mainMeshes)
            {
                List<Mesh> connected = new List<Mesh>();
                foreach(Mesh f in frags)
                {
                    Mesh join = new Mesh();
                    join.Append(m);
                    join.Append(f);
                    if (join.DisjointMeshCount == 1)
                        connected.Add(f);
                }
                if (connected.Count > 0)
                {
                    Mesh joined = new Mesh();
                    joined.Append(m);
                    joined.Append(connected);
                    rejoined.Add(joined);
                }
                else
                    rejoined.Add(m);
            }
            return rejoined;
        }
        private List<Mesh> WallSplit(OrientedBox orientedBox, Mesh mesh)
        {
            double cellSize = parameters.yCell;
            double boxDim = orientedBox.xDim;
            Plane orientationPlane = orientedBox.SideZmaxPlane;
            double cumulativeDim = 0;
            Point3d p1 = orientationPlane.Origin;
            Point3d p2 = orientationPlane.Origin + orientationPlane.XAxis * cellSize;
            double xPanel = cellSize;
            List<Mesh> parts = new List<Mesh>();
            bool lastPanel =false;
            
            while (cumulativeDim < boxDim)
            {
                
                if (lastPanel)
                {
                    xPanel = boxDim - orientationPlane.Origin.DistanceTo(p1);
                    if (xPanel < parameters.cellMin)
                    {
                        //move back to previous
                        p1 = p1 - orientationPlane.XAxis * cellSize;
                        parts.RemoveAt(parts.Count() - 1);
                    }
                }
                Plane cut1 = new Plane(p1, orientationPlane.XAxis);
                Plane cut2 = new Plane(p2, orientationPlane.XAxis * -1);
                Mesh panel = CaveTools.splitTwoPlanes(cut1, cut2, mesh);
                parts.Add(panel);
                
                cumulativeDim += cellSize;
 
                //set p1 and p2 for next part
                p1 = cut2.Origin;
                p2 = p1 + orientationPlane.XAxis * cellSize;
                if (orientationPlane.Origin.DistanceTo(p2) > boxDim)
                    lastPanel = true;
            }
            foreach (Mesh m in parts)
                RhinoDoc.ActiveDoc.Objects.AddMesh(m);
            return parts;
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
