using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using System;

namespace DigitalFormwork.Components.Bugfix
{
    public class Comp_BPrepBrep : GH_Component
    {
        public Comp_BPrepBrep()
          : base("Prepare Brep",
                "PrepBrep",
                "Use in case of errors. Converts Mesh or SubD to Brep reliably. Simplifies Brep. Shows potential problems with Brep.",
                "DigitalFormwork",
                "Bugfix")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Body", "B", "The geometry to prepare as Brep for other components.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Limit", "L", "Safety limit. Prevents conversion from Meshes to Brep, if the input has more faces than the limit in order to prevent crashes. Default = 1000", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Clean Brep.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // assign input
            IGH_GeometricGoo inputGeometry = null;
            if (!DA.GetData(0, ref inputGeometry)) return;

            var limit = 1000;
            DA.GetData(1, ref limit);

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;

            // check and convert
            var castBody = Utils.PrepBrep(inputGeometry, limit, tol);

            // check result
            if (!Utils.CheckBrep(castBody, this)) return;

            // assign output
            DA.SetData(0, castBody);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_BPrepBrep;

        public override Guid ComponentGuid => new Guid("5B3B4BBE-35F1-48FB-B004-30A74C943507");
    }
}

/// DigitalFormwork
/// SPDX-License-Identifier: GPL-3.0
/// Copyright(c) 2025 Simon M. Hausknecht