using System;
using System.Collections.Generic;
using Rhino;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Media;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Morpho
{
    public class ViewCapture : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ViewCapture()
          : base("ViewCapture", "ViewCapture",
              "Everything colored red, in your viewport, is considered a good view.Get captures from each point to check your tower's views. ",
              "Morpho", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Use the pManager object to register your input parameters.
            // You can often supply default values when creating parameters.
            // All parameters must have the correct access type. If you want 
            // to import lists or trees of values, modify the ParamAccess flag.
            pManager.AddPointParameter("Filtered_Points", "FP", "Captures will be taken from each filtered point", GH_ParamAccess.list);
            pManager.AddVectorParameter("Filtered_Vectors", "FV", "The vectors on the filtered points", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Width", "W", "The width in pixels of each saved capture", GH_ParamAccess.item, 20);
            pManager.AddIntegerParameter("Height", "H", "The height in pixels of each saved capture", GH_ParamAccess.item, 20);
            pManager.AddBooleanParameter("Save_Captures", "SC", "Keep false for faster performance", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Capture", "C", "Turn Capture to true,get the captures, and change back  to false", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Path", "P", "The folder path to save the captures", GH_ParamAccess.item);


        }
        public static List<Point3d> Filtered_Points;
        public static List<Vector3d> Filtered_Vectors;

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {         
            pManager.AddNumberParameter("Total Capture", "TCV", "The maximum capture value is 100", GH_ParamAccess.item);
            pManager.AddColourParameter("Colors", "COL", "Colors indicating the view from each point, red=bad, orange=medium, green=good", GH_ParamAccess.list);
             
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Filtered_Points = new List<Point3d>();
            Filtered_Vectors = new List<Vector3d>();
            int Width = 10;
            int Height = 10;
            bool Save_Captures = false;
            bool Capture = false;
            string Path = "";

            // Then we need to access the input parameters individually. 
            // When data cannot be extracted from a parameter, we should abort this method.
            if (!DA.GetDataList(0, Filtered_Points)) return;
            if (!DA.GetDataList(1, Filtered_Vectors)) return;
            if (!DA.GetData(2, ref Width)) return;
            if (!DA.GetData(3, ref Height)) return;
            if (!DA.GetData(4, ref Save_Captures)) return;
            if (!DA.GetData(5, ref Capture)) return;
            if (!DA.GetData(6, ref Path)) return;

            double cs = 0;

            List<Color> Cl = new List<Color>();
            if (Capture)
            {              
               cs = GetCaptures(Filtered_Points, Filtered_Vectors, Width, Height, Save_Captures, Path, out List<Color> Colors);
               Cl.AddRange(Colors);
            }
           

            DA.SetData(0, cs);
            DA.SetDataList(1, Cl);

        }
        public static double GetCaptures(List<Point3d> allPoints, List<Vector3d> allVectors, int width, int height, bool savefiles,string Path,out List<Color> Colors)
        {
            int captureSum = 0;
            int total_sum = 0;
             Colors = new List<Color>();

            for (int i = 0; i < allPoints.Count; i++)
            {
                
                //the target of the capture(look straight in front of you)
                Point3d endp = allPoints[i] + (allVectors[i] * 10);
                var RhDocument = RhinoDoc.ActiveDoc;
                //capture what each points can "see"
                RhDocument.Views.ActiveView.ActiveViewport.SetCameraLocations(endp, allPoints[i]);              
                var view = RhDocument.Views.ActiveView;

                Bitmap bit = view.CaptureToBitmap(new Size(width, height), false, false, false);

                //Don't save the image to run faster
                if (savefiles)
                {
                    //string FName = @"C:\Users\Eleni\Desktop\New folder (2)\point" + i.ToString() + ".jpg";
                    //C:\Users\Eleni\Desktop\New folder (2)
                    string FName = @""+ Path + "point" + i.ToString() + ".jpg";
                   // RhinoApp.WriteLine(FName);
                    bit.Save(FName);
                   

                }

                //read all the pixels for each bitmap
                var point_sum = 0;
                for (int j = 0; j < width; j++)
                {
                    for (int k = 0; k < height; k++)
                    {
                        Color color = bit.GetPixel(j, k);
                        total_sum++;
                       //all the objects considered good view(like landmakrs) should be colored red in Rhino
                        if (color == Color.FromArgb(255, 0, 0))
                        {
                            captureSum++;
                            point_sum++;
                        }
                    }

                }
                double perc = (double)point_sum/(double)(width*height);
               
                //'bad" views
                if (perc < 0.333)
                {
                    Colors.Add(Color.FromArgb(255,126,0));
                }
                //"average" views
                else if (perc < 0.666)
                {
                    Colors.Add(Color.FromArgb(253, 255, 74));
                }
                //"good" views
                else
                {
                    Colors.Add(Color.FromArgb(102, 255, 86));
                }

                
            }
            double percentage = (double)captureSum/total_sum *100;

            return percentage;
        }
        public static double Remap(double x, double Min, double Max, double newMin, double newMax)
        {
            double B = ((x - Min) / (Max - Min)) * (newMax - newMin) + newMin;
            return B;
        }




        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Morpho.Properties.Resources.Capture;

                //return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3f4ad2db-2f05-4027-a87c-e1de0ecd9b81"); }
        }
    }
}
