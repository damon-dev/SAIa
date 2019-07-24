using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvolutionalNeuralNetwork
{
    public class Population
    {
        public List<Entity> Members { get; set; }
        public int Size { get; set; } = 500;
        public int TournamentSize { get; set; } = 10;
        //Random rand = new Random();

        public Population(DataCollection data)
        {
            GenerateMembers(data.InputWidth, data.OutputWidth, data);
        }

        public int Tournament(Entity mate, Random rand)
        {
            int position = 0;
            Entity bestMate = null;
            double bestFitnes = double.MinValue;
            //var rand = new Random();

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

        public int Victim(Random rand)
        {
            int position = 0;
            Entity weakestPrey = null;
            double worstFitness = double.MaxValue;
            //var rand = new Random();

            for (int i = 0; i < TournamentSize * 2; ++i)
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
            var cluster = new Cluster(new Random());

            // Creating GUIDS for input neurons
            for (int i = 0; i < inputSize; ++i)
                inputGuids.Add(Guid.NewGuid());

            // Creating GUIDS for output neurons
            for (int i = 0; i < outputSize; ++i)
                outputGuids.Add(Guid.NewGuid());

            // Linking input neurons to the reference node and the seed node
            foreach (var iGuid in inputGuids)
            {
                initialStructure.Add((Cluster.InputGuid, iGuid, 0));
                initialStructure.Add((Cluster.RecoveryMark, iGuid, 1));

            //    initialStructure.Add((iGuid, Cluster.SeedGuid, cluster.RandomSynapseStrength()));
            }

            // Linking output neurons to the reference node and the seed node
            foreach (var oGuid in outputGuids)
            {
                initialStructure.Add((oGuid, Cluster.OutputGuid, 0));
                initialStructure.Add((Cluster.RecoveryMark, oGuid, 1));

            //    initialStructure.Add((Cluster.SeedGuid, oGuid, cluster.RandomSynapseStrength()));
            }

            //initialStructure.Add((Cluster.BiasMark, Cluster.SeedGuid, cluster.RandomSynapseStrength()));
            //initialStructure.Add((Cluster.RecoveryMark, Cluster.SeedGuid, Math.Abs(cluster.RandomSynapseStrength())));

            var members = new Entity[Size];
            Parallel.For(0, Size, (i) => {
                members[i] = new Entity(initialStructure, data);
                members[i].EvaluateFitness(new Random());
            });

            Members = new List<Entity>(members);
        }
    }
}