namespace Morpho.AnalysisComponents
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;
    using Grasshopper.Kernel;
    using Rhino.Geometry;

    /// <summary>
    /// Defines the <see cref="RayShooter" />
    /// </summary>
    public class RayShooter : GH_Component
    {
        /// Initializes a new instance of the RayShooter class.

        /// <summary>
        /// Initializes a new instance of the <see cref="RayShooter"/> class.
        /// </summary>
        public RayShooter()
            : base(
                "Ray Shooter",
                "Ray Shooter",
                "Shoots rays to neighboring buildings to check view obstruction",
                "Morpho",
                "Analysis"
            ) { }

        /// Registers all the input parameters for this component.

        /// <summary>
        /// The RegisterInputParams
        /// </summary>
        /// <param name="pManager">The pManager<see cref="GH_Component.GH_InputParamManager"/></param>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter(
                "Tower",
                "T",
                "The tower made of boxes (voxels).",
                GH_ParamAccess.list
            );
            pManager.AddMeshParameter(
                "Neighborhood",
                "N",
                "All the buildings around the tower or other obstacles.",
                GH_ParamAccess.list
            );
            pManager.AddPointParameter(
                "Points",
                "P",
                "The initial division points to filter.",
                GH_ParamAccess.list
            );
            pManager.AddVectorParameter(
                "Vectors",
                "V",
                "Normal vectors at the points.",
                GH_ParamAccess.list
            );
            pManager.AddNumberParameter(
                "Search Radius",
                "SR",
                "Maximum length of the rays.",
                GH_ParamAccess.item,
                200
            );
            pManager.AddBooleanParameter(
                "Use Parallel",
                "Use Parallel",
                "Set to true to use parallel processing. Default is false (sequential).",
                GH_ParamAccess.item,
                false
            );
        }

        /// Registers all the output parameters for this component.

        /// <summary>
        /// The RegisterOutputParams
        /// </summary>
        /// <param name="pManager">The pManager<see cref="GH_Component.GH_OutputParamManager"/></param>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter(
                "Rays",
                "R",
                "Rays from the tower to surrounding buildings.",
                GH_ParamAccess.list
            );
            pManager.AddNumberParameter(
                "Total Ray Value",
                "TRV",
                "Larger TRV indicates less obstructed view; maximum is 100.",
                GH_ParamAccess.item
            );
            pManager.AddPointParameter(
                "Filtered Points",
                "FP",
                "Filtered points (only on exterior facades).",
                GH_ParamAccess.list
            );
            pManager.AddVectorParameter(
                "Filtered Vectors",
                "FV",
                "Normal vectors at filtered points.",
                GH_ParamAccess.list
            );
            pManager.AddColourParameter(
                "Colors",
                "COL",
                "Colors indicating view quality from each point (red=bad, green=good).",
                GH_ParamAccess.list
            );
            pManager.AddNumberParameter(
                "Execution Time (ms)",
                "Execution Time",
                "Time taken to execute the chosen method (in milliseconds).",
                GH_ParamAccess.item
            );
        }

        /// <summary>
        /// Main logic method that performs ray shooting
        /// </summary>
        /// <param name="DA">The DA<see cref="IGH_DataAccess"/></param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Input data retrieval
            List<Brep> tower = new List<Brep>();
            List<Mesh> neighborhood = new List<Mesh>();
            List<Point3d> points = new List<Point3d>();
            List<Vector3d> vectors = new List<Vector3d>();
            double searchRadius = 200;
            bool useParallel = false;

            if (!DA.GetDataList(0, tower))
                return;
            if (!DA.GetDataList(1, neighborhood))
                return;
            if (!DA.GetDataList(2, points))
                return;
            if (!DA.GetDataList(3, vectors))
                return;
            if (!DA.GetData(4, ref searchRadius))
                return;
            DA.GetData(5, ref useParallel); // Parallel or Sequential

            // Mesh preparation
            Mesh joinedNeighborhood = JoinMeshes(neighborhood);
            Mesh towerMesh = MeshTower(tower);

            // Define output variables for lines, points, and vectors
            List<Line> filteredLines;
            List<Point3d> filteredPoints;
            List<Vector3d> filteredVectors;

            // Measure execution time
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Choose method based on the Use Parallel input
            if (useParallel)
            {
                FilterPointsParallel(
                    points,
                    vectors,
                    joinedNeighborhood,
                    towerMesh,
                    searchRadius,
                    out filteredLines,
                    out filteredPoints,
                    out filteredVectors
                );
            }
            else
            {
                FilterPointsSequential(
                    points,
                    vectors,
                    joinedNeighborhood,
                    towerMesh,
                    searchRadius,
                    out filteredLines,
                    out filteredPoints,
                    out filteredVectors
                );
            }
            stopwatch.Stop();

            DA.SetDataList(0, filteredLines);
            DA.SetDataList(2, filteredPoints);
            DA.SetDataList(3, filteredVectors);

            double executionTime = stopwatch.Elapsed.TotalMilliseconds;

            // Calculate visibility value and colors for either case
            double visibilityValue = CalculateRayVisibility(filteredLines, searchRadius);
            List<Color> colorMapping = GenerateColorMapping(filteredLines, searchRadius);

            // Set output data
            DA.SetData(1, visibilityValue);
            DA.SetDataList(4, colorMapping);
            DA.SetData(5, executionTime);
        }

        /// <summary>
        /// Joins multiple meshes into one
        /// </summary>
        /// <param name="meshes">The meshes<see cref="List{Mesh}"/></param>
        /// <returns>The <see cref="Mesh"/></returns>
        private Mesh JoinMeshes(List<Mesh> meshes)
        {
            Mesh joinedMesh = new Mesh();
            joinedMesh.Append(meshes);
            return joinedMesh;
        }

        /// <summary>
        /// Converts the tower voxels (Brep) to a single joined mesh
        /// </summary>
        /// <param name="boxTower">The boxTower<see cref="List{Brep}"/></param>
        /// <returns>The <see cref="Mesh"/></returns>
        private Mesh MeshTower(List<Brep> boxTower)
        {
            List<Mesh> allMeshes = new List<Mesh>();
            foreach (Brep brep in boxTower)
            {
                Mesh[] tempMeshes = Mesh.CreateFromBrep(brep, new MeshingParameters());
                if (tempMeshes != null)
                    allMeshes.AddRange(tempMeshes);
            }
            Mesh joinedTowerMesh = new Mesh();
            joinedTowerMesh.Append(allMeshes);
            return joinedTowerMesh;
        }

        // Sequential version of FilterPoints

        /// <summary>
        /// The FilterPointsSequential
        /// </summary>
        /// <param name="allPoints">The allPoints<see cref="List{Point3d}"/></param>
        /// <param name="allVectors">The allVectors<see cref="List{Vector3d}"/></param>
        /// <param name="buildings">The buildings<see cref="Mesh"/></param>
        /// <param name="tower">The tower<see cref="Mesh"/></param>
        /// <param name="searchRadius">The searchRadius<see cref="double"/></param>
        /// <param name="filteredLines">The filteredLines<see cref="List{Line}"/></param>
        /// <param name="filteredPoints">The filteredPoints<see cref="List{Point3d}"/></param>
        /// <param name="filteredVectors">The filteredVectors<see cref="List{Vector3d}"/></param>
        private void FilterPointsSequential(
            List<Point3d> allPoints,
            List<Vector3d> allVectors,
            Mesh buildings,
            Mesh tower,
            double searchRadius,
            out List<Line> filteredLines,
            out List<Point3d> filteredPoints,
            out List<Vector3d> filteredVectors
        )
        {
            filteredLines = new List<Line>();
            filteredPoints = new List<Point3d>();
            filteredVectors = new List<Vector3d>();

            for (int i = 0; i < allPoints.Count; i++)
            {
                Point3d startPoint = allPoints[i] + (allVectors[i] * 0.1);
                Line ray = new Line(startPoint, allVectors[i], searchRadius);

                if (!CheckRayIntersection(tower, ray))
                {
                    if (CheckRayIntersection(buildings, ray, out Line finalRay))
                    {
                        filteredLines.Add(finalRay);
                    }
                    else
                    {
                        filteredLines.Add(ray);
                    }
                    filteredPoints.Add(ray.From);
                    filteredVectors.Add(allVectors[i]);
                }
            }
        }

        // Parallel version of FilterPoints

        /// <summary>
        /// The FilterPointsParallel
        /// </summary>
        /// <param name="allPoints">The allPoints<see cref="List{Point3d}"/></param>
        /// <param name="allVectors">The allVectors<see cref="List{Vector3d}"/></param>
        /// <param name="buildings">The buildings<see cref="Mesh"/></param>
        /// <param name="tower">The tower<see cref="Mesh"/></param>
        /// <param name="searchRadius">The searchRadius<see cref="double"/></param>
        /// <param name="filteredLines">The filteredLines<see cref="List{Line}"/></param>
        /// <param name="filteredPoints">The filteredPoints<see cref="List{Point3d}"/></param>
        /// <param name="filteredVectors">The filteredVectors<see cref="List{Vector3d}"/></param>
        private void FilterPointsParallel(
            List<Point3d> allPoints,
            List<Vector3d> allVectors,
            Mesh buildings,
            Mesh tower,
            double searchRadius,
            out List<Line> filteredLines,
            out List<Point3d> filteredPoints,
            out List<Vector3d> filteredVectors
        )
        {
            ConcurrentBag<Line> linesBag = new ConcurrentBag<Line>();
            ConcurrentBag<Point3d> pointsBag = new ConcurrentBag<Point3d>();
            ConcurrentBag<Vector3d> vectorsBag = new ConcurrentBag<Vector3d>();

            Parallel.For(
                0,
                allPoints.Count,
                i =>
                {
                    Point3d startPoint = allPoints[i] + (allVectors[i] * 0.1);
                    Line ray = new Line(startPoint, allVectors[i], searchRadius);

                    if (!CheckRayIntersection(tower, ray))
                    {
                        if (CheckRayIntersection(buildings, ray, out Line finalRay))
                        {
                            linesBag.Add(finalRay);
                        }
                        else
                        {
                            linesBag.Add(ray);
                        }
                        pointsBag.Add(ray.From);
                        vectorsBag.Add(allVectors[i]);
                    }
                }
            );

            filteredLines = linesBag.ToList();
            filteredPoints = pointsBag.ToList();
            filteredVectors = vectorsBag.ToList();
        }

        /// <summary>
        /// Checks if the ray intersects with the mesh and returns the resulting line
        /// </summary>
        /// <param name="mesh">The mesh<see cref="Mesh"/></param>
        /// <param name="ray">The ray<see cref="Line"/></param>
        /// <param name="resultingLine">The resultingLine<see cref="Line"/></param>
        /// <returns>The <see cref="bool"/></returns>
        private bool CheckRayIntersection(Mesh mesh, Line ray, out Line resultingLine)
        {
            Point3d[] hitPoints = Rhino.Geometry.Intersect.Intersection.MeshLine(mesh, ray, out _);
            if (hitPoints.Length > 0)
            {
                Point3d closestHit = Rhino.Collections.Point3dList.ClosestPointInList(
                    hitPoints,
                    ray.From
                );
                resultingLine = new Line(ray.From, closestHit);
                return true;
            }
            resultingLine = new Line();
            return false;
        }

        /// <summary>
        /// Checks if the ray intersects with the mesh
        /// </summary>
        /// <param name="mesh">The mesh<see cref="Mesh"/></param>
        /// <param name="ray">The ray<see cref="Line"/></param>
        /// <returns>The <see cref="bool"/></returns>
        private bool CheckRayIntersection(Mesh mesh, Line ray)
        {
            return Rhino.Geometry.Intersect.Intersection.MeshLine(mesh, ray, out _).Length > 0;
        }

        /// <summary>
        /// Calculates the total visibility percentage based on ray lengths
        /// </summary>
        /// <param name="filteredLines">The filteredLines<see cref="List{Line}"/></param>
        /// <param name="searchRadius">The searchRadius<see cref="double"/></param>
        /// <returns>The <see cref="double"/></returns>
        private double CalculateRayVisibility(List<Line> filteredLines, double searchRadius)
        {
            double totalLength = filteredLines.Sum(line => line.Length);
            double maxAchievableLength = filteredLines.Count * searchRadius;
            return (totalLength / maxAchievableLength) * 100;
        }

        /// <summary>
        /// Generates colors based on ray length ratios for visibility indication
        /// </summary>
        /// <param name="filteredLines">The filteredLines<see cref="List{Line}"/></param>
        /// <param name="searchRadius">The searchRadius<see cref="double"/></param>
        /// <returns>The <see cref="List{Color}"/></returns>
        private List<Color> GenerateColorMapping(List<Line> filteredLines, double searchRadius)
        {
            List<Color> colors = new List<Color>();
            foreach (Line line in filteredLines)
            {
                double ratio = line.Length / searchRadius;
                if (ratio <= 0.333)
                    colors.Add(Color.FromArgb(255, 126, 0)); // red: poor visibility
                else if (ratio <= 0.666)
                    colors.Add(Color.FromArgb(253, 255, 74)); // orange: medium visibility
                else
                    colors.Add(Color.FromArgb(102, 255, 86)); // green: good visibility
            }
            return colors;
        }

        /// Provides an Icon for the component.

        /// <summary>
        /// Gets the Icon
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Morpho.Properties.Resources.Rays;

        /// Gets the unique ID for this component.

        /// <summary>
        /// Gets the ComponentGuid
        /// </summary>
        public override Guid ComponentGuid => new Guid("53b06f70-4fc1-4860-ba8e-67cb04960166");
    }
}
