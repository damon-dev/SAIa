using System;
using System.Collections.Generic;

namespace NeuralNetExp
{
    public class GeneticAlgorithm
    {
        public List<Gene> Run(int generations)
        {
            var rand = new Random();

            // generate population
            var population = new Population(rand);
            population.GeneratePopulation(1, 1);

            int kidPerPair = 3;
            int batchSize = 3 * kidPerPair;
            var batch = new Chromosome[batchSize];

            for (int k = 0; k < generations; ++k)
            {
                Chromosome parent1 = null;
                Chromosome parent2 = null;

                for (int i = 0; i < batch.Length; ++i)
                {
                    // torunament
                    if (i % kidPerPair == 0)
                    {
                        parent1 = population.Tournament(null);
                        parent2 = population.Tournament(parent1);
                    }

                    // crossover
                    var child = Chromosome.CrossOver(parent1, parent2, i % kidPerPair, rand);

                    // mutation
                    child.Mutate();

                    child.Generation = Math.Max(parent1.Generation, parent2.Generation) + 1;

                    batch[i] = child;
                }

                for (int i = 0; i < batch.Length; ++i)
                    batch[i].EvaluateFitness();

                population.AddKids(batch);
            }

            return population.GetBest().Genes;
        }
    }

    class Population
    {
        public int TournamentSize { get; set; } = 5;

        public List<Chromosome> Folk { get; set; }
        public int Size { get; set; } = 100;
        private Random rand;

        public Population(Random _rand)
        {
            Folk = new List<Chromosome>();
            rand = _rand;
        }

        public void GeneratePopulation(int inputSize, int outputSize)
        {
            var array = new Chromosome[Size];
            var initialStructure = new List<Gene>();
            var inputGuids = new List<Guid>();
            var outputGuids = new List<Guid>();

            // Creating GUIDS for input neurons
            for (int i = 0; i < inputSize; ++i)
                inputGuids.Add(Guid.NewGuid());

            // Creating GUIDS for output neurons
            for (int i = 0; i < outputSize; ++i)
                outputGuids.Add(Guid.NewGuid());

            // Linking input neurons
            foreach(var iGuid in inputGuids)
            {
                initialStructure.Add((Cluster.InputGuid, iGuid, 1)); // Initialising input dendrites with 1 to facilitate creation of bias nodes

                foreach (var oGuid in outputGuids)
                    initialStructure.Add((iGuid, oGuid, rand.NextDouble() * 2 - 1)); // random value between -1 and 1
            }

            // Linking output neurons
            foreach(var oGuid in outputGuids)
            {
                initialStructure.Add((oGuid, Cluster.OutputGuid, 0));
            }

            inputGuids.AddRange(outputGuids); // merging the guids to pass them to chromosomes

            for (int i = 0; i < Size; ++i)
            {
                array[i] = new Chromosome(rand, inputGuids, initialStructure)
                {
                    Generation = 1
                };
            }

            for (int i = 0; i < Size; ++i)
            {
                array[i].EvaluateFitness();
            }

            for (int i = 0; i < Size; ++i)
                Folk.Add(array[i]);
        }

        public Chromosome Tournament(Chromosome mate)
        {
            Chromosome bestMate = null;
            double bestFitnes = double.MinValue;

            for (int i = 0; i < TournamentSize; ++i)
            {
                int index = rand.Next(0, Folk.Count);
                if (Folk[index].FitnessValue > bestFitnes && !Folk[index].Equals(mate))
                {
                    bestMate = Folk[index];
                    bestFitnes = bestMate.FitnessValue;
                }
            }

            return bestMate;
        }

        public void AddKids(Chromosome[] kids)
        {
            Folk.Sort((a, b) => { return a.FitnessValue.CompareTo(b.FitnessValue); });

            for (int i = 0; i < kids.Length; ++i)
                Folk[i] = kids[i];
        }

        public Chromosome GetBest()
        {
            Folk.Sort((a, b) => { return a.FitnessValue.CompareTo(b.FitnessValue); });
            return Folk[Folk.Count - 1];
        }
    }

    class Chromosome
    {
        public double MutationRate { get; set; } = 0.015;

        private Random rand;
        public List<Gene> Genes { get; private set; }
        public List<Guid> GeneGuids { get; private set; }
        public int Generation { get; set; } = 0;
        public double FitnessValue { get; private set; }

        public Chromosome(Random _rand, List<Guid> initialGuids, List<Gene> initialStructure)
        {
            rand = _rand;

            GeneGuids = new List<Guid>(initialGuids);
            Genes = new List<Gene>(initialStructure);
        }

        public void EvaluateFitness()
        {
            // TODO
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
                        Genes.RemoveAt(i);
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

                int candidateIndex = rand.Next(0, candidates.Count);

                Genes.Add((GeneGuids[index], candidates[candidateIndex], rand.NextDouble() * 2 - 1)); // random value between -1 and 1
            }
        }

        private bool ShouldMutate()
        {
            double chance = (double)rand.Next(0, 1000) / 1000;
            return chance < MutationRate;
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
                    for(m = 0; m < mother.Genes.Count; ++m)
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

                case 3: // more like father
                    baby = new Chromosome(rand, father.GeneGuids, commonStructure);

                    b = 0;
                    for (f = 0; f < father.Genes.Count; ++f)
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