using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EvolutionalNeuralNetwork
{
    [JsonObject]
    public class Entity
    {
        [JsonProperty]
        public List<Gene> Genes { get; private set; }
        [JsonProperty]
        public double FitnessValue { get; private set; } // the greated the better

        private DataCollection dataSource;

        public Entity() { }

        public Entity(List<Gene> initialStructure, DataCollection _dataSource, double fitness = 0)
        {
            dataSource = _dataSource;
            Genes = new List<Gene>(initialStructure);
            FitnessValue = fitness;
        }

        public void EvaluateFitness(Random rand)
        {
            var cluster = new Cluster(rand);
            dataSource.FetchTrainingData(out List<List<double>> input, out List<List<double>> expectedOutput, 10);

            double meanSquareSum = 0;
            double totalTime = 0;

            Genes = cluster.GenerateFromStructure(Genes, true);
            
            for (int i = 0; i < input.Count; ++i)
            {
                double squareSum = 0;

                var predictedOutput = cluster.Querry(input[i], out TimeSpan time);
                cluster.Nap();

                for (int j = 0; j < expectedOutput[i].Count; ++j)
                    squareSum += (predictedOutput[j] - expectedOutput[i][j]) * (predictedOutput[j] - expectedOutput[i][j]);

                squareSum /= expectedOutput[i].Count;
                meanSquareSum += squareSum;
                totalTime += time.TotalSeconds;
            }

            meanSquareSum /= input.Count;
            totalTime /= input.Count;

            if (meanSquareSum == 0) FitnessValue = double.PositiveInfinity;
            else FitnessValue = 1 / (meanSquareSum * meanSquareSum + totalTime);
        }

        public Entity CrossOver(Entity father, Random rand)
        {
            Entity mother = this;
            Entity baby;

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
                        m++; // mother is smaller so we increase mother index
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
                //gene.Strength -= CalculateOffset(gene.Strength, gene.Strength / 2, rand); // halving them to make them closer to 0, as if the value in the other parent is 0
                motherUniqueStructure.Add(gene);
            }

            while (f < fatherGenes.Count)
            {
                var gene = fatherGenes[f++];
                //gene.Strength -= CalculateOffset(gene.Strength, gene.Strength / 2, rand);
                fatherUniqueStructure.Add(gene);
            }

            switch (rand.Next(0, 4))
            {
                case 0: // more like mother
                    baby = new Entity(commonStructure, mother.dataSource);

                    int b = 0;
                    for(m = 0; m < motherGenes.Count && b < baby.Genes.Count; ++m)
                    {
                        var X = motherGenes[m];
                        var B = baby.Genes[b];
                        if (X.Source == B.Source && X.Destination == B.Destination)
                        {
                            B.Strength -= CalculateOffset(B.Strength, X.Strength, rand);
                            b++;
                        }
                    }

                    for (int i = 0; i < motherUniqueStructure.Count; ++i)
                    {
                        var gene = motherUniqueStructure[i];
                        gene.Strength -= CalculateOffset(gene.Strength, gene.Strength / 2, rand);
                        baby.Genes.Add(gene);
                    }

                    for (int i = 0; i < fatherUniqueStructure.Count; ++i)
                    {
                        var gene = fatherUniqueStructure[i];
                        gene.Strength /= 2;
                        gene.Strength -= CalculateOffset(gene.Strength, 0, rand);
                        baby.Genes.Add(gene);
                    }

                    return baby;

                case 1: // similar to both
                    baby = new Entity(commonStructure, mother.dataSource);

                    for (int i = 0; i < motherUniqueStructure.Count; ++i)
                    {
                        var gene = motherUniqueStructure[i]; 
                        gene.Strength /= 2;
                        gene.Strength -= CalculateOffset(gene.Strength, 0, rand);
                        baby.Genes.Add(gene);
                    }

                    for (int i = 0; i < fatherUniqueStructure.Count; ++i)
                    {
                        var gene = fatherUniqueStructure[i];
                        gene.Strength /= 2;
                        gene.Strength -= CalculateOffset(gene.Strength, 0, rand);
                        baby.Genes.Add(gene);
                    }

                    return baby;

                case 2: // more like father
                    baby = new Entity(commonStructure, father.dataSource);

                    b = 0;
                    for (f = 0; f < fatherGenes.Count && b < baby.Genes.Count; ++f)
                    {
                        var Y = fatherGenes[f];
                        var B = baby.Genes[b];
                        if (Y.Source == B.Source && Y.Destination == B.Destination)
                        {
                            B.Strength -= CalculateOffset(B.Strength, Y.Strength, rand);
                            b++;
                        }
                    }

                    for (int i = 0; i < motherUniqueStructure.Count; ++i)
                    {
                        var gene = motherUniqueStructure[i];
                        gene.Strength /= 2;
                        gene.Strength -= CalculateOffset(gene.Strength, 0, rand);
                        baby.Genes.Add(gene);
                    }

                    for (int i = 0; i < fatherUniqueStructure.Count; ++i)
                    {
                        var gene = fatherUniqueStructure[i];
                        gene.Strength -= CalculateOffset(gene.Strength, gene.Strength / 2, rand);
                        baby.Genes.Add(gene);
                    }

                    return baby;

                case 3: // only similar to both
                    baby = new Entity(commonStructure, mother.dataSource);

                    return baby;

                default: return null; // should never happen
            }
        }
        
        // how much needs to be substracted from x to get randomly closer to target
        private double CalculateOffset(double x, double target, Random rand)
        {
            return rand.NextDouble() * (x - target);
        }
    }
}