using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace DigitalFormwork.Components.Analysis
{
    public class Comp_BGenVector : GH_Component
    {
        public Comp_BGenVector()
          : base("Generate Vector From Brep Face Normal",
                "GenVecBrep",
                "Generates Vectors based on face indices.",
                "DigitalFormwork",
                "Analysis")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "The Brep that will be used to generate vectors from its faces.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Face Indices", "I", "Face indices to create formwork removal vectors from.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("U", "U", "Optional. Percentage [0-100%] of the U-domain of the selected face.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("V", "V", "Optional. Percentage [0-100%] of the V-domain of the selected face.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Length", "L", "Vector Length. Defaults to length of the Bounding Box.", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Anchors", "A", "Anchor points for each vector for easy vector display.", GH_ParamAccess.list);
            pManager.AddVectorParameter("Vectors", "V", "Vectors with an appropriate length for collision testing.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // assign input
            Brep inputBody = null;
            if (!DA.GetData(0, ref inputBody)) return;
            if (!Utils.CheckBrep(inputBody, this)) return;

            var inputIndices = new List<int>();
            if (!DA.GetDataList(1, inputIndices)) return;

            var inU = -1;
            var inV = -1;
            if(DA.GetData(2, ref inU)) inU = RhinoMath.Clamp(inU, 0, 100);
            if(DA.GetData(3, ref inV)) inV = RhinoMath.Clamp(inV, 0, 100);

            var minVlength = 1.0;
            if(!DA.GetData(4, ref minVlength))
            {
                var bbox = inputBody.GetBoundingBox(false);
                minVlength = bbox.Diagonal.Length;
            }

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;
            var atol = RhinoDoc.ActiveDoc?.ModelAngleToleranceRadians ?? Math.PI / 180.0;

            // generate vectors with anchors
            var anchors = new List<Point3d>();
            var fwrv = new List<Vector3d>();
            for (int i = 0; i < inputIndices.Count; i++)
            {
                // get face
                var currentFaceIndex = RhinoMath.Clamp(inputIndices[i], 0, inputBody.Faces.Count - 1);
                var face = inputBody.Faces[currentFaceIndex];
                var faceNV = Utils.GetNV(face, inU, inV, out Point3d currentBasePt) * minVlength;
                anchors.Add(currentBasePt);
                fwrv.Add(faceNV);
            }

            // assign output
            DA.SetDataList(0, anchors);
            DA.SetDataList(1, fwrv);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_BGenVector;

        public override Guid ComponentGuid => new Guid("77b5013a-7c3c-4497-b38f-3c82d414fc37");
    }
}

/// DigitalFormwork
/// SPDX-License-Identifier: GPL-3.0
/// Copyright(c) 2025 Simon M. Hausknecht