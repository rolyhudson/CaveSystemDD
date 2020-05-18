using Rhino.Display;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class Documenter
    {
        Dictionary<string, ObjectAttributes> layerAttributes = new Dictionary<string, ObjectAttributes>();
        Random randomGen = new Random();
        File3dm file;

        List<string> layerNames = new List<string>()
        {
            "HANGER",
            "WALL CORNER STUB",
            "WALL INTERNAL STUB",
            "WALL SUBFRAME",
            "WALL CANTILEVER BEAM",
            "HANG CORNER STUB",
            "HANG INTERNAL STUB",
            "HANG SUBFRAME",
            "DIAGONAL BRACING",
            "GSA MESH",
            "INTERNAL COLUMN",
            "PERIMETER COLUMN",
            "ROOF BEAM",
            "CONNECTION",
            "CAVE PANELS",
        };
        List<Color> layerColors = new List<Color>()
        {
            Color.FromArgb( 229,34,145),
            Color.FromArgb( 255,0,218),
            Color.Black,
            Color.FromArgb( 183,61,203),
            Color.FromArgb( 191,63,63),
            Color.FromArgb( 72,0,255),
            Color.Black,
            Color.FromArgb( 61,61,203),
            Color.FromArgb( 34,90,229),
            Color.FromArgb( 0,145,255),
            Color.FromArgb( 34,229,201),
            Color.Fuchsia,
            Color.Black,
            Color.FromArgb( 0,127,0),
            Color.HotPink
        };
        public Documenter()
        {
            file = new File3dm();
            SetUpLayers();
            
        }
        private void SetUpLayers()
        {
            for(int i = 0; i < layerNames.Count; i++)
            {
                Layer layer = new Layer();
                layer.Name = layerNames[i];
                layer.Color = layerColors[i];
                file.AllLayers.Add(layer);

                ObjectAttributes oa = new ObjectAttributes();
                oa.LayerIndex = i;

                oa.ObjectColor = layerColors[i];
                oa.Name = layerNames[i];

                if(layerNames[i] == "CAVE PANELS")
                    oa.ColorSource = ObjectColorSource.ColorFromObject;
                layerAttributes.Add(layerNames[i], oa);
            }
        }
        public void WritePart3d(PartController partController, Parameters parameters, string filePath)
        {
            foreach(BayController bayController in partController.bayControllers)
            {
                foreach(CaveElement caveElement in bayController.caveElements)
                {
                    AddSupportAssembly(caveElement.supportAssembly,caveElement.orientation);
                    AddPanelFrames(caveElement.panelFrames, caveElement.orientation);
                }
            }
            RhinoViewport viewport = new RhinoViewport();
            viewport.SetProjection(DefinedViewportProjection.Perspective, "cavern view", false);
            viewport.ZoomExtents();
            file.AllViews.Add(new ViewInfo(viewport));
            file.Write(filePath, 5);
        }
        private void AddSupportAssembly(SupportAssembly supportAssembly,Orientation orientation)
        {
            if (orientation == Orientation.Ceiling)
            {
                AddLines(supportAssembly.hanger, "HANGER");
                AddLines(supportAssembly.connection, "CONNECTION");
                AddLines(supportAssembly.cornerStub, "HANG CORNER STUB");
            }
            if (orientation == Orientation.SideFar || orientation == Orientation.SideFar)
            {
                AddLines(supportAssembly.hanger, "WALL CANTILEVER BEAM");
                AddLines(supportAssembly.connection, "CONNECTION");
                AddLines(supportAssembly.cornerStub, "WALL CORNER STUB");
            }
            }
        private void AddPanelFrames(List<PanelFrame> panelFrames, Orientation orientation)
        {
            foreach(PanelFrame panelFrame in panelFrames)
            {
                if (!panelFrame.FailedFrame)
                {
                    AddMesh(panelFrame.GSAmesh, "GSA MESH");
                    AddMesh(panelFrame.CavePanels, "CAVE PANELS");
                    if(orientation == Orientation.Ceiling)
                    {
                        AddLines(panelFrame.internalStub, "HANG INTERNAL STUB");
                        AddLines(panelFrame.subFrame, "HANG SUBFRAME");
                        AddLines(panelFrame.cornerStub, "HANG CORNER STUB");

                    }
                    if (orientation == Orientation.SideFar || orientation == Orientation.SideFar)
                    {
                        AddLines(panelFrame.internalStub, "WALL INTERNAL STUB");
                        AddLines(panelFrame.subFrame, "WALL SUBFRAME");
                        AddLines(panelFrame.cornerStub, "WALL CORNER STUB");

                    }
                }
               
            }
        }
        private void AddMesh(Mesh mesh, string layer)
        {
            ObjectAttributes oa = layerAttributes[layer];
            if (layer == "CAVE PANELS")
                oa.ObjectColor = CaveTools.getRandomColour();
            file.Objects.AddMesh(mesh, oa);
        }
        private void AddLines(List<Line> lines, string layer)
        {
            foreach(Line l in lines)
            {
                file.Objects.AddLine(l, layerAttributes[layer]);
            }
            
        }
        private void LookUpAttributes(string prop, Orientation orientation)
        {
            //lookup by prop name and orientation
            //return ObjectAttributes
        }
    }
}
