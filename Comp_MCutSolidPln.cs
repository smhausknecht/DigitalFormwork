using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace DigitalFormwork
{
    public class Comp_MCutSolidPln : GH_Component
    {
        public Comp_MCutSolidPln()
          : base("Cut Solid Mesh By Planes",
                "CutMeshPln",
                "Cuts solid Mesh by planes into solid parts.",
                "DigitalFormwork",
                "Modeling")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Solid mesh to split and cap.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Cutting Planes", "CP", "Planes to cut mesh with.", GH_ParamAccess.list);
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
            if (!Utils.CheckMeshSolid(inputBody, this)) return;

            var cuttingPlanes = new List<Plane>();
            if (!DA.GetDataList(1, cuttingPlanes)) return;
            if (null == cuttingPlanes || cuttingPlanes.Count == 0) return;

            bool returnSolid = true;
            DA.GetData(2, ref returnSolid);

            // loop through cutting planes
            var meshesToCut = new List<Mesh> { inputBody };
            foreach (var plane in cuttingPlanes)
            {
                var splitParts = new List<Mesh>();
                // loop through meshes
                for (int mi = 0; mi < meshesToCut.Count; mi++)
                {
                    if (null == meshesToCut[mi] || !meshesToCut[mi].IsValid) continue;
                    var pieces = Utils.CutMeshByPlane(meshesToCut[mi], plane, returnSolid, this) ?? Array.Empty<Mesh>(); 
                    splitParts.AddRange(pieces);
                }
                meshesToCut.Clear();
                meshesToCut.AddRange(splitParts);
                splitParts.Clear();
            }

            // assign output
            DA.SetDataList(0, meshesToCut);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_MCutSolidPln;

        public override Guid ComponentGuid => new Guid("B26FAF8B-B50D-4C12-A2A2-F56B19453597");
    }
}