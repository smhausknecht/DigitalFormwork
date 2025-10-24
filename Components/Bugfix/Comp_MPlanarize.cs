using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;

namespace DigitalFormwork.Components.Bugfix
{
    public class Comp_MPlanarize : GH_Component
    {

        public Comp_MPlanarize()
          : base("Planarize Mesh",
                "PlanMesh",
                "Turns all non-planar quad faces to triangles.",
                "DigitalFormwork",
                "Bugfix")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "The Mesh that will be planarized.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Triangulate", "T", "If true, all faces will be triangulated even if the quad face is planar.", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh with planar faces only.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // assign input
            Mesh inputBody = null;
            if (!DA.GetData(0, ref inputBody)) return;
            if (!Utils.CheckMeshSolid(inputBody, this)) return;
            var planarMesh = inputBody.DuplicateMesh();

            bool forceTriangulate = false;
            DA.GetData(1, ref forceTriangulate);

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;
            var atol = RhinoDoc.ActiveDoc?.ModelAngleToleranceRadians ?? Math.PI / 180.0;

            // planarize faces
            if (forceTriangulate) planarMesh.Faces.ConvertQuadsToTriangles();
            else planarMesh.Faces.ConvertNonPlanarQuadsToTriangles(tol, atol, 0);

            // clean up
            planarMesh = Utils.CleanMesh(inputBody);

            // assign output
            DA.SetData(0, planarMesh);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_MPlanarize;

        public override Guid ComponentGuid => new Guid("1F4BC66F-3461-4753-B37D-A24B0574F012");
    }
}

/// DigitalFormwork
/// SPDX-License-Identifier: GPL-3.0
/// Copyright(c) 2025 Simon M. Hausknecht