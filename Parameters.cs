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
        public double cellMin = 500;
        public double cellGap = 250;
        
        public List<Brep> roofs = new List<Brep>();
        public List<Brep> walls = new List<Brep>();
        public List<Brep> floors = new List<Brep>();
        public Parameters(double x, double y, double z, List<Brep> r, List<Brep> w, List<Brep> f)
        {
            xCell = x;
            yCell = y;
            zCell = z;
            roofs = r;
            walls = w;
            floors = f;
        }

    }
}
