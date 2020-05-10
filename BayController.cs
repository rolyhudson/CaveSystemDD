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
        public Brep bayBoundary;
        public Brep meshBox;
        public Plane minPlane;
        public Plane maxPlane;
        Plane referencePlane;
        Parameters parameters;

        public BayController (Mesh mesh,Brep bbox,Parameters iparameters)
        {
            slice = mesh;
            bayBoundary = bbox;
            parameters = iparameters;
            //sliceByFace(1, 2000);
            //sliceByFace(3, 2000);
            //midSection();
            Voxelise();
        }
        private void midSection()
        {
            Plane plane1 = getFaceOffsetPlane(1, 2000);
            plane1.Flip();
            Plane plane2 = getFaceOffsetPlane(3, 2000);
            plane2.Flip();
            RhinoDoc.ActiveDoc.Objects.AddMesh(CaveTools.splitTwoPlanes(plane1, plane2, slice));
        }
        private Mesh sliceByFace(int face,double trimOffset)
        {
            
            Plane plane = getFaceOffsetPlane(face, trimOffset);


            Mesh m = CaveTools.split(slice, plane);
            RhinoDoc.ActiveDoc.Objects.AddMesh(m);
            return m;

        }
        private Plane getFaceOffsetPlane(int face, double trimOffset)
        {
            BrepFace brepFace = bayBoundary.Faces[face];
            Point3d origin = brepFace.PointAt(brepFace.Domain(0).Mid, brepFace.Domain(1).Mid);
            Vector3d normal = brepFace.NormalAt(brepFace.Domain(0).Mid, brepFace.Domain(1).Mid);

            normal.Unitize();

            origin = origin + (normal * -trimOffset);

            Plane plane = new Plane(origin, normal);
            return plane;
        }
        private Plane BrepFacePlane(Brep brep,int face,bool pointInside)
        {
            BrepFace brepFace = brep.Faces[face];
            Point3d origin = brepFace.PointAt(brepFace.Domain(0).Mid, brepFace.Domain(1).Mid);
            Vector3d normal = brepFace.NormalAt(brepFace.Domain(0).Mid, brepFace.Domain(1).Mid);
            Plane plane = new Plane(origin, normal);
            if (pointInside) plane.Flip();
            return plane;
        }
        private void Voxelise()
        {
            double xdim = bayBoundary.Vertices[7].Location.DistanceTo(bayBoundary.Vertices[6].Location);
            double zdim = bayBoundary.Vertices[7].Location.DistanceTo(bayBoundary.Vertices[3].Location);
            int unitsX = (int)Math.Ceiling(xdim / 2000);
            int unitsZ = (int)Math.Ceiling(zdim / 2000);
            double dimX = xdim / unitsX;
            double dimZ = zdim / unitsZ;
            Vector3d xdir = bayBoundary.Vertices[6].Location - bayBoundary.Vertices[7].Location;
            Vector3d zdir = bayBoundary.Vertices[3].Location - bayBoundary.Vertices[7].Location;
            Vector3d ydir = bayBoundary.Vertices[4].Location - bayBoundary.Vertices[7].Location;
            xdir.Unitize();
            ydir.Unitize();
            zdir.Unitize();
            for (int x = 0; x < unitsX; x++)
            {
                for(int z = 0; z < unitsZ; z++)
                {
                    Vector3d shift = (xdir * dimX * (x+0.5)) + (ydir * 1500) + (zdir * dimZ * (z+0.5));
                    Point3d origin = bayBoundary.Vertices[7].Location + shift;
                    Plane plane = new Plane(origin, xdir, ydir);
                    Brep voxel = CaveTools.makeCuboid(plane, dimX,3000, dimZ);
                    RhinoDoc.ActiveDoc.Objects.AddBrep(voxel);
                    Mesh m1 = CaveTools.splitTwoPlanes(BrepFacePlane(voxel, 3,true), BrepFacePlane(voxel, 1, true), slice);
                    Mesh panel = CaveTools.splitTwoPlanes(BrepFacePlane(voxel, 5, true), BrepFacePlane(voxel, 4, true), m1);
                    ObjectAttributes oa = new ObjectAttributes();
                    oa.ObjectColor = CaveTools.getRandomColour();
                    oa.ColorSource = ObjectColorSource.ColorFromObject;
                    RhinoDoc.ActiveDoc.Objects.AddMesh(panel,oa);

                    Mesh[] parts = panel.SplitDisjointPieces();
                    List<BrepFace> supports = new List<BrepFace>()
                    {
                        bayBoundary.Faces[1],
                        bayBoundary.Faces[3],
                        bayBoundary.Faces[4],
                        bayBoundary.Faces[5]
                    };
                    foreach(Mesh p in parts)
                    {
                        Vector3d avNormal = CaveTools.averageVector(p);
                        avNormal.Reverse();
                        Point3d avPoint = CaveTools.averagePoint(p);
                        Line line = new Line(avPoint, avNormal * 100000000);
                        foreach(BrepFace brepface in supports)
                        {
                            Point3d[] points;
                            Curve[] curves;
                            double u = 0;
                            double v = 0;
                            Rhino.Geometry.Intersect.Intersection.CurveBrepFace(line.ToNurbsCurve(), brepface, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, out curves, out points);
                            if (points.Length > 0)
                            {
                                brepface.ClosestPoint(avPoint, out u, out v);
                                Line connector = new Line(avPoint, brepface.PointAt(u, v));
                               //RhinoDoc.ActiveDoc.Objects.AddLine(connector, oa);
                            }
                        }
                    }
                }
            }
        }
    }
}
