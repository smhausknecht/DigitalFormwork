using DigitalFormwork.Components;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace DigitalFormwork
{
    public class Comp_BShellSrf : GH_Component
    {
        public Comp_BShellSrf()
          : base("Create Brep Shell With Brep Surface",
                "BrepShellSrf",
                "Cuts two Brep layers by a surface into solid shells.",
                "DigitalFormwork",
                "Modeling")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Inner Shell", "IS", "Inner Shell.", GH_ParamAccess.item);
            pManager.AddBrepParameter("Outer Shell", "OS", "Outer Shell.", GH_ParamAccess.item);
            pManager.AddBrepParameter("Cutting Surface", "CS", "Brep surface to cut solid with. Should be a single face Brep.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Shells", "S", "Formwork shells.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // assign input
            Brep innerShell = null;
            if (!DA.GetData(0, ref innerShell)) return;
            if (!Utils.CheckBrep(innerShell)) return;

            Brep outerShell = null;
            if (!DA.GetData(1, ref outerShell)) return;
            if (!Utils.CheckBrep(outerShell)) return;

            Brep cuttingBrep = null;
            if (!DA.GetData(2, ref cuttingBrep)) return;

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;

            // cut
            var cutShell = Brep.CreateBooleanSplit(outerShell, cuttingBrep, tol);
            var shells = new List<Brep>();
            foreach (var part in cutShell)
            {
                var pieces = Brep.CreateBooleanDifference(part, innerShell, tol) ?? Array.Empty<Brep>();
                shells.AddRange(pieces);
            }

            // assign output
            DA.SetDataList(0, shells);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_BShellSrf;

        public override Guid ComponentGuid => new Guid("C908F7BF-E47E-4145-B357-F6072E24EFE2");
    }
}