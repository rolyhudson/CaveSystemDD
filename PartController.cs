﻿using Rhino;
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
            SetBays();
        }
        private void SetBays()
        {
            for(int i=0;i< grid.Count-1;i++)
            {
                gridPlane = new Plane(grid[i].To, grid[i].From, grid[i + 1].To);
                //OrientedBox.CheckPlane(gridPlane);
                Slice();
            }

        }
        private void Slice()
        {
            Vector3d shiftY = gridPlane.YAxis * parameters.yCell;
            Point3d basePt = new Point3d(gridPlane.OriginX, gridPlane.OriginY, gridPlane.OriginZ);
            Point3d origin = basePt + shiftY;

            Plane p1 = new Plane(basePt, gridPlane.YAxis);
            Plane p2 = new Plane(basePt + gridPlane.YAxis * parameters.yCell, gridPlane.YAxis * -1);

            //try and slice the mesh
            Mesh slice = CaveTools.splitTwoPlanes(p1, p2, meshToVoxelise);
            bool normals = slice.FaceNormals.ComputeFaceNormals();
            Plane boxPln = new Plane(basePt, gridPlane.XAxis, gridPlane.YAxis);
            if (slice != null && slice.Faces.Count > 0)
            {
                Brep minVol = CaveTools.findBBoxGivenPlane(boxPln, slice);
                OrientedBox oBox = CaveTools.FindOrientedBox(boxPln, slice);
                brepBBoxes.Add(oBox.BoundingBox);
                caveSlices.Add(slice);
                bayControllers.Add(new BayController(slice,oBox,parameters));
            }
        }
        
    }
}
