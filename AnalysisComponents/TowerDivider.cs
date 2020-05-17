using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;

namespace Morpho.AnalysisComponents
{
    public class TowerDivider : GH_Component
    {
      
        /// Initializes a new instance of the MyComponent1 class.   
        public TowerDivider()
          : base("Point Divider", "Point Divider ",
              "Divides each voxel of the tower into points",
              "Morpho", "Analysis")
        {
        }

  
        /// Registers all the input parameters for this component. 
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Tower", "T", "The tower made of boxes(voxels).", GH_ParamAccess.list);
            pManager.AddIntegerParameter("U Count", "U", "Number of segments in {u} direction.", GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("V Count", "V", "Number of segments in {v} direction.", GH_ParamAccess.item, 2);
            pManager.AddNumberParameter("Offset", "OF", "The distance of the division points, from the edges of the voxel. ", GH_ParamAccess.item, 0.1);
        }
 
        /// Registers all the output parameters for this component. 
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The initial division points that will be filtered.", GH_ParamAccess.list);
            pManager.AddPointParameter("Vectors", "V", "The vector normals of the initial points.", GH_ParamAccess.list);
        }

        public static List<Brep> Tower;
        public static List<Mesh> Neighborhood;
        public static int U_Count;
        public static int V_Count;
        public static double Offset;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Tower = new List<Brep>();
            U_Count = 2;
            V_Count = 2;
            Offset = 2;


            if (!DA.GetDataList(0, Tower)) return;       
            if (!DA.GetData(1, ref U_Count)) return;
            if (!DA.GetData(2, ref V_Count)) return;
            if (!DA.GetData(3, ref Offset)) return;
           
            List<Point3d> points = new List<Point3d>();
            List<Vector3d> vecs = new List<Vector3d>();

            DivideBoxFaces(Tower, U_Count, V_Count, Offset, out DataTree<Point3d> allPoints, out DataTree<Vector3d> allVectors);
            allPoints.Flatten(null);
            points.AddRange(allPoints.AllData());

            allVectors.Flatten(null);
            vecs.AddRange(allVectors.AllData());

            DA.SetDataList(0, points);
            DA.SetDataList(1, vecs);

        }
        public void DivideBoxFaces(List<Brep> Boxes, double U_Count, double V_Count, double offset, out DataTree<Point3d> allPoints, out DataTree<Vector3d> allVectors)
        {
            //List<Mesh> allMeshes = new List<Mesh>();
            allPoints = new DataTree<Point3d>();

            //all the normals of a box
            allVectors = new DataTree<Vector3d>();

     
            for (int i = 0; i < Boxes.Count; i++)
            {
                var faces = Boxes[i].Faces;

                //Get the division points of each face and their normals
                var myPoints = new List<Point3d>();
                var myNormals = new List<Vector3d>();

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
                            myPoints.Add(temp);
                            myNormals.Add(tempV);
                        }
                    }
                }
                allPoints.AddRange(myPoints, new GH_Path(new int[] { i, 0 }));
                allVectors.AddRange(myNormals, new GH_Path(new int[] { i, 0 }));

            }

            allPoints.Flatten(null);
            allVectors.Flatten(null);
        }

 
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                
                return Morpho.Properties.Resources.DivideP;
            }
        }
 
        public override Guid ComponentGuid
        {
            get { return new Guid("af7baae4-8209-4e07-96a1-035e92cf3a13"); }
        }
    }
}