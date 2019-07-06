using System;
using System.Collections.Generic;

namespace EvolutionalNeuralNetwork
{
    public class Environment
    {
        public List<double[]> Input { get; set; }
        public List<double> ExpectedOutput { get; set; }

        public List<Gene> Run(int generations, List<double[]> input, List<double> expectedOutput)
        {
            var rand = new Random();
            Input = input;
            ExpectedOutput = expectedOutput;

            // generate population
            var population = new Population(rand);
            population.GeneratePopulation(2, 1);

            for (int i = 0; i < population.Folk.Count; ++i)
            {
                population.Folk[i].EvaluateFitness(input, expectedOutput);
            }

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

                    if (child.GeneGuids.Count > 0)
                        batch[i] = child;
                }

                for (int i = 0; i < batch.Length; ++i)
                    batch[i].EvaluateFitness(input, expectedOutput);

                population.AddKids(batch);
            }

            return population.GetBest().Genes;
        }
    }
}