using System;
using System.Collections.Generic;
using Rhino;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
 

 
namespace Morpho
{
    public class ViewCapture : GH_Component
    {
    
        public ViewCapture()
          : base("View Capture", "View Capture",
              "Everything colored red, in your viewport, is considered a good view.Get captures from each point to check your tower's views. ",
              "Morpho", "Analysis")
        {
        }

       
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            
            pManager.AddPointParameter("Filtered Points", "FP", "Captures will be taken from each filtered point.", GH_ParamAccess.list);
            pManager.AddVectorParameter("Filtered Vectors", "FV", "The vector normals on the filtered points.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Width", "W", "The width in pixels of each saved capture.", GH_ParamAccess.item, 20);
            pManager.AddIntegerParameter("Height", "H", "The height in pixels of each saved capture.", GH_ParamAccess.item, 20);
            pManager.AddBooleanParameter("Save Captures", "SC", "if true the snapshots will be saved in the path given.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Capture", "C", "Turn Capture to true,get the captures, and change back  to false", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Path", "P", "The folder path to save the captures.", GH_ParamAccess.item);


        }
        public static List<Point3d> Filtered_Points;
        public static List<Vector3d> Filtered_Vectors;

  
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {         
            pManager.AddNumberParameter("Total Capture", "TCV", "The maximum capture value is 100", GH_ParamAccess.item);
            pManager.AddColourParameter("Colors", "COL", "Colors indicating the view from each point, red=bad, orange=medium, green=good", GH_ParamAccess.list);
             
        }
 
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Filtered_Points = new List<Point3d>();
            Filtered_Vectors = new List<Vector3d>();
            int Width = 10;
            int Height = 10;
            bool Save_Captures = false;
            bool Capture = false;
            string Path = "";
 
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

 
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

 
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                 
                return Morpho.Properties.Resources.Capture;
 
            }
        }

 
        public override Guid ComponentGuid
        {
            get { return new Guid("3f4ad2db-2f05-4027-a87c-e1de0ecd9b81"); }
        }
    }
}
