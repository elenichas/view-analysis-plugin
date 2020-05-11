using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Morpho
{
    public class MorphoInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Morpho";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return Morpho.Properties.Resources.M;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "A GHA library developed as an assignment for the Morphogenetic Course";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("6d9cbc4e-b0b7-44d9-b655-5e5eb226c53e");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Eleni Chasioti";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "eleni.chasioti.19@ucl.ac.uk";
            }
        }
    }
}
