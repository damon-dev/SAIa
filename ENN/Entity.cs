using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvolutionalNeuralNetwork
{
    [JsonObject]
    public class Entity
    {
        [JsonProperty]
        public List<Gene> Genes { get; private set; }
        [JsonProperty]
        public double FitnessValue { get; private set; } // the closer to 0 the better

        private DataCollection dataSource;

        public Entity() { }

        public Entity(List<Gene> initialStructure, DataCollection _dataSource, double fitness = double.MaxValue)
        {
            dataSource = _dataSource;
            Genes = new List<Gene>(initialStructure);
            FitnessValue = fitness;
        }

        private double SquareSum(Cluster cluster, List<double> input, List<double> expectedOutput)
        {
            double squareSum = 0;

            var predictedOutput = cluster.Querry(input, out int steps);
            cluster.Nap();

            for (int i = 0; i < expectedOutput.Count; ++i)
                squareSum += (predictedOutput[i] - expectedOutput[i]) * (predictedOutput[i] - expectedOutput[i]);

            squareSum /= expectedOutput.Count;

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

            double meanSquareSum = 0;
            for (int i = 0; i < input.Count; ++i)
                meanSquareSum += SquareSum(cluster, input[i], expectedOutput[i]);

            meanSquareSum /= input.Count;

            if (cluster.NeuronCount == 0)
                FitnessValue = double.MaxValue;
            else
                FitnessValue = input.Count * Math.Log(meanSquareSum) +// Math.Pow(Math.Log(meanSquareSum), 1) +
                                                                      //(cluster.NeuronCount) /
                                                                      //(double)(cluster.InputSize + cluster.OutputSize);
                               cluster.NeuronCount + cluster.SynapseCount;
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

        public List<Entity> Copulate(Entity father, Mode mode, Random rand)
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

            var kids = new List<Entity>();

            switch(mode)
            {
                case Mode.Grow:
                    // similar to both X
                    kids.Add(MoreLike(motherGenes, motherUniqueStructure, fatherUniqueStructure, commonStructure, dataSource, rand));
                    // similar to both Y
                    kids.Add(MoreLike(fatherGenes, fatherUniqueStructure, motherUniqueStructure, commonStructure, dataSource, rand));
                    break;

                case Mode.Balance:
                    // similar to both X
                    kids.Add(MoreLike(motherGenes, motherUniqueStructure, fatherUniqueStructure, commonStructure, dataSource, rand));
                    // similar to both Y
                    kids.Add(MoreLike(fatherGenes, fatherUniqueStructure, motherUniqueStructure, commonStructure, dataSource, rand));
                    // more like mother
                    kids.Add(ExclusiveMoreLike(motherGenes, motherUniqueStructure, commonStructure, dataSource, rand));
                    // more like father
                    kids.Add(ExclusiveMoreLike(fatherGenes, fatherUniqueStructure, commonStructure, dataSource, rand));
                    break;

                case Mode.Shrink:
                    // more like mother
                    kids.Add(ExclusiveMoreLike(motherGenes, motherUniqueStructure, commonStructure, dataSource, rand));
                    // more like father
                    kids.Add(ExclusiveMoreLike(fatherGenes, fatherUniqueStructure, commonStructure, dataSource, rand));
                    // only similar to both
                    kids.Add(new Entity(commonStructure, dataSource));
                    break;
            }

            return kids;
        }

        private Entity ExclusiveMoreLike(List<Gene> genome, List<Gene> uniqueGenome, List<Gene> common, DataCollection data, Random rand)
        {
            var baby = new Entity(common, data);

            int i = 0;
            int b = 0;
            for (i = 0; i < genome.Count && b < baby.Genes.Count; ++i)
            {
                var X = genome[i];
                var B = baby.Genes[b];
                if (X.Source == B.Source && X.Destination == B.Destination)
                {
                    B.Strength -= CalculateOffset(B.Strength, X.Strength, rand);
                    b++;
                }
            }

            for (i = 0; i < uniqueGenome.Count; ++i)
            {
                var gene = uniqueGenome[i];
                gene.Strength -= CalculateOffset(gene.Strength, gene.Strength / 2, rand);
                baby.Genes.Add(gene);
            }

            return baby;
        }

        private Entity MoreLike(List<Gene> dominant, List<Gene> uniqueDominant, List<Gene> uniqueRecesive, List<Gene> common, DataCollection data, Random rand)
        {
            var baby = new Entity(common, data);

            int b = 0;
            for (int k = 0; k < dominant.Count && b < baby.Genes.Count; ++k)
            {
                var X = dominant[k];
                var B = baby.Genes[b];
                if (X.Source == B.Source && X.Destination == B.Destination)
                {
                    B.Strength -= CalculateOffset(B.Strength, X.Strength, rand);
                    b++;
                }
            }

            for (int i = 0; i < uniqueDominant.Count; ++i)
            {
                var gene = uniqueDominant[i];
                gene.Strength -= CalculateOffset(gene.Strength, gene.Strength / 2, rand);
                baby.Genes.Add(gene);
            }

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