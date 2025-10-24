using DigitalFormwork.Components;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace DigitalFormwork
{
    public class Comp_BShellPln : GH_Component
    {
        public Comp_BShellPln()
          : base("Create Brep Shell with Plane",
                "BrepShellPln",
                "Cuts two Brep layers with planes into solid shells.",
                "DigitalFormwork",
                "Modeling")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Inner Shell", "IS", "Inner Shell.", GH_ParamAccess.item);
            pManager.AddBrepParameter("Outer Shell", "OS", "Outer Shell.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Cutting Plane", "CS", "List of Brep surfaces to cut solid with.", GH_ParamAccess.item);
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

            var cuttingPlane = Plane.Unset;
            if (!DA.GetData(2, ref cuttingPlane)) return;

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;

            // cut
            var cutShell = Utils.CutBrepByPlane(outerShell, cuttingPlane, tol, this);
            var shells = new List<Brep>();
            foreach (var part in cutShell)
            {
                var pieces = Brep.CreateBooleanDifference(part, innerShell, tol) ?? Array.Empty<Brep>();
                shells.AddRange(pieces);
            }

            // assign output
            DA.SetDataList(0, shells);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_BShellPln;

        public override Guid ComponentGuid => new Guid("FA8B9781-2E75-4B63-AC9C-61DE86D7874C");
    }
}

/// DigitalFormwork
/// SPDX-License-Identifier: GPL-3.0
/// Copyright(c) 2025 Simon M. Hausknecht