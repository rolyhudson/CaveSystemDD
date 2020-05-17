using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class SupportAssembly
    {
        Parameters parameters;
        List<Brep> breps = new List<Brep>();
        public List <Line> hanger = new List<Line>();
        public List<Line> connection = new List<Line>();
        public List<Line> cornerStub = new List<Line>();
        Orientation orientation;
        Plane orientationPlane;

        public SupportAssembly(Parameters iparameters,Orientation iorientation,Plane plane)
        {
            parameters = iparameters;
            orientationPlane = plane;
            orientation = iorientation;

            breps = parameters.roofs;

            if (orientation == Orientation.SideNear || orientation == Orientation.SideFar)
                breps = parameters.walls;
            if (orientation == Orientation.Floor)
                breps = parameters.floors;
        }
        private void SimpleHanger(Point3d p1)
        {

            Point3d envelope = CaveTools.ClosestProjected(breps, p1, orientationPlane.Normal * -1);
            hanger.Add(new Line(p1, envelope));
        }
        private void SetHangerAssembly(Point3d p1, Point3d p2)
        {
            double bridgeZ = Math.Max(p1.Z, p2.Z) + parameters.FrameBridge;
            Point3d b1 = new Point3d(p1.X, p1.Y, bridgeZ);
            Point3d b2 = new Point3d(p2.X, p2.Y, bridgeZ);
            Line bridge = new Line(b1, b2);
            connection.Add(bridge);
            cornerStub.Add(new Line(b1, p1));
            cornerStub.Add(new Line(b2, p2));
            Point3d mid = bridge.PointAt(0.5);


            Point3d envelope = CaveTools.ClosestProjected(breps, mid, orientationPlane.Normal * -1);
            hanger.Add(new Line(mid, envelope));


        }
        public void ConnectToEnvelope(List<PanelFrame> panelFrames)
        {
            for (int i = 0; i < panelFrames.Count; i++)
            {
                if(!panelFrames[i].FailedFrame)
                {
                    if (i < panelFrames.Count - 1)
                    {
                        if (!panelFrames[i + 1].FailedFrame)
                        {
                            SetHangerAssembly(panelFrames[i].frameCorners[2], panelFrames[i + 1].frameCorners[0]);
                            SetHangerAssembly(panelFrames[i].frameCorners[3], panelFrames[i + 1].frameCorners[1]);
                        }
                        if(i > 0 )
                        {
                            if (panelFrames[i - 1].FailedFrame)
                            {
                                SimpleHanger(panelFrames[i].frameCorners[0]);
                                SimpleHanger(panelFrames[i].frameCorners[1]);
                            }
                        }
                    }
                    if (i == 0)
                    {
                        SimpleHanger(panelFrames[i].frameCorners[0]);
                        SimpleHanger(panelFrames[i].frameCorners[1]);
                    }
                    if (i == panelFrames.Count - 1)
                    {
                        SimpleHanger(panelFrames[i].frameCorners[2]);
                        SimpleHanger(panelFrames[i].frameCorners[3]);
                    }
                }
                
            }
            CaveTools.CheckLines(hanger);
            CaveTools.CheckLines(connection);
            CaveTools.CheckLines(cornerStub);
        }
    }
}
