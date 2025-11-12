using DigitalFormwork.Components;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace DigitalFormwork
{
    public class Comp_MShellPln : GH_Component
    {
        public Comp_MShellPln()
          : base("Create Mesh Shell With Plane",
                "MeshShellPln",
                "Cuts two Mesh layers with planes into solid shells.",
                "DigitalFormwork",
                "Modeling")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Inner Shell", "IS", "Inner Shell.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Outer Shell", "OS", "Outer Shell.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Cutting Plane", "CS", "List of Brep surfaces to cut solid with.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Shells", "S", "Formwork shells.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // input checks
            Mesh innerShell = null;
            if (!DA.GetData(0, ref innerShell)) return;
            if (!Utils.CheckMeshSolid(innerShell, this)) return;

            Mesh outerShell = null;
            if (!DA.GetData(1, ref outerShell)) return;
            if (!Utils.CheckMeshSolid(outerShell, this)) return;

            var cuttingPlane = Plane.Unset;
            if (!DA.GetData(2, ref cuttingPlane)) return;

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;

            // cut
            var cutShell = Utils.CutMeshByPlane(outerShell, cuttingPlane, true, this);
            var cutout = new List<Mesh>() { innerShell };
            var shells = new List<Mesh>();
            foreach (var part in cutShell)
            {
                var piece = new List<Mesh> { part };
                var pieces = Mesh.CreateBooleanDifference(piece, cutout) ?? Array.Empty<Mesh>();
                shells.AddRange(pieces);
            }

            // assign output
            DA.SetDataList(0, shells);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_MShellPln;

        public override Guid ComponentGuid => new Guid("1CE7F8A8-F5FE-4DC5-BADB-B5B83F4A16C4");
    }
}

/// DigitalFormwork
/// SPDX-License-Identifier: GPL-3.0
/// Copyright(c) 2025 Simon M. Hausknecht