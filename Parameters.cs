using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class Parameters
    {
        public double xCell;
        public double yCell;
        public double zCell;
        public double cellMin = 750;
        public double cellGap = 250;
        public double FramePlaneMesh = 300;
        public double FrameBridge = 100;

        public List<Brep> roofs = new List<Brep>();
        public List<Brep> walls = new List<Brep>();
        public List<Brep> floors = new List<Brep>();
        public Parameters(double x, double y, double z)
        {
            xCell = x;
            yCell = y;
            zCell = z;
        }

    }
}
