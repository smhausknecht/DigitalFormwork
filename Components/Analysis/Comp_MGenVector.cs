using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace DigitalFormwork.Components.Analysis
{
    public class Comp_MGenVector : GH_Component
    {

        public Comp_MGenVector()
          : base("Generate Vector From Mesh Face Normal",
                "GenVecMesh",
                "Generates Vectors based on face indices.",
                "DigitalFormwork",
                "Analysis")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "The Mesh that will be used to generate vectors from its faces.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Face Indices", "I", "Face indices to create formwork removal vectors from.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Length", "L", "Vector Length. Defaults to length of the Bounding Box.", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Centers", "C", "Face center points. Anchor points for each vector for easy vector display.", GH_ParamAccess.list);
            pManager.AddVectorParameter("Vectors", "V", "Vectors with an appropriate length for collision testing.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // assign input
            Mesh inputBody = null;
            if (!DA.GetData(0, ref inputBody)) return;
            if (!inputBody.IsValid) return;

            var inputIndices = new List<int>();
            if (!DA.GetDataList(1, inputIndices)) return;

            inputBody.Normals.ComputeNormals();
            inputBody.FaceNormals.ComputeFaceNormals();

            var minVlength = 1.0;
            if (!DA.GetData(2, ref minVlength))
            {
                var bbox = inputBody.GetBoundingBox(false);
                minVlength = bbox.Diagonal.Length;
            }

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;
            var atol = RhinoDoc.ActiveDoc?.ModelAngleToleranceRadians ?? Math.PI / 180.0;

            // get centers and vectors
            var centers = new List<Point3d>();
            var rv = new List<Vector3d>();
            for (int i = 0; i < inputIndices.Count; i++)
            {
                var currentFaceIndex = RhinoMath.Clamp(inputIndices[i], 0, inputBody.Faces.Count - 1);
                var currentCentroid = Utils.MeshFaceCentroid(inputBody, inputBody.Faces[currentFaceIndex]);
                var currentCNV = new Vector3d(inputBody.FaceNormals[currentFaceIndex]);
                currentCNV = currentCNV * minVlength;
                centers.Add(currentCentroid);
                rv.Add(currentCNV);
            }

            // assign output
            DA.SetDataList(0, centers);
            DA.SetDataList(1, rv);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_MGenVector;

        public override Guid ComponentGuid => new Guid("34E99012-9FC7-4C96-A895-C4F34DE6E81A");
    }
}

/// DigitalFormwork
/// SPDX-License-Identifier: GPL-3.0
/// Copyright(c) 2025 Simon M. Hausknecht