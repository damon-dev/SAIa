using System;
using System.Collections.Generic;

namespace EvolutionalNeuralNetwork
{
    public class Culture
    {
        public List<Entity> Entities { get; set; }
        public int Size { get; set; } = 500;
        public int TournamentSize { get; set; } = 10;

        public Culture(DataCollection data, List<Entity> entities = null)
        {
            if (entities == null)
                GenerateMembers(data.InputWidth, data.OutputWidth, data);
            else
            {
                Entities = entities;
                Size = Entities.Count;
            }
        }

        public int Tournament(Entity mate, Random rand)
        {
            int position = 0;
            Entity bestMate = null;
            double bestFitnes = double.MinValue;

            for (int i = 0; i < TournamentSize; ++i)
            {
                int index = rand.Next(0, Entities.Count);
                if (Entities[index].FitnessValue > bestFitnes && !Entities[index].Equals(mate))
                {
                    bestMate = Entities[index];
                    bestFitnes = bestMate.FitnessValue;
                    position = index;
                }
            }

            return position;
        }

        public int Victim(Random rand)
        {
            int position = 0;
            Entity weakestPrey = null;
            double worstFitness = double.MaxValue;

            for (int i = 0; i < TournamentSize * 2; ++i)
            {
                int index = rand.Next(1, Entities.Count);
                if (Entities[index].FitnessValue < worstFitness)
                {
                    weakestPrey = Entities[index];
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
            var cluster = new Cluster(new Random());

            Entities = new List<Entity>();

            // Creating GUIDS for input neurons
            for (int i = 0; i < inputSize; ++i)
                inputGuids.Add(Guid.NewGuid());

            // Creating GUIDS for output neurons
            for (int i = 0; i < outputSize; ++i)
                outputGuids.Add(Guid.NewGuid());

            // Linking input neurons to the reference node and the seed node
            foreach (var iGuid in inputGuids)
                initialStructure.Add((Cluster.InputGuid, iGuid, 0));

            // Linking output neurons to the reference node and the seed node
            foreach (var oGuid in outputGuids)
                initialStructure.Add((oGuid, Cluster.OutputGuid, 0));

            for (int i = 0; i < Size; ++i)
                Entities.Add(new Entity(initialStructure, data));
        }
    }
}