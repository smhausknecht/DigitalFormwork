using DigitalFormwork.Components;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace DigitalFormwork
{
    public class Comp_BCutSolidSrf : GH_Component
    {
        public Comp_BCutSolidSrf()
          : base("Cut Solid Brep By Brep Surfaces",
                "CutBrepSrf",
                "Cuts solid Brep by Brep surfaces into solid parts.",
                "DigitalFormwork",
                "Modeling")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Solid", "SB", "Solid Brep to be cut into solid pieces.", GH_ParamAccess.item);
            pManager.AddBrepParameter("Cutting Surfaces", "CS", "List of Brep surfaces to cut solid with. Should be a single face Brep.", GH_ParamAccess.list);
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

            var cuttingBreps = new List<Brep>();
            if (!DA.GetDataList(1, cuttingBreps)) return;
            if (null == cuttingBreps || cuttingBreps.Count == 0) return;

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;

            // loop through cutting Breps
            var brepsToCut = new List<Brep> { inputBrep };
            foreach (var cutter in cuttingBreps)
            {
                if (null == cutter) continue;
                var splitParts = new List<Brep>();
                // loop through breps
                for (int bi = 0; bi < brepsToCut.Count; bi++)
                {
                    if (null == brepsToCut[bi] || !brepsToCut[bi].IsValid) continue;
                    var pieces = Brep.CreateBooleanSplit(brepsToCut[bi], cutter, tol);
                    splitParts.AddRange(pieces);
                }
                brepsToCut.Clear();
                brepsToCut.AddRange(splitParts);
                splitParts.Clear();
            }

            // assign output
            DA.SetDataList(0, brepsToCut);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_BCutSolidSrf;

        public override Guid ComponentGuid => new Guid("FB33DC2B-D227-4450-95D6-51A7786F5275");
    }
}

/// DigitalFormwork
/// SPDX-License-Identifier: GPL-3.0
/// Copyright(c) 2025 Simon M. Hausknecht