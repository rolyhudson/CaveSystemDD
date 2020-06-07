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
    class SupportAssembly
    {
        Parameters parameters;
        List<Brep> breps = new List<Brep>();
        List<Curve> centreLines = new List<Curve>();
        public List <Line> hanger = new List<Line>();
        public List<Line> connection = new List<Line>();
        
        Orientation orientation;
        Plane orientationPlane;

        public SupportAssembly(Parameters iparameters,Orientation iorientation,Plane plane)
        {
            parameters = iparameters;
            orientationPlane = plane;
            orientation = iorientation;

            FindColumnBreps();
            FindBeamCentres();
        }
        private void FindColumnBreps()
        {
            RhinoObject[] objs = RhinoDoc.ActiveDoc.Objects.FindByLayer("COLUMNS");
            foreach (RhinoObject obj in objs)
            {
                if (obj.ObjectType == ObjectType.Brep)
                {
                    breps.Add(obj.Geometry as Brep);
                   
                }
            }
        }
        private void FindBeamCentres()
        {
            RhinoObject[] objs = RhinoDoc.ActiveDoc.Objects.FindByLayer("BEAMS");
            foreach (RhinoObject obj in objs)
            {
                if (obj.ObjectType == ObjectType.Curve)
                {
                    centreLines.Add(obj.Geometry as Curve);

                }
            }
        }
        private void SimpleHanger(Point3d p1)
        {

            Point3d envelope = FindConnectionPoint(p1);
            if (envelope.Z == 0)
                return;
            hanger.Add(new Line(p1, envelope));
        }
        private Line SetHangerAssembly(Point3d p1, Point3d p2)
        {
            //offset a test plane
            Plane testPlane = new Plane(orientationPlane.Origin + orientationPlane.Normal * -100000, orientationPlane.Normal);
            Point3d plnPt2 = testPlane.ClosestPoint(p2);
            Point3d plnPt1 = testPlane.ClosestPoint(p1);

            Point3d b1 = new Point3d();
            Point3d b2 = new Point3d();
            Vector3d back = new Vector3d();
            if (p1.DistanceTo(plnPt1) < p2.DistanceTo(testPlane.ClosestPoint(plnPt2)))
            {
                b1 = p1;
                back = b1 - plnPt1;
                b2 = plnPt2 + back;
            }
            else
            {
                b2 = p2;
                back = b2 - plnPt2;
                b1 = plnPt1 + back;
            }

            Line bridge = new Line(b1, b2);
            connection.Add(bridge);
            Point3d mid = bridge.PointAt(0.5);


            Point3d envelope = FindConnectionPoint(mid);
            hanger.Add(new Line(mid, envelope));

            return bridge;
        }
        private Point3d FindConnectionPoint(Point3d refPoint)
        {
            Point3d connection = new Point3d();
            if (orientation == Orientation.SideNear || orientation == Orientation.SideFar)
            {
                connection = CaveTools.ClosestProjected(breps, refPoint, orientationPlane.Normal * -1);
                
            }
            if(orientation == Orientation.Ceiling)
            {
                connection = CaveTools.ClosestPoint(centreLines, refPoint);
                Plane testPlane = new Plane(connection, orientationPlane.Normal);
                double dist1 = refPoint.DistanceTo(testPlane.ClosestPoint(refPoint));
                connection = refPoint - orientationPlane.Normal * dist1;
            }
            return connection;
        }
        public void ConnectToEnvelope(List<PanelFrame> panelFrames)
        {
            for (int i = 0; i < panelFrames.Count; i++)
            {
                if (panelFrames[i].cornerStub.Count < 4)
                    continue;
                if(!panelFrames[i].FailedFrame)
                {
                    if (i < panelFrames.Count - 1)
                    {
                        if (!panelFrames[i + 1].FailedFrame)
                        {
                            Line b1 = SetHangerAssembly(panelFrames[i].cornerStub[2].stubEnd, panelFrames[i + 1].cornerStub[0].stubEnd);
                            UpdateStubs(panelFrames[i].cornerStub[2], panelFrames[i + 1].cornerStub[0], b1);
                            Line b2 = SetHangerAssembly(panelFrames[i].cornerStub[3].stubEnd, panelFrames[i + 1].cornerStub[1].stubEnd);
                            UpdateStubs(panelFrames[i].cornerStub[3], panelFrames[i + 1].cornerStub[1], b2);
                        }
                        if(i > 0 )
                        {
                            if (panelFrames[i - 1].FailedFrame)
                            {
                                SimpleHanger(panelFrames[i].cornerStub[0].stubEnd);
                                SimpleHanger(panelFrames[i].cornerStub[1].stubEnd);
                            }
                        }
                    }
                    if (i == 0)
                    {
                        SimpleHanger(panelFrames[i].cornerStub[0].stubEnd);
                        SimpleHanger(panelFrames[i].cornerStub[1].stubEnd);
                    }
                    if (i == panelFrames.Count - 1)
                    {
                        SimpleHanger(panelFrames[i].cornerStub[2].stubEnd);
                        SimpleHanger(panelFrames[i].cornerStub[3].stubEnd);
                    }
                }
                CaveTools.CheckLines(panelFrames[i].cornerStub.Select(x => x.Stub).ToList());
            }
            CaveTools.CheckLines(hanger);
            CaveTools.CheckLines(connection);
            
        }
        private void UpdateStubs(StubMember stub1, StubMember stub2, Line bridge)
        {
            if(stub1.stubEnd.DistanceTo(bridge.ClosestPoint(stub1.stubEnd,false))> 0)
            {
                stub1.Update(bridge.ClosestPoint(stub1.stubEnd, false));
            }
            else
            {
                stub2.Update(bridge.ClosestPoint(stub2.stubEnd, false));
            }
        }
    }
}
