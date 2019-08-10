using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvolutionalNeuralNetwork
{
    public enum Mode { Grow, Balance, Shrink }

    public class Culture
    {
        public Entity Champion => entities[borderStart];

        private List<Entity> entities;
        private readonly int borderStart;
        private readonly int borderEnd;
        private readonly int tournamentSize;
        private Task<Entity> overlord;

        public Culture(List<Entity> _entities, DataCollection _data, int _borderStart, int _borderEnd, int _tournamentSize)
        {
            entities = _entities;
            borderStart = _borderStart;
            borderEnd = _borderEnd;
            tournamentSize = _tournamentSize;
        }

        private int Tournament(Entity mate, int mateIndex, Random rand)
        {
            int position = -1;
            double bestFitnes = double.MaxValue;
            int alrightPosition = -1;
            double alrightFitness = double.MaxValue;

            for (int i = 0; i < tournamentSize; ++i)
            {
                int index = rand.Next(borderStart, borderEnd);
                while(index == mateIndex)
                    index = rand.Next(borderStart, borderEnd);

                if (entities[index].FitnessValue < bestFitnes &&
                    mate.Compatible(entities[index]))
                {
                    bestFitnes = entities[index].FitnessValue;
                    position = index;
                }
                else if (entities[index].FitnessValue < alrightFitness)
                {
                    alrightFitness = entities[index].FitnessValue;
                    alrightPosition = index;
                }
            }

            if (position == -1)
            {
                position = rand.Next(entities.Count);
                while (position == mateIndex)
                    position = rand.Next(entities.Count);

                if (entities[position].FitnessValue > alrightFitness)
                    position = alrightPosition;
            }

            return position;
        }

        private int Prey(Entity hunter, Entity parent, int parentIndex, Random rand)
        {
            if (hunter == null) return -1;

            if (hunter.FitnessValue < entities[borderStart].FitnessValue)
                return borderStart;

            int position = -1;
            double worstFitness = hunter.FitnessValue;

            for (int i = 0; i < tournamentSize; ++i)
            {
                int index = rand.Next(borderStart, borderEnd);
                while (index == parentIndex)
                    index = rand.Next(borderStart, borderEnd);

                if (entities[index].FitnessValue > hunter.FitnessValue &&
                    !hunter.Compatible(entities[index]))
                {
                    position = index;
                    break;
                }
                else if (entities[index].FitnessValue > worstFitness)
                {
                    worstFitness = entities[index].FitnessValue;
                    position = index;
                }
            }

            if (position == -1 && parentIndex != borderStart)
            {
                if (entities[parentIndex].Equals(parent) ||
                    hunter.FitnessValue < entities[parentIndex].FitnessValue)
                    position = parentIndex;
            }

            if (position == -1)
            {
                int index = rand.Next(entities.Count);
                if (hunter.FitnessValue < entities[index].FitnessValue)
                    position = index;
            }

            return position;
        }

        public Task<Entity> Develop(Mode mode, double mutationRate)
        {
            if(overlord != null && !overlord.IsCompleted)
                return overlord;

            overlord = Task.Run(() =>
            {
                int motherIndex, fatherIndex;

                Entity mother = null;
                Entity father = null;

                var rand = new Random();
                
                int competition = rand.Next(borderStart, borderEnd);
                fatherIndex = rand.Next(borderStart, borderEnd);
                father = entities[fatherIndex];

                if (entities[competition].FitnessValue < father.FitnessValue)
                {
                    fatherIndex = competition;
                    father = entities[fatherIndex];
                }

                motherIndex = Tournament(father, fatherIndex, rand);
                mother = entities[motherIndex];

                var kids = mother.Copulate(father, mode, rand);

                Parallel.ForEach(kids, (c) =>
                {
                    c.EvaluateFitness(mode, mutationRate, new Random());
                });

                Entity child = null;
                double bestFitness = double.MaxValue;
                foreach(var c in kids)
                {
                    if (c.FitnessValue < bestFitness)
                    {
                        bestFitness = c.FitnessValue;
                        child = c;
                    }
                }

                int prey = Prey(child, father, fatherIndex, rand);
                if (prey > -1)
                    entities[prey] = child;

                return entities[borderStart];
            });

            return overlord;
        }
    }
}