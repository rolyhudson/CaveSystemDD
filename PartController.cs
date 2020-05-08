using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class PartController
    {
        Mesh meshToVoxelise;
        public List<BayController> bayControllers = new List<BayController>();
        public List<Line> grid = new List<Line>();
        public List<Mesh> caveSlices = new List<Mesh>();
        public List<Brep> brepBBoxes = new List<Brep>();
        public Parameters parameters;
        //public Text3d sectionNum;
        Plane gridPlane;

        public PartController(Mesh mesh, List<Line> gridLines, Parameters prms)
        {
            parameters = prms;
            meshToVoxelise = mesh;
            meshToVoxelise.Normals.ComputeNormals();
            meshToVoxelise.FaceNormals.ComputeFaceNormals();
            grid = gridLines;
            
            //setText(parameters.partNumber);
            Slice();
        }
        private void SetBays()
        {
            for(int i=0;i< grid.Count-1;i++)
            {
                gridPlane = new Plane(grid[i].From, grid[i].To, grid[i + 1].From);

                Slice();
            }

        }
        private void Slice()
        {
            Vector3d shiftY = gridPlane.YAxis * parameters.yCell;
            Point3d basePt = new Point3d(gridPlane.OriginX, gridPlane.OriginY, gridPlane.OriginZ);
            Point3d origin = basePt + shiftY;

            Plane p1 = new Plane(origin, gridPlane.YAxis);
            Plane p2 = new Plane(origin + gridPlane.YAxis * parameters.yCell, gridPlane.YAxis * -1);

            //try and slice the mesh
            Mesh slice = CaveTools.splitTwoPlanes(p1, p2, meshToVoxelise);
            bool normals = slice.FaceNormals.ComputeFaceNormals();
            Plane boxPln = new Plane(origin, gridPlane.XAxis, gridPlane.YAxis);
            if (slice != null && slice.Faces.Count > 0)
            {

                BoundingBox boundingBox = new BoundingBox();
                Brep minVol = findBBoxGivenPlane(boxPln, slice, ref boundingBox);
                brepBBoxes.Add(minVol);
                caveSlices.Add(slice);
                bayControllers.Add(new BayController());

            }
            //addGrid(boxPln, y);
        }
        private Brep findBBoxGivenPlane(Plane pln, Mesh m, ref BoundingBox bBox)
        {
            List<Point3d> pts = new List<Point3d>();

            foreach (Point3d p in m.Vertices)
            {
                Point3d remapped = new Point3d();
                pln.RemapToPlaneSpace(p, out remapped);

                pts.Add(remapped);
            }

            bBox = new BoundingBox(pts);
            Brep brep = bBox.ToBrep();
            Transform xform = Transform.PlaneToPlane(Plane.WorldXY, pln);
            brep.Transform(xform);
            return brep;
        }
    }
}
