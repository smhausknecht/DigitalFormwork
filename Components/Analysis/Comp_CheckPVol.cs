using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;

namespace DigitalFormwork.Components.Analysis
{
    public class Comp_CheckPVol : GH_Component
    {
        public Comp_CheckPVol()
          : base("Check Print Volume",
                "CheckPrintVol",
                "Checks whether a given geometry can be printed within certain dimensions by finding the bounding box with the smallest volume possible within the given dimensions.",
                "DigitalFormwork",
                "Analysis")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Part", "P", "Part to check dimensions for.", GH_ParamAccess.item);
            pManager.AddNumberParameter("X-dimension", "X", "X-dimension of printer volume. Default: 256 [mm]", GH_ParamAccess.item);
            pManager.AddNumberParameter("Y-dimension", "Y", "Y-dimension of printer volume. Default: 256 [mm]", GH_ParamAccess.item);
            pManager.AddNumberParameter("Z-dimension", "Z", "Z-dimension of printer volume. Default: 256 [mm]", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBoxParameter("BoundingBox", "BB", "Minimum bounding box within given dimensions.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // assign input
            Mesh inputBody = null;
            if (!DA.GetData(0, ref inputBody)) return;
            // default dimensions: full print volume of BambuLab P1S without AMS in [mm]
            var xDim = 256.0;
            var yDim = 256.0;
            var zDim = 256.0;
            DA.GetData(1, ref xDim);
            DA.GetData(2, ref yDim);
            DA.GetData(3, ref zDim);
            if (!RhinoMath.IsValidDouble(xDim)) return;
            if (!RhinoMath.IsValidDouble(yDim)) return;
            if (!RhinoMath.IsValidDouble(zDim)) return;

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;
            var atol = RhinoDoc.ActiveDoc?.ModelAngleToleranceRadians ?? Math.PI / 180.0;

            // sort dimensions smallest to largest
            var sortedInputDim = new[] { xDim, yDim, zDim }
                                    .OrderBy(x => x)
                                    .ToList();

            var possibleBoxes = new List<Box>();
            var orientedBoxes = Utils.YieldOrientedBoxes(inputBody, tol, atol, this);
            foreach (Box box in orientedBoxes)
            {
                if (!box.IsValid) continue;

                var xInterval = box.X;
                var yInterval = box.Y;
                var zInterval = box.Z;
                var sortedBbDim = new[] { xInterval.Length, yInterval.Length, zInterval.Length }
                                    .OrderBy(a => a)
                                    .ToList();
                // compare dimensions
                if (sortedBbDim[0] <= sortedInputDim[0])
                    if (sortedBbDim[1] <= sortedInputDim[1])
                        if (sortedBbDim[2] <= sortedInputDim[2])
                            possibleBoxes.Add(box);
            }

            // Sort the bounding boxes by volume
            List<Box> sortedBoundingBoxes = possibleBoxes.OrderBy(o => o.Volume).ToList();

            // assign output
            if (null != sortedBoundingBoxes && sortedBoundingBoxes.Count > 0) DA.SetData(0, sortedBoundingBoxes[0]);
            else DA.SetData(0, Box.Unset);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_CheckPVol;

        public override Guid ComponentGuid => new Guid("2403AFE8-9B57-4944-BC14-C2367BE70F17");
    }
}