using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace DigitalFormwork
{
    public class DigitalFormworkInfo : GH_AssemblyInfo
    {
        public override string Name => "DigitalFormwork";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => Properties.Resources.Icon_DigitalFormwork;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Generate 3D-printable formwork parts.";

        public override Guid Id => new Guid("b81bc43a-ec6b-4d6f-8f17-b101330f7ade");

        //Return a string identifying you or your company.
        public override string AuthorName => "S. M. Hausknecht";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "mxmumpwr70@gmail.com";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
    }
}