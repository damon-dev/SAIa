using System;
using System.Collections.Generic;

namespace EvolutionalNeuralNetwork
{
    public class Population
    {
        public List<Entity> Members { get; set; }
        public int Size { get; set; } = 100;
        public int TournamentSize { get; set; } = 10; // 10% of total data size

        private Random rand;

        public Population(int inputSize, int outputSize, DataCollection data)
        {
            Members = new List<Entity>();
            rand = new Random();

            GenerateMembers(inputSize, outputSize, data);
        }

        public int Tournament(Entity mate)
        {
            int position = 0;
            Entity bestMate = null;
            double bestFitnes = double.MinValue;

            for (int i = 0; i < TournamentSize; ++i)
            {
                int index = rand.Next(0, Members.Count);
                if (Members[index].FitnessValue > bestFitnes && !Members[index].Equals(mate))
                {
                    bestMate = Members[index];
                    bestFitnes = bestMate.FitnessValue;
                    position = index;
                }
            }

            return position;
        }

        public int Victim()
        {
            int position = 0;
            Entity weakestPrey = null;
            double worstFitness = double.MaxValue;

            for (int i = 0; i < TournamentSize; ++i)
            {
                int index = rand.Next(1, Members.Count);
                if (Members[index].FitnessValue < worstFitness)
                {
                    weakestPrey = Members[index];
                    worstFitness = weakestPrey.FitnessValue;
                    position = index;
                }
            }

            return position;
        }

        private void GenerateMembers(int inputSize, int outputSize, DataCollection data)
        {
            var initialStructure = new List<Gene>();
            var inputGuids = new List<Guid>();
            var outputGuids = new List<Guid>();

            // Creating GUIDS for input neurons
            for (int i = 0; i < inputSize; ++i)
                inputGuids.Add(Guid.NewGuid());

            // Creating GUIDS for output neurons
            for (int i = 0; i < outputSize; ++i)
                outputGuids.Add(Guid.NewGuid());

            // Linking input neurons to the refference node
            foreach (var iGuid in inputGuids)
            {
                initialStructure.Add((Cluster.InputGuid, iGuid, 0));
                initialStructure.Add((Cluster.RecoveryMark, iGuid, 1));
            }

            // Linking output neurons to the refference node
            foreach (var oGuid in outputGuids)
            {
                initialStructure.Add((oGuid, Cluster.OutputGuid, 0));
                initialStructure.Add((Cluster.RecoveryMark, oGuid, 1));
            }

            for (int i = 0; i < Size; ++i)
                Members.Add(new Entity(initialStructure, data));
        }
    }
}