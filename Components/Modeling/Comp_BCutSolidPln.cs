using DigitalFormwork.Components;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace DigitalFormwork
{
    public class Comp_BCutSolidPln : GH_Component
    {
        public Comp_BCutSolidPln()
          : base("Cut Solid Brep By Planes",
                "CutBrepPln",
                "Cuts solid Brep by planes into solid parts.",
                "DigitalFormwork",
                "Modeling")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Solid", "SB", "Solid Brep to be cut into solid pieces.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Cutting Planes", "CP", "List of planes to cut solid with.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Parts", "P", "Cut parts.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // assign input
            Brep inputBrep = null;
            if (!DA.GetData(0, ref inputBrep)) return;
            if (!Utils.CheckBrep(inputBrep, this)) return;

            var cuttingPlanes = new List<Plane>();
            if (!DA.GetDataList(1, cuttingPlanes)) return;
            if (null == cuttingPlanes || cuttingPlanes.Count == 0) return;

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;

            // loop through cutting planes
            var brepsToCut = new List<Brep> { inputBrep };
            foreach (var plane in cuttingPlanes)
            {
                var splitParts = new List<Brep>();
                // loop through breps
                for (int bi = 0; bi < brepsToCut.Count; bi++)
                {
                    if(null == brepsToCut[bi] || !brepsToCut[bi].IsValid) continue;
                    var pieces = Utils.CutBrepByPlane(brepsToCut[bi], plane, tol, this) ?? Array.Empty<Brep>();
                    splitParts.AddRange(pieces);
                }
                brepsToCut.Clear();
                brepsToCut.AddRange(splitParts);
                splitParts.Clear();
            }

            // assign output
            DA.SetDataList(0, brepsToCut);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_BCutSolidPln;

        public override Guid ComponentGuid => new Guid("A7B4D157-E0F0-466C-A10B-FD2811F1CE2D");
    }
}

/// DigitalFormwork
/// SPDX-License-Identifier: GPL-3.0
/// Copyright(c) 2025 Simon M. Hausknecht