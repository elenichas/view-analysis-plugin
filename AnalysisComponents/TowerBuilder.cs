﻿using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Linq;

namespace Morpho
{
    public class TowerBuilder : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public TowerBuilder()
          : base("TowerBuilder", "TB",
              "TowerBuilder",
              "Morpho", "Design")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddRectangleParameter("Plot", "P", "Base Rectangle for tower", GH_ParamAccess.item);
            pManager.AddNumberParameter("BCR", "BCR", "Building Coverage Ratio", GH_ParamAccess.item,0.5);
            pManager.AddNumberParameter("FAR", "FAR", "Floor Aspect Ratio", GH_ParamAccess.item,700);
            pManager.AddIntegerParameter("Stages", "S", "NUmber of operations to apply to initial volume", GH_ParamAccess.item,4);
            pManager.AddBooleanParameter("Reduce", "RD", "If true some voxels get deleted, if False output will be a solid tower.", GH_ParamAccess.item, true);
            pManager.AddIntegerParameter("Reduction_max", "RDMX", "The number of voxels to be deleted", GH_ParamAccess.item,1);
            pManager.AddIntegerParameter("Rotation_max", "RDMX", "The number of voxels to be rotated", GH_ParamAccess.item, 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBoxParameter("Tower", "T", "The  tower made of boxes", GH_ParamAccess.list);

        }

        // Variables
        Random rnd;
         
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
            rnd = new Random();
            Rectangle3d Plot = new Rectangle3d();
            double BCR = 0; 
            double FAR = 0;
            int Stages = 1;
            bool Reduce = true;
            int Reduction_max = 6;
            int Rotation_max = 6;

            if (!DA.GetData(0, ref Plot)) return;
            if (!DA.GetData(1, ref BCR)) return;
            if (!DA.GetData(2, ref FAR)) return;
            if (!DA.GetData(3, ref Stages)) return;
            if (!DA.GetData(4, ref Reduce)) return;
            if (!DA.GetData(5, ref Reduction_max)) return;
            if (!DA.GetData(6, ref Rotation_max)) return;

            //list with the divided tower
            List<Box> DividedTower = ApplyStages(Plot, BCR, FAR, Stages);
          
            //list after random voxels removed
            List<Box> DividedTowerR = RandomReduce(DividedTower,Reduction_max);
          
            List<Box> Final_Tower;

            if (Reduce)
                Final_Tower = Rotate(DividedTowerR,Rotation_max);
            else
                Final_Tower = Rotate(DividedTower,Rotation_max);


            DA.SetDataList(0, Final_Tower);
        }

        public Box MakeTowerVolume(Rectangle3d Plot, double BCR, double FAR)
        {
            //how much from the Plot can be covered with a building
            //Building Coverage Ratio = (Building Area/Site Area)* 100
            Rectangle3d Cover = new Rectangle3d(new Plane(Plot.Plane), Plot.Width * BCR, Plot.Height * BCR);
            var xmax = (BCR + BCR / 2) * Plot.Width;

           var  ymax = (BCR + BCR / 2) * Plot.Height;

           var  xbound = rnd.Next((int)xmax, (int)(1.2 * xmax));
            var ybound = rnd.Next((int)ymax, (int)(1.2 * ymax));


            //var xbound = rnd.Next((int)xmax / 3, (int)ymax / 2);
            // var ybound = rnd.Next((int)ymax / 3, (int)ymax / 2);

            Interval x = new Interval(-xbound, xbound);
            Interval y = new Interval(-ybound, ybound);

            //Floor Aspect Ratio  = (Total Floor Area/ Site Area)*100
            var TFA = FAR * Plot.Area / 100;
            var floors = (int)(TFA / (x.Length * y.Length));
            var zmax = floors * 4.5;

            Interval z = new Interval(0, zmax);
            
            //keep the volume inside the plot
            var xpos = rnd.Next(-(int)(Plot.Width / 3),(int) (Plot.Width / 3));
            var ypos = rnd.Next(-(int)(Plot.Height/ 3), (int)(Plot.Height / 3));
     
            //the general volume of the tower
            Box Start = new Box(new Plane(new Point3d(xpos, ypos, 0), Vector3d.ZAxis), x, y, z);

            return Start;

        }

        //call the divide method to create a more detailed tower
        //if stages =0 voxels = 2,stages = 1 voxels = 4....stages = 5  voxels = 64
        public List<Box> ApplyStages(Rectangle3d Plot, double BCR, double FAR, int Stages)
        {
            List<List<Box>> Tower = new List<List<Box>>();
            List<Box> temp = new List<Box>();

            Box Start = MakeTowerVolume(Plot, BCR, FAR);

            temp.AddRange(Divide(Start));
            //Tower[0]
            Tower.Add(temp);

            for (int i = 0; i < Stages; i++)
            {
                List<Box> temp1 = new List<Box>();
                foreach (var item in Tower[i])
                {
                    temp1.AddRange(Divide(item));
                }

                Tower.Add(temp1);
            }
            return Tower[Stages];
        }

        //Divide each voxel face into points
        public List<Box> Divide(Box Start)
        {
            //convert the starting box to a brep
            Brep bstart = Start.ToBrep();

            //create a random point inside the volume that will define the first subdivision
            Point3d pt1 = Start.PointAt(rnd.Next(3, 9) * 0.1, rnd.Next(3, 9) * 0.1, rnd.Next(3, 9) * 0.1);

            Vector3d vec = new Vector3d();
            
            var values = new List<String>() { "X", "Y", "Z" };
            var keys = new double[] { Start.X.Length, Start.Y.Length, Start.Z.Length };
            // https://stackoverflow.com/questions/1760185/c-sharp-sort-list-while-also-returning-the-original-index-positions
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

            Brep[] cut = new Brep[0];
            cut = Rhino.Geometry.Brep.CreatePlanarBreps(myArrayC, 0.001);

            List<Box> newboxes = new List<Box>();

            var cutter = cut;

            //Split the original brep into two
            Brep[] newbreps = bstart.Split(cutter.First(), 0.5);

            //convert the breps into boxes to "feed" them again  to the method and subdivide them
            for (int i = 0; i < newbreps.Length; i++)
            {
                newboxes.Add(new Box(newbreps[i].GetBoundingBox(true)));
            }

            return newboxes;
        }

        //Delete some voxels randomly(20% probability to delete)
        public List<Box> RandomReduce(List<Box> DividedTower,int reduction_max)
        {
            if (reduction_max > DividedTower.Count - 1)
                return null;

            List<Box> ReducedTower = new List<Box>();
                       
           //make 10 random, non repetitive indices
           //https://stackoverflow.com/questions/26931528/random-number-generator-with-no-duplicates
             var rnd = new Random();
             var indexes = Enumerable.Range(0, DividedTower.Count-1).OrderBy(x => rnd.Next()).Take(DividedTower.Count-reduction_max).ToList();
          
            for (int i = 0; i < DividedTower.Count; i++)
            {
                 //those boxes will be the ones kept
                  if ((indexes.Contains(i))&&(!(i==0)))            
                  ReducedTower.Add(DividedTower[i]);
            }
            
            return ReducedTower;
        }

        //Rotate some of the voxels to allow them seek for better views
        //In the GA I might rotate them all
        //50% Probability to rotate from 45 to 345
        public List<Box> Rotate(List<Box> DividedTower,int rotation_max)
        {
            if (rotation_max > DividedTower.Count)
                return null;
            //make 6 random, non repetitive indices
            //https://stackoverflow.com/questions/26931528/random-number-generator-with-no-duplicates

            List<Box> copy = new List<Box>();

            var rnd = new Random();
            var indexes = Enumerable.Range(0, DividedTower.Count - 1).OrderBy(x => rnd.Next()).Take(rotation_max).ToList();
            int counter = 0;
            foreach (var item in DividedTower)
            {
                var rndrot = new Random();
                //rotate the boxes that belong to those indices
                if (indexes.Contains(counter))
                {
                    //the angle from the genotype will define the uper limit
                    //I create the r to be the rotation and I want different rotation for each box
                    var r = rndrot.Next(45, 345);
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

        


        

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Morpho.Properties.Resources.Divide;
              // return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("86b9490f-f756-46ce-ab54-35dc5c9e3216"); }
        }
    }
}