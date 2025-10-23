using DigitalFormwork.Components;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace DigitalFormwork
{
    public class Comp_MShellSrf : GH_Component
    {
        public Comp_MShellSrf()
          : base("Create Mesh Shell With Mesh Surface",
                "MeshShellSrf",
                "Cuts two Mesh layers by a surface into solid shells.",
                "DigitalFormwork",
                "Modeling")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Inner Shell", "IS", "Inner Shell.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Outer Shell", "OS", "Outer Shell.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Cutting Surface", "CS", "Mesh surface to cut solid with.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Shells", "S", "Formwork shells.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // assign input
            Mesh innerShell = null;
            if (!DA.GetData(0, ref innerShell)) return;
            if (!Utils.CheckMeshSolid(innerShell, this)) return;
            var cutout = new List<Mesh>() { innerShell };

            Mesh outerShell = null;
            if (!DA.GetData(1, ref outerShell)) return;
            if (!Utils.CheckMeshSolid(outerShell, this)) return;

            Mesh cuttingMesh = null;
            if (!DA.GetData(2, ref cuttingMesh)) return;
            if (!Utils.CheckMeshSolid(cuttingMesh, this)) return;

            // set tolerance
            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;

            // cut
            var cutShell = Utils.CutMeshByMesh(outerShell, cuttingMesh, true, this);
            var shells = new List<Mesh>();
            foreach (var part in cutShell)
            {
                if(null == part || !part.IsValid) continue;
                var solid = new List<Mesh>() { part };
                var pieces = Mesh.CreateBooleanDifference(solid, cutout) ?? Array.Empty <Mesh>();
                shells.AddRange(pieces);
            }

            // assign output
            DA.SetDataList(0, shells);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_MShellSrf;

        public override Guid ComponentGuid  => new Guid("0499E695-1D0E-4352-9512-78D092E5AEE1");
    }
}