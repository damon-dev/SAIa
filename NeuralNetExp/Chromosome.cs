using System;
using System.Collections.Generic;

namespace EvolutionalNeuralNetwork
{
    public class Chromosome
    {
        public double MutationRate { get; set; } = 0.015;

        private Random rand;
        public List<Gene> Genes { get; private set; }
        public List<Guid> GeneGuids { get; private set; }
        public int Generation { get; set; } = 0;
        public double FitnessValue { get; private set; } // the greated the better

        public Chromosome(Random _rand, List<Guid> initialGuids, List<Gene> initialStructure)
        {
            rand = _rand;

            GeneGuids = new List<Guid>(initialGuids);
            Genes = new List<Gene>(initialStructure);
        }

        public void EvaluateFitness(List<double[]> input, List<double> expectedOutput)
        {
            double squareSum = 0;
            var cluster = new Cluster(Genes);

            for (int i = 0; i < input.Count; ++i)
            {
                double predictedOutput = cluster.Querry(new List<double> { input[i][0], input[i][1] });
                squareSum = (predictedOutput - expectedOutput[i]) * (predictedOutput - expectedOutput[i]);
            }

            if (squareSum == 0) FitnessValue = double.PositiveInfinity;
            else FitnessValue = input.Count / squareSum;
        }

        public void Mutate()
        {
            // mutation for synapse destruction
            if (ShouldMutate())
            {
                int index = rand.Next(0, Genes.Count);
                Genes.RemoveAt(index);
            }

            // mutation for neuron destruction
            if (ShouldMutate())
            {
                int index = rand.Next(0, GeneGuids.Count);

                int i = 0;
                while (i < Genes.Count)
                {
                    if (Genes[i].Source == GeneGuids[index] || Genes[i].Destination == GeneGuids[index])
                    {
                        if (Genes.Count > 0)
                            Genes.RemoveAt(i);
                    }
                    else
                        i++;
                }

                GeneGuids.RemoveAt(index);
            }

            // mutation for synapse value change
            if (ShouldMutate())
            {
                int index = rand.Next(0, Genes.Count);

                Genes[index].Strength = rand.NextDouble() * 2 - 1; // random value between -1 and 1
            }

            // mutation for neuron creation
            if (ShouldMutate())
            {
                int index = rand.Next(0, GeneGuids.Count);

                var newGuid = Guid.NewGuid();
                GeneGuids.Add(newGuid);

                int count = Genes.Count;
                for(int i = 0; i < count; ++i)
                {
                    if (GeneGuids[index] == Genes[i].Source)
                    {
                        Genes.Add((newGuid, Genes[i].Destination, Genes[i].Strength / 2));
                        Genes[i].Strength /= 2; // dividing destination strength by 2 to not make new neuron insertion too disruptive
                    }
                    else if(GeneGuids[index] == Genes[i].Destination)
                    {
                        Genes.Add((Genes[i].Source, newGuid, Genes[i].Strength)); // source strength should not be divided
                    }
                }
            }

            // mutation for synapse creation
            if (ShouldMutate())
            {
                int index = rand.Next(0, GeneGuids.Count);
                var neighboors = new HashSet<Guid>();
                var candidates = new List<Guid>();
                foreach(var gene in Genes)
                {
                    if (gene.Source == GeneGuids[index])
                        neighboors.Add(gene.Destination);
                }

                foreach(var gene in GeneGuids)
                {
                    if (!neighboors.Contains(gene))
                    {
                        candidates.Add(gene);
                    }
                }

                if (candidates.Count > 0)
                {
                    int candidateIndex = rand.Next(0, candidates.Count);

                    Genes.Add((GeneGuids[index], candidates[candidateIndex], rand.NextDouble() * 2 - 1)); // random value between -1 and 1
                }
            }
        }

        private bool ShouldMutate()
        {
            double chance = (double)rand.Next(0, 1000) / 1000;
            return chance < MutationRate && GeneGuids.Count > 0 && Genes.Count > 0;
        }

        public static Chromosome CrossOver(Chromosome mother, Chromosome father, int similarityType, Random rand)
        {
            Chromosome baby;

            mother.Genes.Sort();
            father.Genes.Sort();

            var commonStructure = new List<Gene>();
            var motherUniqueStructure = new List<Gene>();
            var fatherUniqueStructure = new List<Gene>();

            int m = 0, f = 0;
            while(m < mother.Genes.Count && f < father.Genes.Count)
            {
                var X = mother.Genes[m];
                var Y = father.Genes[f];
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

            while (m < mother.Genes.Count)
                motherUniqueStructure.Add(mother.Genes[m++]);

            while (f < father.Genes.Count)
                fatherUniqueStructure.Add(father.Genes[f++]);

            switch(similarityType)
            {
                case 0: // more like mother
                    baby = new Chromosome(rand, mother.GeneGuids, commonStructure);

                    int b = 0;
                    for(m = 0; m < mother.Genes.Count && b < baby.Genes.Count; ++m)
                    {
                        var X = mother.Genes[m];
                        var B = baby.Genes[b];
                        if (X.Source == B.Source && X.Destination == B.Destination)
                        {
                            B.Strength -= CalculateOffset(B.Strength, X.Strength, rand);
                            b++;
                        }
                    }

                    baby.Genes.AddRange(motherUniqueStructure);

                    return baby;

                case 1: // similar to both
                    baby = new Chromosome(rand, mother.GeneGuids, commonStructure);

                    baby.Genes.AddRange(motherUniqueStructure);
                    baby.Genes.AddRange(fatherUniqueStructure);

                    baby.GeneGuids.AddRange(father.GeneGuids);

                    // removing duplicates
                    for (int i = 0; i < baby.GeneGuids.Count; ++i)
                        for (int j = i + 1; j < baby.GeneGuids.Count; ++j)
                            if (baby.GeneGuids[i] == baby.GeneGuids[j])
                                baby.GeneGuids.RemoveAt(j--);

                    return baby;

                case 2: // more like father
                    baby = new Chromosome(rand, father.GeneGuids, commonStructure);

                    b = 0;
                    for (f = 0; f < father.Genes.Count && b < baby.Genes.Count; ++f)
                    {
                        var Y = father.Genes[f];
                        var B = baby.Genes[b];
                        if (Y.Source == B.Source && Y.Destination == B.Destination)
                        {
                            B.Strength -= CalculateOffset(B.Strength, Y.Strength, rand);
                            b++;
                        }
                    }

                    baby.Genes.AddRange(fatherUniqueStructure);

                    return baby;

                default: return null; // should never happen
            }
        }
        
        // how much needs to be substracted from x to get rancomly closer to target
        private static double CalculateOffset(double x, double target, Random rand)
        {
            int precision = 1000000;
            double diff = x - target;
            int sign = (diff < 0) ? -1 : 1;

            int range = (int)(sign * diff * precision);

            int offset = rand.Next(range);

            double result = sign * (double)offset / precision;

            return result;
        }
    }
}