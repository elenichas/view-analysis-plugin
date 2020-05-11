using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Linq;
using Rhino;
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI.Canvas;
using System.Drawing;

namespace Morpho.GA
{
    public class GA : GH_Component
    {
       
        /// Initializes a new instance of the MyComponent1 class.      
        public override void CreateAttributes()
        {
            m_attributes = new CustomAttributes(this);
        }

        public GA()
          :  base("Genetic Algorithm", "GA",
              "Genetic Algorithm",
              "Morpho", "Optimization")
        {
        }
       
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "RN", "Run the Genetic Algorithm", GH_ParamAccess.item,false);
            pManager.AddRectangleParameter("Plot", "P", "The boundary rectangle of the building plot", GH_ParamAccess.item);
            pManager.AddNumberParameter("BCR", "BCR", "building Coverage Ratio", GH_ParamAccess.item,0.5);
            pManager.AddNumberParameter("FAR", "FAR", "Floor Aspect Ratio", GH_ParamAccess.item,600);
            pManager.AddBooleanParameter("Reduce", "RD", "If true random voxels will be deleted from the final tower", GH_ParamAccess.item,false);
           // pManager.AddIntegerParameter("U_Count", "U", "Number of segments in {u} direction", GH_ParamAccess.item,2);
           // pManager.AddIntegerParameter("V_Count", "V", "Number of segments in {v} direction", GH_ParamAccess.item, 2);
           // pManager.AddNumberParameter("Offset", "OF", "The distance of the division points from the edges of the voxel ", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("Search_Radius", "SR", "The GA will search for other buildings inside the SR ", GH_ParamAccess.item, 200);
            pManager.AddMeshParameter("Neighborhood", "N", "The building around the tower ", GH_ParamAccess.list);
        
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBoxParameter("Best Tower", "BT", "The best tower in current generation", GH_ParamAccess.list);
            pManager.AddNumberParameter("Best_Fitness", "BF", "The best fitness in current generation", GH_ParamAccess.item);
            pManager.AddNumberParameter("Best Genotype", "BG", "The best genotype in current generation", GH_ParamAccess.list);
            pManager.AddBoxParameter("All Towers", "AT", "All the towers in current generation", GH_ParamAccess.list);
            pManager.AddNumberParameter("All Fitnesses", "AF", "The best fitness in current generation", GH_ParamAccess.list);
            pManager.AddPointParameter("Positions", "PT", "Follow the positions of the towers as generations increase", GH_ParamAccess.list);
            pManager.AddNumberParameter("Generations", "G", "The total number of generations", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Good Planes", "GP", "Good planes found through the GA", GH_ParamAccess.list);
        }

        ////////////////////// //Global Variables///////////////////////////////
        public static bool Run;
        public static Rectangle3d Plot;
        public static double BCR, FAR;
        public static bool Reduce;     
        public static double Search_Radius;
        
        public static Random rnd = new Random();
        int Generations = -1;
      
        //from these lists we will take the best tower and its fitness to output them
        public static List<double> fitnesses = new List<double>();      
        public static List<List<Box>> Towers = new List<List<Box>>();
        public static List<Plane> GoodPlanes = new List<Plane>();
        public static List<Point3d> pts = new List<Point3d>();

        //GA variables
       // public static Genotype a;
       // public static Phenotype b;
        Population p;       
        public static int PopulationNum =50;
       
       
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Run = false;
            Plot = new Rectangle3d();
            BCR = 0;
            FAR = 0;
            Reduce = false;        
           // int U_Count = 0;
            //int V_Count = 0;
           // double Offset = 0;
            Search_Radius = 0;
            List<Mesh> NeighborhoodList = new List<Mesh>();

            if (!DA.GetData(0, ref Run)) return;
            if (!DA.GetData(1, ref Plot)) return;
            if (!DA.GetData(2, ref BCR)) return;
            if (!DA.GetData(3, ref FAR)) return;
            if (!DA.GetData(4, ref Reduce)) return;
           // if (!DA.GetData(5, ref U_Count)) return;
           // if (!DA.GetData(6, ref V_Count)) return;
           // if (!DA.GetData(7, ref Offset)) return;
            if (!DA.GetData(5, ref Search_Radius)) return;
            if (!DA.GetDataList(6, NeighborhoodList)) return;
 

            Mesh Neighborhood = new Mesh();
            if(NeighborhoodList.Count > 0)
               Neighborhood.Append(NeighborhoodList);
           
            
            if (Run)
            {
                Generations = -1;
                Towers.Clear();
                fitnesses.Clear();
                pts.Clear();
                GoodPlanes.Clear();
                //create new Population
                p = new Population(Neighborhood);
                Run = false;
            }      
          
            p.Evolve();      
            Generations++;      
            DA.SetData(6, Generations);
            Towers.Clear();
            fitnesses.Clear();
             pts.Clear();
            GoodPlanes.Clear();

            //redraw the population
            for (int i = 0; i < p.pop.Length; i++)
            {
                // i had draw down there
                Towers.Add(p.pop[i].i_phenotype.Final_Tower);
                fitnesses.Add(p.pop[i].i_fitness);
                pts.Add(new Point3d(p.pop[i].i_phenotype.xpos, p.pop[i].i_phenotype.ypos, 0));
            
                 
            }

            //best tower
            DA.SetDataList(0, Towers[Towers.Count-1]);

            //best_fitness
            DA.SetData(1, p.pop[p.pop.Length-1].i_fitness);

            //best genotype
            List<double> BG = p.pop[p.pop.Length - 1].i_genotype.genes.ToList();
            DA.SetDataList(2, BG);

            //To see all the towers of one generation
            List<Box> Individuals = Towers.SelectMany(x => x).ToList();
            //all towers
            DA.SetDataList(3, Individuals);

            //all fitnesses
            DA.SetDataList(4,fitnesses);

            //all positions
            DA.SetDataList(5, pts);

            //good planes
            DA.SetDataList(7, GoodPlanes);


        }
        //Genotype is an array of ints representing properties of the  Tower
        //it is the DNA of the tower
        public class Genotype
        {
            public double [] genes;
        
            public Genotype( )
            {
                genes = new double [19];
                for (int i = 0; i < genes.Length; i++)
                {
                    genes[i] = rnd.NextDouble();                 
                }
            }

            //Randomly change one property(gene value) to mantain diversity in the population
            public void Mutate()
            {
                for (int i = 0; i < genes.Length; i++)
                {
                    //2% mutation rate
                    if (rnd.Next(100) < 5)
                        genes[i] = rnd.NextDouble();                   
                }
            }
        }

        //Phenotype is the representation of each individual,in our case a Circle
        public class Phenotype
        {
            //maximum dimensions and boundaries for the Tower
            public double ymax, xmax;   
            public double xb, yb, xpos, ypos, x0, y0, z0, x1, y1, z1, x2, y2, z2, x3, y3, z3, reduction_num, rotation_num, angle_upper_limit;
           
            public List<Box> Final_Tower;
            public double RayValue;

            public Phenotype(Genotype g)
            {
 
                xmax = (BCR + BCR / 2) * Plot.Width;
         
                ymax = (BCR + BCR / 2) * Plot.Height;
     
                xb = GAUtilities.Remap(g.genes[0], xmax, 1.2*xmax) ;        
                yb = GAUtilities.Remap(g.genes[1], ymax, 1.2*ymax);
                
                xpos = GAUtilities.Remap(g.genes[2],-Plot.Width/2.5, Plot.Width/2.5);
                ypos = GAUtilities.Remap(g.genes[3], -Plot.Height/2.5, Plot.Height/2.5);
              
                x0 = GAUtilities.Remap(g.genes[4],0.2,0.9);  y0 = GAUtilities.Remap(g.genes[5], 0.3, 0.9);  z0 = GAUtilities.Remap(g.genes[6], 0.3, 0.9);
               
                x1 = GAUtilities.Remap(g.genes[7], 0.2, 0.9);  y1 = GAUtilities.Remap(g.genes[8], 0.3, 0.9);  z1 = GAUtilities.Remap(g.genes[9], 0.3, 0.9);
              
                x2 = GAUtilities.Remap(g.genes[10], 0.2, 0.9);  y2 = GAUtilities.Remap(g.genes[11], 0.3, 0.9);  z2 = GAUtilities.Remap(g.genes[12], 0.3, 0.9);
               
                x3 = GAUtilities.Remap(g.genes[13], 0.2, 0.9);  y3 = GAUtilities.Remap(g.genes[14], 0.3, 0.9);  z3 = GAUtilities.Remap(g.genes[15], 0.3, 0.9);
                
                //the less voxels we keep is 9 the maximum is 13
                reduction_num = GAUtilities.Remap(g.genes[16], 9, 13);
                rotation_num =  GAUtilities.Remap(g.genes[17], 9, 13);            
                angle_upper_limit = GAUtilities.Remap(g.genes[18], 45, 345);

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //make the tower  
                //the divided tower
                List<Box> DividedTower = new List<Box>();
                DividedTower.AddRange(GAUtilities.ApplyStages(xb, yb, xpos, ypos, x0, y0, z0, x1, y1, z1, x2, y2, z2, x3, y3, z3, Plot, FAR, BCR));
                Final_Tower =  CreateTower(DividedTower,Plot, BCR, Reduce);

            }

            private List<Box> CreateTower(List<Box> DividedTower,Rectangle3d Plot, double BCR, bool Reduce)
            {
               List<Box> Final_Towerin = new List<Box>();
               
                //the tower divided and with some voxels "deleted"
                List<Box> DividedTowerR = new List<Box>();
               
                //the user can decide if they will allow deletion
                if (Reduce)
                {                  
                    //delete some voxels
                    DividedTowerR.AddRange(GAUtilities.RandomReduce(DividedTower, reduction_num));
                    Final_Towerin.AddRange(GAUtilities.Rotate(DividedTowerR, rotation_num, angle_upper_limit));
   
                }
                else
                    Final_Towerin.AddRange(GAUtilities.Rotate(DividedTower, rotation_num, angle_upper_limit));

                return Final_Towerin;

            }

            public List<Box> Draw()
            {
                
                return this.Final_Tower;
            }
            public double Evaluate( Mesh Target, double search_rad)
            {
                         
                List<Point3d> points = new List<Point3d>();
                List<Vector3d> vecs = new List<Vector3d>();
                //the method created points in all 4 faces of each voxel(not the top and bottom)
                //the useful ones are the ones that actually have some view to the exterior,not the ones inside the tower
               GAUtilities.DivideBoxFaces2(Final_Tower, out List<Point3d> allPoints, out List<Vector3d> allVectors);
 
                points.AddRange(allPoints);
                vecs.AddRange(allVectors);
               
                List<Mesh> MeshVoxels = new List<Mesh>();
                //Convert the  Tower voxels from boxes to meshes
                for (int i = 0; i < Final_Tower.Count; i++)
                {
                    Mesh[] temp = Mesh.CreateFromBrep(Final_Tower[i].ToBrep(), new MeshingParameters());
                    MeshVoxels.AddRange(temp.ToList());
                }

                //Create a mesh
                Mesh MeshTower = new Mesh();
                MeshTower.Append(MeshVoxels);

                //the filter method will check the distances between the points and will return only the ones belonging to the outer faces
                GAUtilities.Filter(points, vecs, Target, MeshTower, search_rad, out List< Line> FilteredLines,
                    out List<Point3d> FilteredPoints, out List<Vector3d> FilteredVectors);
                
                for (int i = 0; i < FilteredLines.Count; i++)
                {
                    if (FilteredLines[i].Length / Search_Radius >= 0.90)
                    {
                        GoodPlanes.Add(new Plane(FilteredPoints[i], FilteredVectors[i]));
                    }
                   
                }

                double part_fitness1 = GAUtilities.GetCaptures (FilteredPoints, FilteredVectors, 10,10, false);
    
                double part_fitness2 = GAUtilities.RaysSum(FilteredLines, Search_Radius);
                double fitness = 0.5 * part_fitness1 + 0.5 * part_fitness2;
                //RhinoApp.WriteLine("fitness from capture " + part_fitness1);
               // RhinoApp.WriteLine("fitness from  rays" + part_fitness2);
                //RhinoApp.WriteLine("_____________________________________");
                return fitness; 
            }
        }


        public class Individual : IComparable<Individual>
        {
            public Genotype i_genotype;
            public Phenotype i_phenotype;
            public double i_fitness;

            public Individual()
            {
                i_genotype = new Genotype();
                i_phenotype = new Phenotype(i_genotype);
                i_fitness = 0;
            }

           // public List<Box> Draw()
          //{
           //     return i_phenotype.Draw();
           //
           // }

            //Implement the CompareTo method looking for the biggest fitness
            public int CompareTo(Individual iToCompare)
            {
                if(iToCompare == null) return -1;
                if (iToCompare.i_fitness == i_fitness)
                {
                    return 0;
                }
                return i_fitness > iToCompare.i_fitness ? 1 : -1;
            }

            public void Evaluate(Mesh Target, double search_rad)
            {
                i_fitness = i_phenotype.Evaluate( Target, search_rad);
            }
        }


        public class Population
        {
            public Individual[] pop;
            public Mesh Neighborhood;
            public Population(Mesh Neighborhood)
            {
  
                this.Neighborhood = Neighborhood;
                pop = new Individual[PopulationNum];

                for (int i = 0; i < PopulationNum; i++)
                {
                    pop[i] = new Individual();                
                    pop[i].Evaluate(Neighborhood, Search_Radius);
                }
                //We sort the population based on the fitnsees as we defined in the Icomparable
                Array.Sort(pop);
            }

          
            public void Evolve()
            {
                Individual a, b, x,y;
               
                for (int i = 0; i < PopulationNum; i++)
                {
                    a = SelectIndividual();
                    b = SelectIndividual();
                    //make both possible offsprings from a couple
                     Breed(a, b,out Individual c, out Individual d);
                    x = c;
                    y = d;        
                    x.Evaluate(Neighborhood, Search_Radius);
                    y.Evaluate(Neighborhood, Search_Radius);

                    //evaluate the two offsprings and compare them
                    //the fittest will survive into the population
                    if (x.i_fitness >= y.i_fitness)
                       pop[0] = x;
                     else
                      pop[0] = y;
 
                    Array.Sort(pop);
                   // RhinoApp.WriteLine("Best is:" + pop[PopulationNum-1].i_fitness.ToString());
                }
               // RhinoApp.WriteLine("--------------and thats a wrap");
            }

            public Individual SelectIndividual()
            {
                int which = (int)Math.Floor(((double)PopulationNum - 1e-3) * (1.0 - Math.Pow((rnd.NextDouble()), 2)));
                return pop[which];
            }
        }

        public static void Breed(Individual a, Individual b,out Individual c, out Individual d)
        {
             c = new Individual();
             d = new Individual();   
            
            Crossover(a.i_genotype, b.i_genotype,out Genotype baby1,out Genotype baby2);
            c.i_genotype = baby1;
            d.i_genotype = baby2;

            c.i_genotype.Mutate();
            d.i_genotype.Mutate();

            c.i_phenotype = new Phenotype(c.i_genotype);
            d.i_phenotype = new Phenotype(d.i_genotype);
       
        }

        public static void Crossover(Genotype a, Genotype b,out Genotype baby1,out Genotype baby2)
        {
             baby1 = new Genotype();
             baby2 = new Genotype();

            //the offspring get hapf of their genes from one parent and half from the other
            for (int i = 0; i < 10; i++)
            {
                baby1.genes[i] = a.genes[i];
                baby2.genes[i] = b.genes[i];
            }

            for (int i = 10; i < 19; i++)
            {
                baby1.genes[i] = b.genes[i];
                baby2.genes[i] = a.genes[i];
            }
        
        }

       
        protected override System.Drawing.Bitmap Icon
        {
            get
            { 
                return Morpho.Properties.Resources.GA;
            }
        }

     
        /// Gets the unique ID for this component. Do not change this ID after release.  
        public override Guid ComponentGuid
        {
            get { return new Guid("38a8b3c7-3e41-4c77-8d71-71d1c364af7c"); }
        }
    }

    //the customAttributes class changed the color of my GA component to pink :)
    public class CustomAttributes : GH_ComponentAttributes
    {
        public CustomAttributes(IGH_Component component)
          : base(component)
        { }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel == GH_CanvasChannel.Objects)
            {
                // Cache the existing style.
                GH_PaletteStyle style = GH_Skin.palette_normal_standard;

                // Swap out palette for normal, unselected components.
                GH_Skin.palette_normal_standard = new GH_PaletteStyle(Color.DeepPink, Color.Black, Color.Black);

                base.Render(canvas, graphics, channel);

                // Put the original style back.
                GH_Skin.palette_normal_standard = style;
            }
            else
                base.Render(canvas, graphics, channel);
        }
    }
}