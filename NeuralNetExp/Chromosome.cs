using System;
using System.Collections.Generic;

namespace EvolutionalNeuralNetwork
{
    public class Chromosome
    {
        public List<Gene> Genes { get; private set; }
        public double FitnessValue { get; private set; } // the greated the better
        public DataCollection DataSource { get; private set; }

        public Chromosome(List<Gene> initialStructure, DataCollection _dataSource)
        {
            DataSource = _dataSource;
            Genes = new List<Gene>(initialStructure);
        }

        public void EvaluateFitness()
        {
            var input = DataSource.FetchInput();
            var expectedOutput = DataSource.FetchOutput();

            double meanSquareSum = 0;
            var cluster = new Cluster();

            Genes = cluster.GenerateFromStructure(Genes, true);
            
            for (int i = 0; i < input.Count; ++i)
            {
                double squareSum = 0;
                var predictedOutput = cluster.Querry(input[i]);
                cluster.GenerateFromStructure(Genes);
                for (int j = 0; j < expectedOutput[i].Count; ++j)
                    squareSum += (predictedOutput[j] - expectedOutput[i][j]) * (predictedOutput[j] - expectedOutput[i][j]);

                squareSum /= expectedOutput[i].Count;
                meanSquareSum += squareSum;
            }

            meanSquareSum /= input.Count;

            if (meanSquareSum == 0) FitnessValue = double.MaxValue;

            FitnessValue = 1 / meanSquareSum;
        }

        public static Chromosome CrossOver(Chromosome mother, Chromosome father)
        {
            Chromosome baby;
            Random rand = new Random();

            var motherGenes = new List<Gene>(mother.Genes);
            var fatherGenes = new List<Gene>(father.Genes);

            motherGenes.Sort();
            fatherGenes.Sort();

            var commonStructure = new List<Gene>();
            var motherUniqueStructure = new List<Gene>();
            var fatherUniqueStructure = new List<Gene>();

            int m = 0, f = 0;
            while(m < motherGenes.Count && f < fatherGenes.Count)
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
                gene.Strength -= CalculateOffset(gene.Strength, gene.Strength / 2); // halving them to make them closer to 0, as if the value in the other parent is 0
                motherUniqueStructure.Add(gene);
            }

            while (f < fatherGenes.Count)
            {
                var gene = fatherGenes[f++];
                gene.Strength -= CalculateOffset(gene.Strength, gene.Strength / 2);
                fatherUniqueStructure.Add(gene);
            }

            switch (rand.Next(0, 3))
            {
                case 0: // more like mother
                    baby = new Chromosome(commonStructure, mother.DataSource);

                    int b = 0;
                    for(m = 0; m < motherGenes.Count && b < baby.Genes.Count; ++m)
                    {
                        var X = motherGenes[m];
                        var B = baby.Genes[b];
                        if (X.Source == B.Source && X.Destination == B.Destination)
                        {
                            B.Strength -= CalculateOffset(B.Strength, X.Strength);
                            b++;
                        }
                    }

                    baby.Genes.AddRange(motherUniqueStructure);

                    return baby;

                case 1: // similar to both
                    baby = new Chromosome(commonStructure, mother.DataSource);

                    baby.Genes.AddRange(motherUniqueStructure);
                    baby.Genes.AddRange(fatherUniqueStructure);

                    return baby;

                case 2: // more like father
                    baby = new Chromosome(commonStructure, father.DataSource);

                    b = 0;
                    for (f = 0; f < fatherGenes.Count && b < baby.Genes.Count; ++f)
                    {
                        var Y = fatherGenes[f];
                        var B = baby.Genes[b];
                        if (Y.Source == B.Source && Y.Destination == B.Destination)
                        {
                            B.Strength -= CalculateOffset(B.Strength, Y.Strength);
                            b++;
                        }
                    }

                    baby.Genes.AddRange(fatherUniqueStructure);

                    return baby;

                default: return null; // should never happen
            }
        }
        
        // how much needs to be substracted from x to get randomly closer to target
        private static double CalculateOffset(double x, double target)
        {
            int precision = 1000000;
            double diff = x - target;
            int sign = (diff < 0) ? -1 : 1;

            int range = (int)(sign * diff * precision);

            int offset = new Random().Next(range);

            double result = sign * (double)offset / precision;

            return result;
        }
    }
}