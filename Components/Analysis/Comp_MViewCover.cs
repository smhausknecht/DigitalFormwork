using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DigitalFormwork.Components.Analysis
{
    public class Comp_MViewCover : GH_Component
    {
        //private Mesh _wholeBody;
        private BoundingBox _wholeBB;
        private List<Mesh> _meshesRed = new List<Mesh>();
        private List<Mesh> _meshesGreen = new List<Mesh>();
        private Rhino.Display.DisplayMaterial _matR = new Rhino.Display.DisplayMaterial(Color.Red, 0.0);
        private Rhino.Display.DisplayMaterial _matG = new Rhino.Display.DisplayMaterial(Color.Green, 0.0);

        public Comp_MViewCover()
          : base("View Removal Vector Coverage On Mesh",
                "ViewCoverMesh",
                "For Meshes. Checks whether a set of removal vectors covers all faces without collision.",
                "DigitalFormwork",
                "Analysis")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh to check coverage for.", GH_ParamAccess.item);
            pManager.AddVectorParameter("Removal Vectors", "RV", "Removal vectors to check for coverage.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Angle Threshold", "AT", "Maximum angle (in degrees) allowed between removal vector and face normal. Must be between 0 and 90. Default at 0.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Point Density", "P", "Adjusts number of points to test for each mesh face. Default = 1. (N=0: only centroid; N=1: centroid and vertices; N=2: additional points)", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Uncovered faces", "UF", "All faces (in red) that are not covered by the given removal vectors.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // clear display
            _meshesRed.Clear();
            _meshesGreen.Clear();

            // assign input
            var inputBody = new Mesh();
            if (!DA.GetData(0, ref inputBody)) return;
            if (!Utils.CheckMeshSolid(inputBody, this)) return;
            inputBody = Utils.CleanMesh(inputBody);

            var inputVectors = new List<Vector3d>();
            if (!DA.GetDataList(1, inputVectors)) return;
            var uVectors = new List<Vector3d>(inputVectors);
            for (int vi = 0; vi < uVectors.Count; vi++)
            {
                uVectors[vi].Unitize();
            }

            var angleThreshold = 90.0;
            DA.GetData(2, ref angleThreshold);
            angleThreshold = RhinoMath.Clamp(angleThreshold, 0.0, 90.0);
            var radiantThreshold = RhinoMath.ToRadians(angleThreshold);
            var rpThreshold = Math.Cos(radiantThreshold);

            var ptDensity = 1;
            DA.GetData(3, ref ptDensity);
            ptDensity = RhinoMath.Clamp(ptDensity, 0, 100);

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;
            // threshold for two vectors to be considered perpendicular
            var perpThreshold = tol;
            // distance to move vectors away from face to avoid collision
            var dis = (float)tol;

            var noCollision = new bool[inputBody.Faces.Count];
            var uncovered = new List<int>();

            // loop through faces
            for (int fi = 0; fi < inputBody.Faces.Count; fi++)
            {
                // loop through vectors
                for (int vi = 0; vi < inputVectors.Count; vi++)
                {
                    // avoid double checking faces without collision
                    if (noCollision[fi]) continue;

                    var currentAngle = Vector3d.VectorAngle(inputBody.FaceNormals[fi], uVectors[vi]);
                    var currentRp = Math.Cos(currentAngle);
                    if (currentRp < rpThreshold - perpThreshold) noCollision[fi] = false;           // if draft angle is below threshold
                    else
                    {
                        noCollision[fi] = Utils.MeshFaceNoCollision(inputBody, fi, ptDensity, tol, dis, inputVectors[vi]);
                    }
                }
                // add faces to list for display
                if (!noCollision[fi])
                {
                    _meshesRed.Add(Utils.MeshFaceToMesh(inputBody, fi));
                    uncovered.Add(fi);
                }
                else _meshesGreen.Add(Utils.MeshFaceToMesh(inputBody, fi));
            }

            // for display
            _wholeBB = inputBody?.GetBoundingBox(false) ?? BoundingBox.Empty;

            // assign output
            DA.SetDataList(0, uncovered);
        }

        // display collisions
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            foreach (var mr in _meshesRed ?? new List<Mesh>()) args.Display.DrawMeshShaded(mr, _matR);

            foreach (var mg in _meshesGreen ?? new List<Mesh>()) args.Display.DrawMeshShaded(mg, _matG);
        }

        // refit clipping box to whole brep
        public override BoundingBox ClippingBox => _wholeBB;

        protected override Bitmap Icon => Properties.Resources.Icon_MViewCover;

        public override Guid ComponentGuid => new Guid("6CB79B32-EF1B-4036-BEBA-AAE0E097CA4F");
    }
}