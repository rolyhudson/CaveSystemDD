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
        public List <Line> hangerA = new List<Line>();
        public List<Line> hangerB = new List<Line>();
        public List<Line> connection = new List<Line>();
        public List<Line> brace = new List<Line>();
        Orientation orientation;
        Plane orientationPlane;

        public SupportAssembly(Parameters iparameters,Orientation iorientation,Plane plane)
        {
            parameters = iparameters;
            orientationPlane = plane;
            orientation = iorientation;

            FindSupportGeo();
            
        }
        private void FindSupportGeo()
        {
            string supportLayer = "column_cls";

            if (orientation == Orientation.Ceiling)
                supportLayer = "roof";
            RhinoObject[] objs = RhinoDoc.ActiveDoc.Objects.FindByLayer(supportLayer);
            foreach (RhinoObject obj in objs)
            {
                if (obj.ObjectType == ObjectType.Brep)
                    breps.Add(obj.Geometry as Brep);
                if (obj.ObjectType == ObjectType.Curve)
                    centreLines.Add(obj.Geometry as Curve);
            }
        }
        
        private void SimpleHanger(Point3d p1, ref List<Line> container)
        {

            Point3d envelope = FindConnectionPoint(p1);
            if (envelope.Z == 0)
                return;
            container.Add(new Line(p1, envelope));
        }
        private Line SetHangerAssembly(Point3d p1, Point3d p2,ref List<Line> container)
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
            if(envelope.Z > 10)
                container.Add(new Line(mid, envelope));

            return bridge;
        }
        private Point3d FindConnectionPoint(Point3d refPoint)
        {
            Point3d connection = new Point3d();
            if (orientation == Orientation.Ceiling)
                connection = CaveTools.ClosestProjected(breps, refPoint, orientationPlane.Normal * -1);
            else
            {
                connection = CaveTools.ClosestPoint(centreLines, refPoint, orientationPlane);
                //Plane testPlane = new Plane(connection, orientationPlane.Normal);
                //double dist1 = refPoint.DistanceTo(testPlane.ClosestPoint(refPoint));
                //connection = refPoint - orientationPlane.Normal * dist1;
            }
            return connection;
        }
        public void ConnectToEnvelope(List<PanelFrame> panelFrames)
        {
            for (int i = 0; i < panelFrames.Count; i++)
            {

                if (i < panelFrames.Count - 1)
                {
                    if (!panelFrames[i + 1].FailedFrame)
                    {
                        Line b1 = SetHangerAssembly(panelFrames[i].cornerStub[2].stubEnd, panelFrames[i + 1].cornerStub[0].stubEnd, ref hangerA);
                        UpdateStubs(panelFrames[i].cornerStub[2], panelFrames[i + 1].cornerStub[0], b1);

                        Line b2 = SetHangerAssembly(panelFrames[i].cornerStub[3].stubEnd, panelFrames[i + 1].cornerStub[1].stubEnd, ref hangerB);
                        UpdateStubs(panelFrames[i].cornerStub[3], panelFrames[i + 1].cornerStub[1], b2);
                    }
                    if (i > 0)
                    {
                        if (panelFrames[i - 1].FailedFrame)
                        {
                            SimpleHanger(panelFrames[i].cornerStub[0].stubEnd, ref hangerA);
                            SimpleHanger(panelFrames[i].cornerStub[1].stubEnd, ref hangerB);
                        }
                    }
                }
                if (i == 0)
                {
                    SimpleHanger(panelFrames[i].cornerStub[0].stubEnd, ref hangerA);
                    SimpleHanger(panelFrames[i].cornerStub[1].stubEnd, ref hangerB);
                }
                if (i == panelFrames.Count - 1)
                {
                    SimpleHanger(panelFrames[i].cornerStub[2].stubEnd, ref hangerA);
                    SimpleHanger(panelFrames[i].cornerStub[3].stubEnd, ref hangerB);
                }
                CaveTools.CheckLines(panelFrames[i].cornerStub.Select(x => x.Stub).ToList());
            }
            CaveTools.CheckLines(hangerA);
            CaveTools.CheckLines(hangerB);
            CaveTools.CheckLines(connection);
            if(orientation != Orientation.Ceiling)
            {
                HangerBrace(hangerA);
                HangerBrace(hangerB);
            }
            CaveTools.CheckLines(brace);
        }
        private void HangerBrace(List<Line> hanger)
        {
            List<Line> verticalsort = hanger.OrderBy(x => x.FromZ).ToList();
            //min 2 hangers for one panel
            for(int i = 1; i < verticalsort.Count; i++)
            {
                if(verticalsort[i].Length> 1000)
                {
                    brace.Add(new Line(verticalsort[i-1].To, verticalsort[i].From));
                }
            }
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
