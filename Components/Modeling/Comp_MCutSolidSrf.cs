using DigitalFormwork.Components;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace DigitalFormwork
{
    public class Comp_MCutSolidSrf : GH_Component
    {

        public Comp_MCutSolidSrf()
          : base("Cut Solid Mesh By Mesh Surface",
                "CutMeshSrf",
                "Cuts solid Mesh by Mesh Surface into solid parts.",
                "DigitalFormwork",
                "Modeling")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Solid mesh to split and cap.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Cutting Surfaces", "CS", "Mesh surfaces to cut mesh with.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Keep Solid", "S", "Create split parts as solids. True by default.", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Parts", "P", "Cut parts.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // assign input
            Mesh inputBody = null;
            if (!DA.GetData(0, ref inputBody)) return;
            if (!Utils.CheckMeshSolid(inputBody, this)) ;

            var cuttingMeshes = new List<Mesh>();
            if (!DA.GetDataList(1, cuttingMeshes)) return;
            if (null == cuttingMeshes || cuttingMeshes.Count == 0) return;
            foreach(var mesh in cuttingMeshes)
            {
                if (!Utils.CheckMeshSolid(mesh, this)) return;
            }

            bool returnSolid = true;
            DA.GetData(2, ref returnSolid);

            // loop through cutting Meshes
            var meshesToCut = new List<Mesh> { inputBody };
            foreach (var cutter in cuttingMeshes)
            {
                var splitParts = new List<Mesh>();
                // loop through breps
                for (int mi = 0; mi < meshesToCut.Count; mi++)
                {
                    if (null == meshesToCut[mi] || !meshesToCut[mi].IsValid) continue;
                    var pieces = Utils.CutMeshByMesh(meshesToCut[mi], cutter, returnSolid, this) ?? Array.Empty<Mesh>();
                    splitParts.AddRange(pieces);
                }
                meshesToCut.Clear();
                meshesToCut.AddRange(splitParts);
                splitParts.Clear();
            }

            // assign output
            DA.SetDataList(0, meshesToCut);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_MCutSolidSrf;

        public override Guid ComponentGuid => new Guid("6074B1E6-D74C-44C5-A270-A85B8595E3BE");
    }
}