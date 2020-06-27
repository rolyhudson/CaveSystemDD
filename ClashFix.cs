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
            
            List<CaveElement> ceilings = caveElements.FindAll(x => x.orientation == Orientation.Ceiling);


            List<CaveElement> north = caveElements.FindAll(x => x.orientation == Orientation.SideNorth);
            List<CaveElement> south = caveElements.FindAll(x => x.orientation == Orientation.SideSouth);
            List<CaveElement> east = caveElements.FindAll(x => x.orientation == Orientation.SideEast);
            List<CaveElement> west = caveElements.FindAll(x => x.orientation == Orientation.SideWest);
            
            
            foreach(CaveElement ceiling in ceilings)
            {
                foreach (CaveElement northEles in north)
                    CheckFixClash(northEles.panelFrames.First(), ceiling.panelFrames.Last(), true, Orientation.SideNorth);
                foreach (CaveElement southEles in south)
                    CheckFixClash(southEles.panelFrames.Last(), ceiling.panelFrames.First(), true, Orientation.SideSouth);

                foreach (CaveElement eastEles in east)
                {
                    foreach (PanelFrame panelCeiling in ceiling.panelFrames)
                        CheckFixClash(eastEles.panelFrames.Last(), panelCeiling, false, Orientation.SideEast);
                    
                }
                foreach (CaveElement westEles in west)
                {
                    foreach (PanelFrame panelCeiling in ceiling.panelFrames)
                        CheckFixClash(westEles.panelFrames.First(), panelCeiling, false, Orientation.SideWest);
                }
            }
        }
        private void CheckFixClash(PanelFrame wall, PanelFrame roof, bool adjustroof, Orientation wallOrientation)
        {
            
            Brep roofBrep = roof.subFrameBoundary();
            Brep wallBrep = wall.subFrameBoundary();
            Curve[] curves = null;
            Point3d[] points = null;
            int wallRow = 0;
            int ceilingRow = 0;
            if (wallOrientation == Orientation.SideEast)
                wallRow = wall.frameGrid.Count-1;
            if (wallOrientation == Orientation.SideNorth)
                ceilingRow = roof.frameGrid.Count-1;
            if (wallOrientation == Orientation.SideSouth)
                wallRow = wall.frameGrid.Count - 1;
            if (Rhino.Geometry.Intersect.Intersection.BrepBrep(roofBrep, wallBrep, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, out curves, out points))
            {
                if (curves.Length > 0)
                {
                    TrySnapGhosts(wall,wallRow);
                    if(adjustroof) TrySnapGhosts(roof,ceilingRow);
                }
            }
        }
        public void TrySnapGhosts(PanelFrame panelFrame, int rowToAdjust)
        {
            //only apply to edge near clash
            if (panelFrame.clashFixAttempted) return;
            panelFrame.clashFixAttempted = true;
            for (int i = 0; i < panelFrame.meshnodes.Count; i++)
            {
                //skip middle row
                if (panelFrame.meshnodes.Count == 3 && i == 1)
                    continue;
                if (i != rowToAdjust)
                    continue;
                for (int j = 0; j < panelFrame.meshnodes[i].Count; j++)
                {
                    if (panelFrame.meshnodes[i][j].isGhost)
                    {
                        bool replaceSupport = false;
                        Point3d closestExtra = CaveTools.ClosestPoint(panelFrame.frameGrid[i][j], panelFrame.meshExtraSupport.Stubs.Select(x => x.From).ToList());
                        Line frameline = new Line();
                        Vector3d test = panelFrame.frameGrid[i][j] - closestExtra;

                        foreach (Line line in panelFrame.frameLinesX)
                        {
                            if (panelFrame.frameGrid[i][j].DistanceTo(line.ClosestPoint(panelFrame.frameGrid[i][j], true)) < 5)
                                frameline = line;
                            // angle should be small a and dist on line
                            double angle = Vector3d.VectorAngle(test, line.Direction);
                            if (Math.Abs(angle - Math.PI) < 0.05 || angle < 0.05)
                            {
                                //the extra stub is on the same frame line
                                Point3d ptOnLn = line.ClosestPoint(closestExtra, true);

                                if (panelFrame.frameGrid[i][j].DistanceTo(ptOnLn) < line.Length && closestExtra.DistanceTo(ptOnLn) < 5)
                                {
                                    replaceSupport = true;
                                    break;
                                }
                            }

                        }
                        if (replaceSupport)
                            FrameGridToExtraSupportPos(panelFrame, frameline,i,j, closestExtra);
                        else
                            FrameGridToLineCentre(panelFrame, frameline, i, j);
                    }
                }
            }
            //now re set frame
            panelFrame.frameLinesX = new List<Line>();
            panelFrame.frameLinesY = new List<Line>();
            panelFrame.SetFrameLines(ref panelFrame.frameGrid, ref panelFrame.frameLinesX, ref panelFrame.frameLinesY);
        }
        private void FrameGridToExtraSupportPos(PanelFrame panelFrame,Line frameline,int i, int j, Point3d closestExtra)
        {
            
            Point3d ptOnLn = frameline.ClosestPoint(closestExtra, true);
            Point3d meshpt = CaveTools.ClosestPoint(panelFrame.frameGrid[i][j], panelFrame.meshExtraSupport.Stubs.Select(x => x.To).ToList());
            UpdateStubs(panelFrame, panelFrame.frameGrid[i][j], meshpt, ptOnLn);
            //remove extra support
            RemoveExtraSupport(panelFrame, closestExtra);
            panelFrame.frameGrid[i][j] = ptOnLn;
        }
        private void FrameGridToLineCentre(PanelFrame panelFrame, Line frameline, int i, int j)
        {
            Point3d ptOnLn = frameline.PointAt(0.5);
            UpdateStubs(panelFrame, panelFrame.frameGrid[i][j], ptOnLn, ptOnLn);
            panelFrame.frameGrid[i][j] = ptOnLn;
        }
        private void RemoveExtraSupport(PanelFrame panelFrame, Point3d pt)
        {
            Line toRemove = new Line();
            foreach (Line line in panelFrame.meshExtraSupport.Stubs)
            {
                if (pt.DistanceTo(line.ClosestPoint(pt, true)) < 5)
                {
                    toRemove = line;

                }
            }
            panelFrame.meshExtraSupport.Stubs.Remove(toRemove);
        }
        private void UpdateStubs(PanelFrame panelFrame, Point3d oldframePt, Point3d meshpt, Point3d newframPt)
        {
            foreach (StubMember stubMember in panelFrame.cornerStub)
            {
                if (oldframePt.DistanceTo(stubMember.Stub.ClosestPoint(oldframePt, true)) < 5)
                {
                    stubMember.Update(meshpt, newframPt);
                }
            }
            foreach (StubMember stubMember in panelFrame.internalStub)
            {
                if (oldframePt.DistanceTo(stubMember.Stub.ClosestPoint(oldframePt, true)) < 5)
                {
                    stubMember.Update(meshpt, newframPt);
                }
            }
        }
    }
}
