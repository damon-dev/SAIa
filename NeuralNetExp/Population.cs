using System;
using System.Collections.Generic;

namespace EvolutionalNeuralNetwork
{
    public class Population
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
}