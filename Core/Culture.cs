using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public enum Mode { Grow, Balance, Shrink }

    public class Culture
    {
        public Entity Champion => entities[borderStart];

        private readonly int borderStart;
        private readonly int borderEnd;
        private readonly int tournamentSize;
        private readonly List<Datum> features;

        private List<Entity> entities;
        private Task<Entity> overlord;

        public Culture(List<Entity> _entities, List<Datum> _features, int _borderStart, int _borderEnd, int _tournamentSize)
        {
            entities = _entities;
            borderStart = _borderStart;
            borderEnd = _borderEnd;
            tournamentSize = _tournamentSize;
            features = _features;
        }

        private int Tournament(Entity mate, int mateIndex)
        {
            int position = -1;
            double bestFitnes = double.PositiveInfinity;
            int alrightPosition = -1;
            double alrightFitness = double.PositiveInfinity;

            for (int i = 0; i < tournamentSize; ++i)
            {
                int index = R.NG.Next(borderStart, borderEnd);
                while(index == mateIndex)
                    index = R.NG.Next(borderStart, borderEnd);

                double compatibility = mate.Compatibility(entities[index]);

                if (entities[index].FitnessValue < bestFitnes &&
                    compatibility > double.Epsilon && compatibility < 0.01)
                {
                    bestFitnes = entities[index].FitnessValue;
                    position = index;
                }
                else if (entities[index].FitnessValue < alrightFitness && compatibility < 0.25)
                {
                    alrightFitness = entities[index].FitnessValue;
                    alrightPosition = index;
                }
            }

            if (position == -1)
            {
                position = R.NG.Next(entities.Count);
                while (position == mateIndex)
                    position = R.NG.Next(entities.Count);

                if (entities[position].FitnessValue > alrightFitness)
                    position = alrightPosition;
            }

            return position;
        }

        private int Prey(Entity hunter, Entity parent, int parentIndex)
        {
            if (hunter == null) return -1;

            if (hunter.FitnessValue < entities[borderStart].FitnessValue)
                return borderStart;

            int position = -1;
            double worstFitness = hunter.FitnessValue;

            for (int i = 0; i < tournamentSize; ++i)
            {
                int index = R.NG.Next(borderStart, borderEnd);
                while (index == parentIndex)
                    index = R.NG.Next(borderStart, borderEnd);

                if (entities[index].FitnessValue > hunter.FitnessValue &&
                    hunter.Compatibility(entities[index]) < double.Epsilon)
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

            if (position == -1)
            {
                if (hunter.FitnessValue < entities[parentIndex].FitnessValue)
                    position = parentIndex;
                else
                {
                    int index = R.NG.Next(entities.Count);
                    if (hunter.FitnessValue < entities[index].FitnessValue)
                        position = index;
                }
            }

            return position;
        }

        public Task<Entity> EvaluateAll()
        {
            if (overlord != null && !overlord.IsCompleted)
                return overlord;

            overlord = Task.Run(() =>
            {
                for (int i = borderStart; i < borderEnd; ++i)
                {
                    entities[i].EvaluateFitness(features);

                    if (entities[i].FitnessValue < entities[borderStart].FitnessValue)
                    {
                        var t = entities[borderStart];
                        entities[borderStart] = entities[i];
                        entities[i] = t;
                    }
                }

                return entities[borderStart];
            });

            return overlord;
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
                
                int competition = R.NG.Next(borderStart, borderEnd);
                fatherIndex = R.NG.Next(borderStart, borderEnd);
                father = entities[fatherIndex];

                if (entities[competition].FitnessValue < father.FitnessValue)
                {
                    fatherIndex = competition;
                    father = entities[fatherIndex];
                }

                motherIndex = Tournament(father, fatherIndex);
                mother = entities[motherIndex];

                if (mother.FitnessValue > father.FitnessValue)
                {
                    var t1 = mother;
                    mother = father;
                    father = t1;

                    var t2 = motherIndex;
                    motherIndex = fatherIndex;
                    fatherIndex = t2;
                }

                var child = mother.Copulate(father, mode);

                child.Mutate(mode, mutationRate);

                child.EvaluateFitness(features);

                int prey = Prey(child, father, fatherIndex);
                if (prey > -1)
                    entities[prey] = child;

                return entities[borderStart];
            });

            return overlord;
        }
    }
}