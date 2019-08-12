using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    [JsonObject]
    public class Entity
    {
        [JsonProperty]
        public List<Gene> Genes { get; private set; }
        [JsonProperty]
        public double FitnessValue { get; private set; } // the smaller the better

        private Data dataSource;

        public Entity() { }

        public Entity(List<Gene> initialStructure, Data _dataSource, double fitness = double.PositiveInfinity)
        {
            dataSource = _dataSource;
            Genes = new List<Gene>(initialStructure);
            FitnessValue = fitness;
        }

        private double SquareSum(Cluster cluster, List<double> input, List<double> expectedOutput)
        {
            double squareSum = 0;

            var predictedOutput = cluster.Querry(input, out long steps);
            cluster.Nap();

            if (steps == -1)
                squareSum = double.PositiveInfinity;
            else
            {
                for (int i = 0; i < expectedOutput.Count; ++i)
                    squareSum += (predictedOutput[i] - expectedOutput[i]) * (predictedOutput[i] - expectedOutput[i]);

                squareSum /= expectedOutput.Count;
            }

            return squareSum;
        }

        public void EvaluateFitness()
        {
            EvaluateFitness(Mode.Balance, 0, new Random());
        }

        public void EvaluateFitness(Mode mode, double mutationRate, Random rand)
        {
            var cluster = new Cluster(rand);
            dataSource.FetchTrainingData(out List<List<double>> input, out List<List<double>> expectedOutput, 100, false);

            Genes = cluster.GenerateFromStructure(Genes, mode, mutationRate);

            if (cluster.NeuronCount == 0)
                FitnessValue = double.PositiveInfinity;
            else
            {
                double[] meanSquareSums = new double[input.Count];
                double mean = 0;

                //Parallel.For(0, input.Count, (i) =>
                for (int i = 0; i < input.Count; ++i)
                {
                    //var cl = new Cluster(new Random());
                    //cl.GenerateFromStructure(Genes);
                    meanSquareSums[i] = SquareSum(cluster, input[i], expectedOutput[i]);
                }//);

                for (int i = 0; i < input.Count; ++i)
                    mean += meanSquareSums[i];

                mean /= input.Count;

                FitnessValue = input.Count * Math.Pow(Math.Log(mean), 3) +
                               cluster.NeuronCount + cluster.SynapseCount;
            }
        }

        public bool Compatible(Entity mate)
        {
            if (mate.Genes.Count != Genes.Count)
                return true;

            int compatibility = 0;

            foreach(var x in Genes)
            {
                foreach(var y in mate.Genes)
                {
                    if (x.Equals(y))
                    {
                        compatibility++;
                        break;
                    }
                }
            }

            if (compatibility == Genes.Count)
                return false;

            return true;
        }

        public Entity Copulate(Entity father, Mode mode, Random rand)
        {
            Entity mother = this;

            var motherGenes = new List<Gene>(mother.Genes);
            var fatherGenes = new List<Gene>(father.Genes);

            motherGenes.Sort();
            fatherGenes.Sort();

            var commonStructure = new List<Gene>();
            var motherUniqueStructure = new List<Gene>();
            var fatherUniqueStructure = new List<Gene>();

            int m = 0, f = 0;
            while (m < motherGenes.Count && f < fatherGenes.Count)
            {
                var X = motherGenes[m];
                var Y = fatherGenes[f];
                if (X.Source == Y.Source)
                {
                    if (X.Destination == Y.Destination)
                    {
                        commonStructure.Add((X.Source, Y.Destination,
                            (X.Strength + Y.Strength) / 2));

                        m++;
                        f++;
                    }
                    else if (X.Destination.CompareTo(Y.Destination) < 0)
                    {
                        motherUniqueStructure.Add(X);
                        m++;
                    }
                    else
                    {
                        fatherUniqueStructure.Add(Y);
                        f++;
                    }
                }
                else if (X.Source.CompareTo(Y.Source) < 0)
                {

                    motherUniqueStructure.Add(X);
                    m++;
                }
                else
                {
                    fatherUniqueStructure.Add(Y);
                    f++;
                }
            }

            while (m < motherGenes.Count)
            {
                var gene = motherGenes[m++];
                motherUniqueStructure.Add(gene);
            }

            while (f < fatherGenes.Count)
            {
                var gene = fatherGenes[f++];
                fatherUniqueStructure.Add(gene);
            }

            // father is the weaker one
            switch (mode)
            {
                case Mode.Grow:
                    // more like mother
                    return MoreLike(motherGenes, motherUniqueStructure, fatherUniqueStructure, commonStructure, dataSource, rand);

                case Mode.Balance:
                    // exclusive more like mother
                    return ExclusiveMoreLike(motherGenes, motherUniqueStructure, commonStructure, dataSource, rand);

                case Mode.Shrink:
                    // absolute more like mother
                    return AbsoluteMoreLike(motherGenes, motherUniqueStructure, commonStructure, dataSource, rand);

                default: return null;
            }
        }

        private Entity AbsoluteMoreLike(List<Gene> dominant, List<Gene> uniqueDominant, List<Gene> common, Data data, Random rand)
        {
            var baby = new Entity(common, data);

            int i = 0;
            int b = 0;
            for (i = 0; i < dominant.Count && b < baby.Genes.Count; ++i)
            {
                var X = dominant[i];
                var B = baby.Genes[b];
                if (X.Source == B.Source && X.Destination == B.Destination)
                {
                    B.Strength -= CalculateOffset(B.Strength, X.Strength, rand);
                    b++;
                }
            }

            return baby;
        }

        private Entity ExclusiveMoreLike(List<Gene> dominant, List<Gene> uniqueDominant, List<Gene> common, Data data, Random rand)
        {
            var baby = AbsoluteMoreLike(dominant, uniqueDominant, common, data, rand);

            for (int i = 0; i < uniqueDominant.Count; ++i)
            {
                var gene = uniqueDominant[i];
                gene.Strength -= CalculateOffset(gene.Strength, gene.Strength / 2, rand);
                baby.Genes.Add(gene);
            }

            return baby;
        }

        private Entity MoreLike(List<Gene> dominant, List<Gene> uniqueDominant, List<Gene> uniqueRecesive, List<Gene> common, Data data, Random rand)
        {
            var baby = ExclusiveMoreLike(dominant, uniqueDominant, common, data, rand);

            for (int i = 0; i < uniqueRecesive.Count; ++i)
            {
                var gene = uniqueRecesive[i];
                gene.Strength /= 2;
                gene.Strength -= CalculateOffset(gene.Strength, 0, rand);
                baby.Genes.Add(gene);
            }

            return baby;
        }
        
        // how much needs to be substracted from x to get randomly closer to target
        private double CalculateOffset(double x, double target, Random rand)
        {
            return rand.NextDouble() * (x - target);
        }
    }
}