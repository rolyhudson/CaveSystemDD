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
            CaveElement ceiling = new CaveElement(SelectMesh("CeilingMesh"), ReferencePlane, Orientation.Ceiling, parameters);
            caveElements.Add(ceiling);
            CaveElement farside = new CaveElement(SelectMesh("SideMesh"), ReferencePlane, Orientation.SideFar, parameters);
            caveElements.Add(farside);
            //CaveElement nearside = new CaveElement(NearSide(), bayBoundary.ReferencePlane, Orientation.SideNear, parameters);
            //caveElements.Add(nearside);
            //MeshToPanels(NearSide(), Orientation.SideNear);

        }
        private Mesh SelectMesh(string layerName)
        {
            RhinoObject[] objs = RhinoDoc.ActiveDoc.Objects.FindByLayer(layerName );
            if (objs == null)
                return null;
            Plane bayXZ = new Plane(ReferencePlane.Origin, ReferencePlane.YAxis);

            foreach(RhinoObject obj in objs)
            {
                if(obj.ObjectType == ObjectType.Mesh)
                {
                    Point3d centroid = CaveTools.averagePoint(obj.Geometry as Mesh);
                    double dist = centroid.DistanceTo(bayXZ.ClosestPoint(centroid));
                    if (dist <= parameters.yCell && CaveTools.pointInsidePlane(centroid,bayXZ))
                        return obj.Geometry as Mesh;
                }
            }
            return null;
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
