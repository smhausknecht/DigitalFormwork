using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;

namespace DigitalFormwork.Components.Bugfix
{
    public class Comp_BRebuildBrep : GH_Component
    {
        public Comp_BRebuildBrep()
          : base("Rebuild Brep",
                "RebBrep",
                "Use in case of errors. Rebuilds Brep.",
                "DigitalFormwork",
                "Bugfix")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Faulty Brep", "B", "The faulty Brep to rebuild.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Rebuilt Brep.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // assign input
            Brep inputGeometry = null;
            if (!DA.GetData(0, ref inputGeometry)) return;

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;

            // rebuild Brep
            var rebuilt = Utils.RebuildBrep(inputGeometry, tol);
            inputGeometry.Repair(tol);
            inputGeometry.MergeCoplanarFaces(tol);
            inputGeometry.Compact();

            // assign output
            DA.SetData(0, rebuilt);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_BRebuildBrep;

        public override Guid ComponentGuid => new Guid("D69A5123-03F1-42D1-83C6-AF6F65FA33FB");
    }
}