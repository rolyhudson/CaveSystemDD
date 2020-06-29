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
        StalactiteHangers stalactiteHangers;

        public BayController(Plane iplane, Parameters iparameters, StalactiteHangers hangers)
        {
            //slice = mesh;
            //bayBoundary = obox;
            ReferencePlane = iplane;
            parameters = iparameters;
            stalactiteHangers = hangers;
            SliceElements();
            ClashFix clashFix = new ClashFix(caveElements);
            CheckPanelGeometry();
            setStalactites();
        }
        private void setStalactites()
        {
            foreach(CaveElement caveElement in caveElements)
            {
                if(caveElement.orientation == Orientation.Ceiling)
                    stalactiteHangers.AddStalactiteSupport(caveElement);
            }
                
        }
        private void SliceElements()
        {
            List<string> meshLayers = new List<string>()
            {
                "HANG",
                "WALL-N",
                "WALL-S",
                "WALL-E",
                "WALL-W"
            };
            Orientation orientation = Orientation.Ceiling;
            foreach(string layer in meshLayers)
            {
                List<Mesh> meshes = SelectMeshes(layer, ReferencePlane);
                meshes = NearFragments(meshes);
                switch (layer)
                {
                    case "HANG":
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
                        List<Mesh> refFrags = SelectMeshes("columnRef", ReferencePlane);
                        Mesh refMesh = new Mesh();
                        refMesh.Append(m);
                        refMesh.Append(refFrags);
                        OrientedBox oBox = CaveTools.FindOrientedBox(ReferencePlane, refMesh, parameters.yCell);
                        splits.AddRange(WallSplit3(oBox, m,refFrags));
                    }
                    meshes = splits;
                }
                
                foreach (Mesh m in meshes)
                {

                    CaveElement element = new CaveElement(m, ReferencePlane, orientation, parameters);
                    
                    caveElements.Add(element);
                }
            }
        }
        private void CheckPanelGeometry()
        {
            
            foreach(CaveElement caveElement in caveElements)
            {
                if (caveElement.supportAssembly == null)
                    continue;
                caveElement.supportAssembly.ConnectToEnvelope(caveElement.panelFrames);
                foreach (PanelFrame panelFrame in caveElement.panelFrames)
                {
                    
                    panelFrame.CheckGeometry();
                }
            }
        }
        public List<Mesh> SelectMeshes(string layerName, Plane plane)
        {
            RhinoObject[] objs = RhinoDoc.ActiveDoc.Objects.FindByLayer(layerName );
            if (objs == null)
                return new List<Mesh>();
            Plane pln1 = new Plane(plane.Origin - plane.YAxis * 100, plane.YAxis);
            Plane pln2 = new Plane(plane.Origin + plane.YAxis * (parameters.yCell+100), plane.YAxis * -1);
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
            prev.Origin = prev.Origin - ReferencePlane.YAxis * parameters.yCell*0.75;
            frags.AddRange(SelectMeshes("fragments", prev));

            Plane next = new Plane(ReferencePlane);
            next.Origin = prev.Origin + ReferencePlane.YAxis * 1.75 * parameters.yCell;
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
        
        private List<Mesh> WallSplit3(OrientedBox orientedBox, Mesh mesh, List<Mesh> meshes)
        {
            
            Plane orientationPlane = orientedBox.SideZmaxPlane;
            
            List<Mesh> parts = new List<Mesh>();
            foreach (Mesh refmesh in meshes)
            {
                OrientedBox refBox = CaveTools.FindRefOrientedBox(orientationPlane, refmesh);
                //OrientedBox.CheckPlane(refBox.SideXmaxPlane);
                Mesh panel = CaveTools.splitTwoPlanes(refBox.SideXmaxPlane, refBox.SideXminPlane, mesh);
                parts.Add(panel);
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
