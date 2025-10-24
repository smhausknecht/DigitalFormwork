using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Drawing;
using System.Collections.Generic;

namespace DigitalFormwork.Components.Analysis
{
    public class Comp_BViewCover : GH_Component
    {
        private List<Mesh> _meshesGreen = new List<Mesh>();
        private List<Mesh> _meshesYellow = new List<Mesh>();
        private List<Mesh> _meshesRed = new List<Mesh>();
        private BoundingBox _wholeBB;

        public Comp_BViewCover()
          : base("View Removal Vector Coverage On Brep",
                "ViewCoverMesh",
                "For Breps with only planar faces. Highlights all faces that can be moved along a given vector.Red: collision! Green: no collisions; Yellow: no collisions, but face is perpendicular to vector.NoCollision shows the corresponding boolean value for each face at the given index in the list of faces.",
                "DigitalFormwork",
                "Analysis")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Cast Body", "CB", "The body to create formwork for", GH_ParamAccess.item);
            pManager.AddVectorParameter("Removal Vector", "RV", "Formwork removal vector to check for collisions.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("No Collision", "NoColl", "Given for each face of the cast body. True, if the extrusion does not collide with cast body.", GH_ParamAccess.list);
            pManager.AddBrepParameter("Collision Volume", "CV", "In case of collision, shows collision volume", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // clear display
            _meshesGreen.Clear();
            _meshesYellow.Clear();
            _meshesRed.Clear();

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;

            // assign input
            var inputBody = new Brep();
            if (!DA.GetData(0, ref inputBody)) return;
            if (!Utils.CheckBrep(inputBody, this)) return;
            inputBody.Compact();
            if (!Utils.BrepPlanarOnly(inputBody, tol, this)) return;
            _wholeBB = inputBody?.GetBoundingBox(false) ?? BoundingBox.Empty;

            var rv = Vector3d.Unset;
            if (!DA.GetData(1, ref rv)) return;
            if (!rv.IsValid || rv.IsZero)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "At least one invalid fwrv.");
                return;
            }
            var uFwrv = rv;
            uFwrv.Unitize();

            // threshold for two vectors to be considered perpendicular
            var perpThreshold = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;
            // number of points on collision surface in each direction
            var srfRes = 3;


            bool[] noCollision = new bool[inputBody.Faces.Count];
            var collisionVolumes = new List<Brep>();
            for (int fi = 0; fi < inputBody.Faces.Count; fi++)
            {
                // get face centroid normal vector and calculate formwork removal product
                var duplicateOfFace = inputBody.Faces[fi].DuplicateFace(false);
                var currentFace = duplicateOfFace.Faces[0];
                var currentCNV = Utils.GetNV(currentFace, -1, -1, out _);
                var fwrp = currentCNV * uFwrv;

                if (Math.Abs(fwrp) < perpThreshold)                           // fwrp == 0 (surface perpendicular to fwrv)
                {
                    // surface collision
                    var currentCC = Utils.NoCollisionSurface(inputBody, currentFace, rv, srfRes, tol, this, out _);
                    noCollision[fi] = currentCC;
                }
                else if (fwrp > perpThreshold)
                {
                    // volume collision
                    var currentCC = Utils.NoCollisionVolume(inputBody, currentFace, rv, tol, this, out Brep[] collisionVol);
                    noCollision[fi] = currentCC;
                    collisionVolumes.AddRange(collisionVol ?? Array.Empty<Brep>());
                }
                else
                {   // negative fwrp always collides
                    noCollision[fi] = false;
                }

                // preview of collision
                var singleFaceMesh = Mesh.CreateFromBrep(duplicateOfFace, MeshingParameters.Default);
                var displayMesh = singleFaceMesh[0];
                if (noCollision[fi])
                {
                    if (Math.Abs(fwrp) < perpThreshold)
                    {
                        _meshesYellow.Add(displayMesh);
                    }
                    else
                    {
                        _meshesGreen.Add(displayMesh);
                    }
                }
                else
                {
                    _meshesRed.Add(displayMesh);
                }
            }

            // assign output
            DA.SetDataList(0, noCollision);
            DA.SetDataList(1, collisionVolumes);
        }

        // display collisions
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            var matG = new Rhino.Display.DisplayMaterial(args.ShadeMaterial);
            var matY = new Rhino.Display.DisplayMaterial(args.ShadeMaterial);
            var matR = new Rhino.Display.DisplayMaterial(args.ShadeMaterial);

            matG.Diffuse = Color.Green;
            matY.Diffuse = Color.Yellow;
            matR.Diffuse = Color.Red;

            matR.Transparency = 0.30;

            foreach (var mg in _meshesGreen ?? new List<Mesh>())
            {
                args.Display.DrawMeshShaded(mg, matG);
            }
            foreach (var my in _meshesYellow ?? new List<Mesh>())
            {
                args.Display.DrawMeshShaded(my, matY);
            }
            foreach (var mr in _meshesRed ?? new List<Mesh>())
            {
                args.Display.DrawMeshShaded(mr, matR);
            }
        }

        // refit clipping box to whole brep
        public override BoundingBox ClippingBox => _wholeBB;

        protected override Bitmap Icon => Properties.Resources.Icon_BViewCover;

        public override Guid ComponentGuid => new Guid("457E12F4-FCDA-4776-8268-3C7E324CC453");
    }
}

/// DigitalFormwork
/// SPDX-License-Identifier: GPL-3.0
/// Copyright(c) 2025 Simon M. Hausknecht