using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace DigitalFormwork
{
    public class Comp_BOffset : GH_Component
    {
        public Comp_BOffset()
          : base("Offset Brep",
                "offBrep",
                "Offsets Brep. May run slowly for Breps with a high number of faces.",
                "DigitalFormwork",
                "Modeling")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Cast Body", "CB", "The body to create formwork for.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Offset", "O", "Offset distance for the outer layer.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Tolerance", "T", "Optional. Tolerance. Adjust if no offset is produced. Set the value as high as necessary, but keep it as low as possible.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Fillet Edges", "F", "Optional. Create offset with filleted edges.", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Offset", "O", "Offset brep body.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // assign input
            Brep inputBody = null;
            if (!DA.GetData(0, ref inputBody)) return;
            if (!Utils.CheckBrep(inputBody, this)) return;

            var inputOffset = 10.0;
            if (!DA.GetData(1, ref inputOffset)) return;
            if (inputOffset == 0.0) return;

            var tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-3;
            DA.GetData(2, ref tol);
            if (tol == 0) tol = RhinoMath.ZeroTolerance;

            var fillet = true;
            DA.GetData(3, ref fillet);

            // offset Brep
            // solid = false, extend = !fillet, shrink = true
            var offsetBody = Brep.CreateOffsetBrep(inputBody, inputOffset, false, !fillet, true, tol, out _, out _);
            var outputBreps = new List<Brep>();

            if (null != offsetBody || offsetBody.Length > 0)
            {
                foreach (Brep bo in offsetBody)
                {
                    outputBreps.Add(bo);
                }
            }

            // assign output
            DA.SetDataList(0, outputBreps);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icon_BOffset;

        public override Guid ComponentGuid => new Guid("AF59A689-966E-4F0A-AA9E-70C5E2940F05");
    }
}