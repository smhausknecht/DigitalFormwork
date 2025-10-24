using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DigitalFormwork.Components.Analysis
{
    public class Comp_MCheckColl : GH_Component
    {
        private BoundingBox _wholeBB;
        private List<Mesh> _meshesRed = new List<Mesh>();
        private Rhino.Display.DisplayMaterial _matR = new Rhino.Display.DisplayMaterial(Color.Red, 0.0);

        public Comp_MCheckColl()
          : base("Check Formwork Removal",
                "CheckRemoval",
                "For Meshes. Checks whether a formwork part can be removed without collision.",
                "DigitalFormwork",
                "Analysis")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Cast Body", "CB", "Cast body for the formwork parts.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Formwork", "F", "Formwork part", GH_ParamAccess.item);
            pManager.AddVectorParameter("Removal Vector", "V", "Removal vector of formwork part", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Point Density", "P", "Adjusts number of points to test for each mesh face. Default = 1. (N=0: only centroid; N=1: centroid and vertices; N=2: additional points)", GH_ParamAccess.item);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("No Collision", "NC", "True if formwork can be removed safely.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Collision Points", "CP", "Collision points.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // clear display
            _meshesRed.Clear();

            // assign input
            var inputBody = new Mesh();
            if (!DA.GetData(0, ref inputBody)) return;

            var formwork = new Mesh();
            if(!DA.GetData(1, ref formwork)) return;
            formwork = Utils.CleanMesh(formwork);

            var removalVector = new Vector3d();
            if (!DA.GetData(2, ref removalVector)) return;

            var ptDensity = 1;
            DA.GetData(3, ref ptDensity);
            ptDensity = RhinoMath.Clamp(ptDensity, 0, 100);

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;
            // threshold for two vectors to be considered perpendicular
            var perpThreshold = tol;
            // distance to move vectors away from face to avoid collision
            var dis = (float)(-1.0 * tol);

            // check for collisions
            var collIndices = new SortedSet<int>();
            for (int fi = 0; fi < formwork.Faces.Count; fi++)
            {
                var currentAngle = Vector3d.VectorAngle(formwork.FaceNormals[fi], removalVector);
                var currentRp = Math.Cos(currentAngle);
                if (currentRp < perpThreshold) continue;           // if draft angle is below threshold
                else
                {
                    var faceNoColl = Utils.MeshMeshNoCollision(formwork, fi, inputBody, ptDensity, tol, dis, removalVector, out int[] collFaceIndices);
                    if(!faceNoColl) collIndices.UnionWith(collFaceIndices);
                }
            }
            // display collisions
            var noCollision = false;
            if(collIndices.Count == 0) noCollision = true;
            else
            {
                foreach(var index in collIndices)
                {
                    _meshesRed.Add(Utils.MeshFaceToMesh(inputBody, index));
                }
            }
                
            _wholeBB = inputBody?.GetBoundingBox(false) ?? BoundingBox.Empty;

            // assign output
            DA.SetData(0, noCollision);
            DA.SetDataList(1, collIndices);
        }

        // display collisions
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            foreach (var mr in _meshesRed ?? new List<Mesh>()) args.Display.DrawMeshShaded(mr, _matR);
        }

        // refit clipping box to whole brep
        public override BoundingBox ClippingBox => _wholeBB;

        protected override Bitmap Icon => Properties.Resources.Icon_MCheckColl;

        public override Guid ComponentGuid => new Guid("4563DC6E-B76F-4B95-940F-9B7358ECA6FE");
    }
}

/// DigitalFormwork
/// SPDX-License-Identifier: GPL-3.0
/// Copyright(c) 2025 Simon M. Hausknecht