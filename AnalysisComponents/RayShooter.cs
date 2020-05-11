using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;

namespace Morpho.AnalysisComponents
{
    public class RayShooter : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public RayShooter()
          : base("RayShooter", "RSH",
              "Shoots rays to neighbooring buildings to check view obstraction",
              "Morpho", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Tower", "T", "The  tower made of boxes", GH_ParamAccess.list);
            pManager.AddMeshParameter("Neighborhood", "N", "The buildings around the tower ", GH_ParamAccess.list);
            pManager.AddPointParameter("Points", "P", "The initial division points that will be filtered", GH_ParamAccess.list);
            pManager.AddVectorParameter("Vectors", "V", "The vectors of the points", GH_ParamAccess.list);
            pManager.AddNumberParameter("Search_Radius", "SR", "The rays will be inside the SR ", GH_ParamAccess.item, 200);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Rays", "R", "The rays from the tower to surounding buildings", GH_ParamAccess.list);
            pManager.AddNumberParameter("Total Ray Value", "TRV", " Bigger TRV indicates a less obstracted view,maximum value is 100", GH_ParamAccess.item);
            pManager.AddPointParameter("Filtered Points", "FP", "The filtered points", GH_ParamAccess.list);
            pManager.AddVectorParameter("Filtered Vectors", "FV", "The filtered vectors", GH_ParamAccess.list);
            pManager.AddColourParameter("Colors", "COL", "Colors indicating the view from each point, red=bad, orange=medium, green=good", GH_ParamAccess.list);

        }

        public static List<Brep> Tower;
        public static List<Mesh> Neighborhood;
        public static List<Point3d> Points;
        public static List<Vector3d> Vectors;
        public static double Search_Radius;

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Tower = new List<Brep>();
            Neighborhood = new List<Mesh>();
            Points = new List<Point3d>();
            Vectors = new List<Vector3d>();
            Search_Radius = 200;



            if (!DA.GetDataList(0,  Tower)) return;
            if (!DA.GetDataList(1,  Neighborhood)) return;
            if (!DA.GetDataList(2,  Points)) return;
            if (!DA.GetDataList(3,  Vectors)) return;
            if (!DA.GetData(4, ref Search_Radius)) return;

            //join the neighborhood meshes into a mesh
            //Create  a joined mesh         
            Mesh JoinedNeighborhood = new Mesh();
            JoinedNeighborhood.Append(Neighborhood);
           
            //convert the tower to one joined mesh
            Mesh MeshT = MeshTower(Tower);
            //get the rays
            Filter(Points, Vectors, JoinedNeighborhood, MeshT, Search_Radius, out List<Line> FilteredLines, out List<Point3d> FilteredPoints,out List<Vector3d> FilteredVectors);
            //get the rays sum
            double Sum = RaysSum(FilteredLines, Search_Radius);
           
            DA.SetDataList(0, FilteredLines);
            DA.SetData(1, Sum);
            DA.SetDataList(2, FilteredPoints);
            DA.SetDataList(3, FilteredVectors);
            DA.SetDataList(4,ColorPoints(FilteredLines));
        }

        public Mesh MeshTower(List <Brep> BoxTower)
        {
            List<Mesh> allMeshes = new List<Mesh>();
      
            //Convert the  Tower voxels from boxes to meshes
            for (int i = 0; i < BoxTower.Count; i++)
            {
                Mesh[] temp = Mesh.CreateFromBrep(BoxTower[i], new MeshingParameters());
                allMeshes.AddRange(temp.ToList());
            }

            //Create  a joined mesh         
            Mesh All = new Mesh();
            All.Append(allMeshes);
            return All;
        }
        public void Filter(List<Point3d> allPoints, List<Vector3d> allVectors, Mesh Buildings, Mesh Tower, double search_rad,
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
        public List<Color> ColorPoints(List<Line> FilteredLines)
        {
            List<Color> Colors = new List<Color>();
            for (int i = 0; i < FilteredLines.Count; i++)
            {
                if(FilteredLines[i].Length/Search_Radius<= 0.333)
                {
                    Colors.Add(Color.FromArgb(255, 126, 0));
                }
                else if(FilteredLines[i].Length/Search_Radius <=  0.666)
                {
                    Colors.Add(Color.FromArgb(253, 255, 74));
                }
                else
                {
                    Colors.Add(Color.FromArgb(102, 255, 86));
                }

            }

            return Colors;
        }

        public double RaysSum(List<Line> FilteredLines, double search_rad)
        {
            double total_length = 0;
            for (int i = 0; i < FilteredLines.Count; i++)
            {

                total_length += FilteredLines[i].Length;
            }
            //if all the rays had no obstacles their maximum length would be search_rad
            //at their sum would be their number* multiplied with their length = search_rad
            var max_achievable_length = FilteredLines.Count * search_rad;
            var percentage = total_length / max_achievable_length * 100;
            return percentage;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Morpho.Properties.Resources.Rays;
                //return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("53b06f70-4fc1-4860-ba8e-67cb04960166"); }
        }
    }
}