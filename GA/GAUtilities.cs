using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
 
using Grasshopper;
using Grasshopper.Kernel.Data;
using Rhino;
using Rhino.Geometry;
 

namespace Morpho
{
    public static class GAUtilities
    {
       public static double Remap(double x, double Min, double Max, double newMin, double newMax)
       {
            double B = ((x - Min) / (Max - Min)) * (newMax - newMin) + newMin;
            return B;
       }

        public static double Remap(double x, double newMin, double newMax)
        {
            return Remap(x,0,1,newMin,newMax);
        }
        public static double Remap(double x)
        {
            return Remap(x, 0, 1, 0.3, 0.9);
        }
        ////////////////////////////Functions to create the tower inside the GA//////////////////////////////////////////
        public static Box MakeTowerVolume(double xbound, double ybound, double xpos, double ypos, Rectangle3d Plot, double FAR)
        {
          
            Interval x = new Interval(-xbound, xbound);

            Interval y = new Interval(-ybound, ybound);
 
            var TFA = FAR * Plot.Area/ 100;
            var floors = (int)(TFA / (x.Length * y.Length));
            var zmax = floors * 4.5;

            Interval z = new Interval(0, zmax);
          
            //the bounding box of the tower
            Box Start = new Box(new Plane(new Point3d(xpos, ypos, 0), Vector3d.ZAxis), x, y, z);
        
            return Start;
        }

        //call the divide method to divide the bounding box into voxels
        //if stages =0 voxels = 2,stages = 1 voxels = 4....stages = 5  voxels = 64
        public static List<Box> ApplyStages(double xb, double yb,double xpos, double ypos, double x0, double y0, double z0,
            double x1, double y1, double z1, double x2, double y2, double z2,
          double x3, double y3, double z3, Rectangle3d Plot, double FAR)
        {
             
            List<Box> Tower = new List<Box>();

            Box Start = MakeTowerVolume(xb, yb, xpos, ypos, Plot, FAR);
            List<Box> temp = new List<Box>();

            // 2 voxels
            temp.AddRange(Divide(x0, y0, z0, Start));

            //4 voxels
            List<Box> temp1 = new List<Box>();
            foreach (var item in temp)
                temp1.AddRange(Divide(x1, y1, z1, item));

            //8 voxels
            List<Box> temp2 = new List<Box>();
            foreach (var item in temp1)
                temp2.AddRange(Divide(x2, y2, z2, item));

            //16voxels
            List<Box> temp3 = new List<Box>();
            foreach (var item in temp2)
                temp3.AddRange(Divide(x3, y3, z3, item));

            Tower.AddRange(temp3);

            return Tower;
        }

        //Divide each voxel face into points
        public static List<Box> Divide(double xx, double yy, double zz, Box Start)
        {
            //convert the starting box to a brep
            Brep bstart = Start.ToBrep();
          
            //create a random point inside the volume that will define the first subdivision
            Point3d pt1 = Start.PointAt(xx , yy , zz );
      
            Vector3d vec = new Vector3d();

            var values = new List<String>() { "X", "Y", "Z" };
            var keys = new double[] { Start.X.Length, Start.Y.Length, Start.Z.Length };
           
            //https://stackoverflow.com/questions/1760185/c-sharp-sort-list-while-also-returning-the-original-index-positions
            var sorted = values.OrderBy(i => keys[values.IndexOf(i)]).ToList();
            switch (sorted.Last())
            {
                case "X":
                    vec = Vector3d.XAxis;
                    break;
                case "Y":
                    vec = Vector3d.YAxis;
                    break;
                case "Z":
                    vec = Vector3d.ZAxis;
                    break;
                default:
                    break;
            }
            Plane pl = new Plane(pt1, vec);

            //Perform solid intersection between the plane and the box
            Curve[] myArrayC = new Curve[0];
            Point3d[] myArrayP = new Point3d[0];

            bool intersection = Rhino.Geometry.Intersect.Intersection.BrepPlane(bstart, pl, 0.001, out myArrayC, out myArrayP);
           
            Brep[] cut = Rhino.Geometry.Brep.CreatePlanarBreps(myArrayC, 0.001);

            //Split the original brep into two
            Brep[] newbreps = bstart.Split(cut.First(), 0.5);

            List<Box> newboxes = new List<Box>();
            //convert the breps into boxes to "feed" them again  to the method and subdivide them
            for (int i = 0; i < newbreps.Length; i++)
            {
                newboxes.Add(new Box(newbreps[i].GetBoundingBox(true)));
            }

            return newboxes;
        }

        //Delete some voxels randomly(20% probability to delete)
        public static List<Box> RandomReduce(List<Box> DividedTower, double reduction_max)
        {         
            List<Box> ReducedTower = new List<Box>();
                          
                //make 10 random, non repetitive indices
                //https://stackoverflow.com/questions/26931528/random-number-generator-with-no-duplicates
                var rnd = new Random();
                var indexes = Enumerable.Range(0, 15).OrderBy(x => rnd.Next()).Take((int)reduction_max).ToList();
          
            for (int i = 0; i < DividedTower.Count; i++)
            {
                 //those boxes will be the ones kept
                  if ((indexes.Contains(i))&&(!(i==0)))            
                  ReducedTower.Add(DividedTower[i]);
            }
            
            return ReducedTower;
        }

        //Allow a number of voxels to rotate,potentially to better, less obstracted views
        public static List<Box> Rotate(List<Box> DividedTower, double rotation_num, double angle)
        {
            //make 6 random, non repetitive indices
            //https://stackoverflow.com/questions/26931528/random-number-generator-with-no-duplicates
          
            List<Box> copy = new List<Box>();
    
                var rnd = new Random();
                var indexes = Enumerable.Range(0, 15).OrderBy(x => rnd.Next()).Take((int)rotation_num).ToList();
                int counter = 0;
                foreach (var item in DividedTower)
                {  
                    var rndrot = new Random();
                    //rotate the boxes that belong to those indices
                    if (indexes.Contains(counter))
                    {
                        //the angle from the genotype will define the uper limit
                        //I create the r to be the rotation and I want different rotation for each box
                        var r = rndrot.Next(43, (int)angle);
                        Transform rotate = Transform.Identity;             
                        rotate = Transform.Rotation(Rhino.RhinoMath.ToRadians((double)r), item.Center);
                        item.Transform(rotate);
                    
                    }
                    //add the original boxes to the list
                    copy.Add(item);                   
                    counter++;
                }
 
            return copy;

        }
        ////////////////////////////Functions to create points on the faces of each voxel//////////////////////////////////////////

        public static void DivideBoxFaces2(List<Box> Boxes, out List <Point3d> allPoints, out List <Vector3d> allVectors)
        {
            allPoints = new List<Point3d>();
            allVectors = new List<Vector3d>();
            for (int i = 0; i < Boxes.Count; i++)
            {
                var faces = Boxes[i].ToBrep().Faces;

                //Get the faces of each box
                for (int j = 0; j < 4; j++)
                {
                    double U = faces[j].Domain(0).Mid;
                    double V = faces[j].Domain(1).Mid;
                   Point3d pt = faces[j].PointAt(U, V) ;
                   Vector3d norm =  faces[j].NormalAt(U, V);
                    allPoints.Add(pt);
                    allVectors.Add(norm);
                }
            }
        }
        
        
        public static void DivideBoxFaces(List<Box> Boxes, int U_Count, int V_Count, double offset, out List <Point3d> allPoints, out List <Vector3d> allVectors)
        {
 
            //all the points of a box
            allPoints = new List<Point3d>();

            //all the normals of a box
            allVectors = new List <Vector3d>();
 
            for (int i = 0; i < Boxes.Count; i++)
            {
                var faces = Boxes[i].ToBrep().Faces;
     
                //Get the faces of each box
                for (int j = 0; j < 4; j++)
                {
                    //for each surface use a smaller domain to create the points
                    double minU = faces[j].Domain(0).Min + (offset * faces[j].Domain(0).Length);
                    double maxU = faces[j].Domain(0).Max - (offset * faces[j].Domain(0).Length);
                    double minV = faces[j].Domain(1).Min + (offset * faces[j].Domain(1).Length);
                    double maxV = faces[j].Domain(1).Max - (offset * faces[j].Domain(1).Length);

                    var myU = new Interval(minU, maxU);
                    var myV = new Interval(minV, maxV);
                 
                     var divU = myU.Length / U_Count;
                    var divV = myV.Length / V_Count;
                 
                   //Subdivide the domain and create points on the subdivisions
                    for (int l = 0; l < U_Count + 1; l++)
                    {
                        for (int m = 0; m < V_Count + 1; m++)
                        {
                            Vector3d tempV = faces[j].NormalAt(myU[0] + (divU * l), myV[0] + (divV * m));
                            Point3d temp = faces[j].PointAt(myU[0] + (divU * l), myV[0] + (divV * m));

                            allPoints.Add(temp);
                            allVectors.Add(tempV);
                        }
                    }
                }
 
            }
         
        }

        //this nethod will take the points created on the faces of boxes and will give back only the
        //usefull ones for the evaluation(delete points in faces inside the tower volume
        public static void Filter(List<Point3d> allPoints, List<Vector3d> allVectors, Mesh Buildings, Mesh Tower, double search_rad,
          out List<Line> FilteredLines, out List<Point3d> FilteredPoints, out List<Vector3d> FilteredVectors)
        {
            FilteredLines = new List<Line>();
            FilteredPoints = new List<Point3d>();
            FilteredVectors = new List<Vector3d>();

            for (int i = 0; i < allPoints.Count; i++)
            {
                Point3d[] hitPt0;
               
                Point3d startp = allPoints[i] + (allVectors[i] * 0.1);
                Line extended = new Line(startp, allVectors[i], search_rad);

                //Get  all the intersection Points
                hitPt0 = Rhino.Geometry.Intersect.Intersection.MeshLine(Tower, extended, out _);
                if (hitPt0.Length == 0)
                {
                    Point3d[] hitPt;
                    
                   //Get  all the intersection Points
                    hitPt = Rhino.Geometry.Intersect.Intersection.MeshLine(Buildings, extended, out _);

                    //Some lines hit meshes in multiple faces so get only the first intersection point
                    if (hitPt.Length > 0)
                    {
                        Line ln = new Line(startp, Rhino.Collections.Point3dList.ClosestPointInList(hitPt, startp));

                        FilteredLines.Add(ln);
                        FilteredPoints.Add(ln.From);
                        FilteredVectors.Add(allVectors[i]);
                    }
                    else
                    {
                        //some lines didn't hit anything so create a line from the search point to the max of search_rad
                        Line ln = new Line(startp, extended.To);
                        FilteredLines.Add(ln);
                        FilteredPoints.Add(ln.From);
                        FilteredVectors.Add(allVectors[i]);
                    }

                }
            }
        }

        public static double RaysSum(List<Line> FilteredLines, double search_rad)
        {
            double total_length = 0;
            for (int i = 0; i < FilteredLines.Count; i++)
            {
               
                    total_length += FilteredLines[i].Length;
            }
            //if all the rays had no obstacles stoppnig them their length would be  equal to the search_rad
            //and their sum would be their number* multiplied with their length = search_rad
            var max_achievable_length = FilteredLines.Count * search_rad;
            var percentage = total_length / max_achievable_length * 100;
            return percentage;

        }

        public static double GetCaptures( List<Point3d> allPoints, List<Vector3d> allVectors, int width, int height, bool savefiles)
        {
            int captureSum = 0;
            int total_sum = 0;
           
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
                    string FName = @"C:\Users\Eleni\Desktop\New folder (2)\point" + i.ToString() + ".jpg";
                    bit.Save(FName);
                }

                //read all the pixels for each bitmap
                 
                for (int j = 0; j < width; j++)
                {
                    for (int k = 0; k < height; k++)
                    {
                        System.Drawing.Color color = bit.GetPixel(j, k);
                        total_sum++;

                        if (color == System.Drawing.Color.FromArgb(255, 0, 0))
                        {
                            captureSum++;
                             
                        }
                    }

                }
                
 

            }
            double percentage = (double)captureSum / total_sum * 100;

            return percentage;
        }
         
    }
}
