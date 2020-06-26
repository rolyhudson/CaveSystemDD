using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class ClashFix
    {
        List<CaveElement> caveElements = new List<CaveElement>();
        public ClashFix(List<CaveElement> elements)
        {
            caveElements = elements;
            PanelClash();
        }

        private void PanelClash()
        {
            //no hang no clash
            List<PanelFrame> toTest = new List<PanelFrame>();
            CaveElement ceiling = caveElements.Find(x => x.orientation == Orientation.Ceiling);
            if (ceiling == null || caveElements.Count() == 1)
                return;

            CaveElement north = caveElements.Find(x => x.orientation == Orientation.SideNorth);
            CaveElement south = caveElements.Find(x => x.orientation == Orientation.SideSouth);
            List<CaveElement> east = caveElements.FindAll(x => x.orientation == Orientation.SideEast);
            List<CaveElement> west = caveElements.FindAll(x => x.orientation == Orientation.SideWest);
            if (north != null)
            {
                CheckFixClash(north.panelFrames.First(), ceiling.panelFrames.Last(),true);
                if (north.panelFrames.Count > 1)
                    CheckFixClash(north.panelFrames[1], ceiling.panelFrames.Last(), true);
            }
            if (south != null)
            {
                CheckFixClash(south.panelFrames.Last(), ceiling.panelFrames.First(), true);
                if (south.panelFrames.Count > 1)
                    CheckFixClash(south.panelFrames[south.panelFrames.Count - 2], ceiling.panelFrames.First(), true);
            }
            if (east != null)
            {
                foreach (CaveElement eastEles in east)
                {
                    Brep brep = eastEles.panelFrames.Last().subFrameBoundary();
                    foreach (PanelFrame panelCeiling in ceiling.panelFrames)
                    {
                        CheckFixClash(eastEles.panelFrames.Last(), panelCeiling);
                    }
                }

            }
            if (west != null)
            {
                foreach (CaveElement westEles in west)
                {
                    Brep brep = westEles.panelFrames.First().subFrameBoundary();
                    RhinoDoc.ActiveDoc.Objects.AddBrep(brep);
                    foreach (PanelFrame panelCeiling in ceiling.panelFrames)
                    {
                        CheckFixClash(westEles.panelFrames.First(), panelCeiling);
                    }
                }
            }
        }
        private void CheckFixClash(PanelFrame wall, PanelFrame roof, bool adjustroof = false)
        {
            Brep roofBrep = roof.subFrameBoundary();
            Brep wallBrep = wall.subFrameBoundary();
            Curve[] curves = null;
            Point3d[] points = null;
            if (Rhino.Geometry.Intersect.Intersection.BrepBrep(roofBrep, wallBrep, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, out curves, out points))
            {
                if (curves.Length > 0)
                {
                    wall.TrySnapGhosts();
                    if(adjustroof) roof.TrySnapGhosts();
                }
            }
        }
    }
}
