using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

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
              "The GA generates and optimizes a voxelized tower for obstraction,view or both.",
              "Morpho", "Optimization")
        {
        }
       
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "RN", "Add a button to Run,press once to create new population.", GH_ParamAccess.item,false);
            pManager.AddRectangleParameter("Plot", "P", "The boundary rectangle of the building plot.", GH_ParamAccess.item);
            pManager.AddNumberParameter("BCR", "BCR", "building Coverage Ratio.", GH_ParamAccess.item,0.5);
            pManager.AddNumberParameter("FAR", "FAR", "Floor Aspect Ratio.", GH_ParamAccess.item,600);
            pManager.AddBooleanParameter("Reduce", "RD", "If true random voxels will be deleted from the final tower.", GH_ParamAccess.item,false);          
            pManager.AddNumberParameter("Search Radius", "SR", "The GA will search for other buildings inside the SR. ", GH_ParamAccess.item, 200);
            pManager.AddMeshParameter("Neighborhood", "N", "The buildings around the tower. ", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Population_Number", "PN", "The number of individuals in the population.", GH_ParamAccess.item,50);
            pManager.AddIntegerParameter("Objectives", "OB", "OB=0 optimize obstraction,0B=1 optimize view,OB=2 optimize both", GH_ParamAccess.item,0);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBoxParameter("Best Tower", "BT", "The best tower in current generation.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Best Fitness", "BF", "The best fitness in current generation.", GH_ParamAccess.item);
            pManager.AddBoxParameter("All Towers", "AT", "All the towers in current generation.", GH_ParamAccess.list);
            pManager.AddNumberParameter("All Fitnesses", "AF", "The best fitness in current generation.", GH_ParamAccess.list);
            pManager.AddPointParameter("Positions", "PT", "Follow the positions of the towers as generations increase.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Generations", "GN", "The total number of generations.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Good Planes", "GP", "Good planes found through the GA.", GH_ParamAccess.list);
            
        }

        ////////////////////// //Global Variables///////////////////////////////
        public static bool Run;
        public static Rectangle3d Plot;
        public static double BCR, FAR;
        public static bool Reduce;     
        public static double Search_Radius;
        public static int Objectives;
        
        
        public static Random rnd = new Random();
        int Generations = -1;
      
        //from these lists we will take the best tower and its fitness to output them
        public static List<double> fitnesses = new List<double>();      
        public static List<List<Box>> Towers = new List<List<Box>>();
        public static List<Plane> GoodPlanes = new List<Plane>();
        public static List<Point3d> pts = new List<Point3d>();

        //GA variables  
        Population p;       
        public static int Population_Number =50;
            
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Run = false;
            Plot = new Rectangle3d();
            BCR = 0;
            FAR = 0;
            Reduce = false;            
            Search_Radius = 0;
            List<Mesh> NeighborhoodList = new List<Mesh>();
            
            Population_Number = 50;
            Objectives = 0;

            if (!DA.GetData(0, ref Run)) return;
            if (!DA.GetData(1, ref Plot)) return;
            if (!DA.GetData(2, ref BCR)) return;
            if (!DA.GetData(3, ref FAR)) return;
            if (!DA.GetData(4, ref Reduce)) return;    
            if (!DA.GetData(5, ref Search_Radius)) return;
            if (!DA.GetDataList(6, NeighborhoodList)) return;
            if (!DA.GetData(7,  ref Population_Number)) return;
            if (!DA.GetData(8, ref Objectives)) return;

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
               
            }      
          
            p.Evolve();      
            Generations++;   
            //output to see for how many generations the GA ran
            DA.SetData(5, Generations);
            Towers.Clear();
            fitnesses.Clear();
            pts.Clear();
            //GoodPlanes.Clear();

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
           // List<double> BG = p.pop[p.pop.Length - 1].i_genotype.genes.ToList();
           // DA.SetDataList(2, BG);

            //To see all the towers of one generation
            List<Box> Individuals = Towers.SelectMany(x => x).ToList();
            //all towers
            DA.SetDataList(2, Individuals);

            //all fitnesses
            DA.SetDataList(3,fitnesses);

            //all positions
            DA.SetDataList(4, pts);
          
            //good planes
            DA.SetDataList(6, GoodPlanes);
            Rhino.RhinoApp.WriteLine(GoodPlanes.Count.ToString());


             

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
     
                //xb and xy define the width and height of the tower
                xb = GAUtilities.Remap(g.genes[0], xmax, 1.2*xmax) ;        
                yb = GAUtilities.Remap(g.genes[1], ymax, 1.2*ymax);
                
                //xpos and xy define the position of the tower inside the plot
                xpos = GAUtilities.Remap(g.genes[2],-Plot.Width/2.5, Plot.Width/2.5);
                ypos = GAUtilities.Remap(g.genes[3], -Plot.Height/2.5, Plot.Height/2.5);
              
                //x0,y0 ad z0 define the first point inside the boundary box of the tower that we will cut first(and so on)
                x0 = GAUtilities.Remap(g.genes[4]);  y0 = GAUtilities.Remap(g.genes[5]);  z0 = GAUtilities.Remap(g.genes[6]);
               
                x1 = GAUtilities.Remap(g.genes[7]);  y1 = GAUtilities.Remap(g.genes[8]);  z1 = GAUtilities.Remap(g.genes[9]);
              
                x2 = GAUtilities.Remap(g.genes[10]);  y2 = GAUtilities.Remap(g.genes[11]);  z2 = GAUtilities.Remap(g.genes[12]);
               
                x3 = GAUtilities.Remap(g.genes[13]);  y3 = GAUtilities.Remap(g.genes[14]);  z3 = GAUtilities.Remap(g.genes[15]);
                
                //the mininmum voxels we keep is 9, the maximum is 13
                reduction_num = GAUtilities.Remap(g.genes[16], 9, 13);
               
                //the mininmum voxels we rotate is, 9 the maximum is 13
                rotation_num =  GAUtilities.Remap(g.genes[17], 9, 13);   
                
                //the minimum rotation angle is 45, the maximum is 345
                angle_upper_limit = GAUtilities.Remap(g.genes[18], 45, 345);

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                
                //the final tower is the representation of each individual
                List<Box> DividedTower = new List<Box>();
                DividedTower.AddRange(GAUtilities.ApplyStages(xb, yb, xpos, ypos, x0, y0, z0, x1, y1, z1, x2, y2, z2, x3, y3, z3, Plot, FAR));
                Final_Tower =  CreateTower(DividedTower,Reduce);

            }

            private List<Box> CreateTower(List<Box> DividedTower, bool Reduce)
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
            public double Evaluate(Mesh Target, double search_rad)
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
                GAUtilities.Filter(points, vecs, Target, MeshTower, search_rad, out List<Line> FilteredLines,
                    out List<Point3d> FilteredPoints, out List<Vector3d> FilteredVectors);

                //good planes from all the towers can give us an idea about the spots in the plot that have good views
                for (int i = 0; i < FilteredLines.Count; i++)
                {
                    if (FilteredLines[i].Length / Search_Radius > 0.90)
                    {
                        GoodPlanes.Add(new Plane(FilteredPoints[i], FilteredVectors[i]));
                    }

                }
 
                double fitness;
                double fitness1;
                double fitness2;
                if (Objectives == 0)
                {
                    //how unobstracted the views are from the tower?does it have buildings very close to it?
                    //optmize for obstraction
                    fitness = GAUtilities.RaysSum(FilteredLines, Search_Radius);
                }
                else if (Objectives == 1)
                {
                    //how much "red"(good views,landmarks,a mountain view etc) does the tower "sees"?
                    //optimize for views
                    fitness = GAUtilities.GetCaptures(FilteredPoints, FilteredVectors, 10, 10, false);
                }
                else
                {
                    //the final fitness is from 0 to 100 
                    //the final fitness tries to compromise both objectives that are considered equally important
                    fitness1=GAUtilities.RaysSum(FilteredLines, Search_Radius);
                    fitness2 = GAUtilities.GetCaptures(FilteredPoints, FilteredVectors, 10, 10, false);
                    fitness = 0.5 *fitness1 + 0.5 *fitness2;
                }
  
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
                pop = new Individual[Population_Number];

                for (int i = 0; i < Population_Number; i++)
                {
                    pop[i] = new Individual();                
                    pop[i].Evaluate(Neighborhood, Search_Radius);
                }
                //We sort the population based on the fitnesees as we defined in the Icomparable
                Array.Sort(pop);
            }

          
            public void Evolve()
            {
                Individual a, b, x,y;
               
                for (int i = 0; i < Population_Number; i++)
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
                
                }
               
            }

            public Individual SelectIndividual()
            {
                int which = (int)Math.Floor(((double)Population_Number - 1e-3) * (1.0 - Math.Pow((rnd.NextDouble()), 2)));
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

            //the offspring get half of their genes from one parent and half from the other
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