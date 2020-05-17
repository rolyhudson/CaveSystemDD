using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace CaveSystem2020
{
    public class CaveSystem2020Component : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CaveSystem2020Component()
          : base("CaveSystem2020", "CS2020",
              "Description",
              "CS2020", "CS2020")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "M", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("xCell", "x", "", GH_ParamAccess.item, 2000);
            pManager.AddNumberParameter("yCell", "y", "", GH_ParamAccess.item, 3000);
            pManager.AddNumberParameter("zCell", "z", "", GH_ParamAccess.item, 1000);
            pManager.AddLineParameter("grid", "g", "", GH_ParamAccess.list);
            pManager.AddBrepParameter("roof", "r", "", GH_ParamAccess.list);
            pManager.AddBrepParameter("walls", "w", "", GH_ParamAccess.list);
            pManager.AddBrepParameter("floors", "f", "", GH_ParamAccess.list);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("cave slices", "cs", "", GH_ParamAccess.tree);
            pManager.AddBrepParameter("bbox", "bb", "", GH_ParamAccess.tree);

            pManager.AddGenericParameter("trim cells", "tc", "", GH_ParamAccess.tree);
            pManager.AddGenericParameter("perimeter cells", "pc", "", GH_ParamAccess.tree);
            pManager.AddGenericParameter("undefined cells", "udc", "", GH_ParamAccess.tree);


            
            pManager.AddMeshParameter("section boxes", "sb", "", GH_ParamAccess.tree);
            pManager.AddCurveParameter("grid", "g", "", GH_ParamAccess.tree);
            pManager.AddCurveParameter("links", "l", "", GH_ParamAccess.tree);

            pManager.AddBrepParameter("boundaries", "b", "", GH_ParamAccess.tree);
            
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = new Mesh();
            List<Line> bldGrid = new List<Line>();
            double xcell = 0;
            double ycell = 0;
            double zcell = 0;
            List<Brep> roofs = new List<Brep>();
        List<Brep> walls = new List<Brep>();
        List<Brep> floors = new List<Brep>();

            if (!DA.GetData(0, ref mesh)) return;
            if (!DA.GetData(1, ref xcell)) return;
            if (!DA.GetData(2, ref ycell)) return;
            if (!DA.GetData(3, ref zcell)) return;
            if (!DA.GetDataList(4, bldGrid)) return;
            if (!DA.GetDataList(5, roofs)) return;
            if (!DA.GetDataList(6, walls)) return;
            if (!DA.GetDataList(7, floors)) return;

            Parameters parameters = new Parameters(xcell, ycell, zcell,roofs,walls, floors);
            PartController pControl = new PartController(mesh, bldGrid, parameters);

            GH_Structure<GH_Mesh> caveSlices = new GH_Structure<GH_Mesh>();
            GH_Structure<GH_Brep> bboxes = new GH_Structure<GH_Brep>();

            getSlices(pControl, ref bboxes, ref caveSlices);

            DA.SetDataTree(0, caveSlices);
            DA.SetDataTree(1, bboxes);

            Documenter documenter3d = new Documenter();
            documenter3d.WritePart3d(pControl,parameters, @"C:\Users\Admin\Documents\projects\PassageProjects\DD\Output\part.3dm");
        }
        private void getSlices(PartController pcontrol, ref GH_Structure<GH_Brep> boxes, ref GH_Structure<GH_Mesh> slices)
        {
            
            foreach (Brep b in pcontrol.brepBBoxes) boxes.Append(new GH_Brep(b));
            foreach (Mesh m in pcontrol.caveSlices) slices.Append(new GH_Mesh(m));
            
        }
        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8ed7bf2e-6d75-42a0-a8bd-c5998064fdec"); }
        }
    }
}
