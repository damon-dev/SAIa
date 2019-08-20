using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core
{
    public enum Modes { Grow, Shrink, Balance }
    public class CultureConfiguration
    {
        public static readonly CultureConfiguration Balance = new CultureConfiguration
        {
            Mode = Modes.Balance,
            GlobalMutationRate = .05,
            InternalMutationRates = new NeuronMutationProbabilities
            {
                MutationRate = .005,
                NeuronCreation = .2,
                NeuronDeletion = .2,
                SynapseCreation = .3,
                SynapseDeletion = .3,
                SynapseAlteration = .4
            },
            FitnessFunction = (entity, featureCount) =>
            {
                return entity.InputSize * Math.Pow(Math.Log(entity.Mean), 3) +
                       (entity.NeuronCount + entity.SynapseCount) /
                       (double)featureCount;
            },
            SuccessFunction = (expected, predicted) =>
            {
                int label = expected.IndexOf(1);
                for (int i = 0; i < predicted.Count; ++i)
                    if (predicted[i] >= predicted[label] && i != label)
                        return false;

                return true;
            }
        };

        public static readonly CultureConfiguration Shrink = new CultureConfiguration
        {
            Mode = Modes.Shrink,
            GlobalMutationRate = .1,
            InternalMutationRates = new NeuronMutationProbabilities
            {
                MutationRate = .01,
                NeuronCreation = .1,
                NeuronDeletion = .3,
                SynapseCreation = .2,
                SynapseDeletion = .4,
                SynapseAlteration = .5
            },
            FitnessFunction = (entity, featureCount) =>
            {
                return entity.InputSize * Math.Log(entity.Mean) +
                       (entity.NeuronCount + entity.SynapseCount) /
                       (double)featureCount;
            },
            SuccessFunction = (expected, predicted) =>
            {
                int label = expected.IndexOf(1);
                for (int i = 0; i < predicted.Count; ++i)
                    if (predicted[i] >= predicted[label] && i != label)
                        return false;

                return true;
            }
        };

        public static readonly CultureConfiguration Grow = new CultureConfiguration
        {
            Mode = Modes.Grow,
            GlobalMutationRate = .01,
            InternalMutationRates = new NeuronMutationProbabilities
            {
                MutationRate = .001,
                NeuronCreation = .3,
                NeuronDeletion = .1,
                SynapseCreation = .4,
                SynapseDeletion = .2,
                SynapseAlteration = .3
            },
            FitnessFunction = (entity, featureCount) =>
            {
                return entity.InputSize * Math.Pow(Math.Log(entity.Mean), 5) +
                       (entity.NeuronCount + entity.SynapseCount) /
                       (double)featureCount;
            },
            SuccessFunction = (expected, predicted) =>
            {
                int label = expected.IndexOf(1);
                for (int i = 0; i < predicted.Count; ++i)
                    if (predicted[i] >= predicted[label] && i != label)
                        return false;

                return true;
            }
        };

        public CultureConfiguration() { }

        public CultureConfiguration(CultureConfiguration cfg)
        {
            Mode = cfg.Mode;
            GlobalMutationRate = cfg.GlobalMutationRate;
            InternalMutationRates = cfg.InternalMutationRates;
            FitnessFunction = cfg.FitnessFunction;
            SuccessFunction = cfg.SuccessFunction;
            EntityCount = cfg.EntityCount;
            TournamentSize = cfg.TournamentSize;
        }

        public Modes Mode { get; set; }
        public double GlobalMutationRate { get; set; }
        public NeuronMutationProbabilities InternalMutationRates { get; set; }
        public Func<Entity, int, double> FitnessFunction { get; set; }
        public Func<List<double>, List<double>, bool> SuccessFunction { get; set; }
        public int EntityCount { get; set; } = 50;
        public int TournamentSize { get; set; } = 4;
    }

    public class Culture
    {
        public CultureConfiguration Cfg { get; set; }
        public List<Entity> Entities { get; set; }
        public Entity Champion => Entities[0];

        private Incubator parentIncubator;
        private Task<Entity> overlord;

        public Culture(Incubator incubator, CultureConfiguration _configuration)
        {
            Cfg = _configuration;
            parentIncubator = incubator;
            Entities = new List<Entity>();
        }

        private Entity Tournament(Entity mate, int mateIndex)
        {
            // best fit
            int position = -1;
            double bestFitnes = double.PositiveInfinity;

            // good enough fit still in current culture
            int alrightPosition = -1;
            double alrightFitness = double.PositiveInfinity;

            for (int i = 0; i < Cfg.TournamentSize; ++i)
            {
                int index = R.NG.Next(Cfg.EntityCount);
                while(index == mateIndex)
                    index = R.NG.Next(Cfg.EntityCount);

                double compatibility = mate.Compatibility(Entities[index]);

                if (Entities[index].Fitness < bestFitnes &&
                    compatibility > double.Epsilon && compatibility < 0.1) // if very compatible but not identical
                {
                    bestFitnes = Entities[index].Fitness;
                    position = index;
                }
                else if (Entities[index].Fitness < alrightFitness && compatibility < 0.25) // if somewhat compatible or identical
                {
                    alrightFitness = Entities[index].Fitness;
                    alrightPosition = index;
                }
            }

            if (position == -1)
            {
                var targetCulture = parentIncubator.RandomCulture(this);
                position = R.NG.Next(targetCulture.Entities.Count);
                var entity = targetCulture.Entities[position];

                if (alrightFitness < entity.ComputeFitness(Cfg.FitnessFunction))
                    return Entities[alrightPosition];
                else
                    return entity;
            }
            else
                return Entities[position];
        }

        private int Prey(Entity hunter, int parentIndex)
        {
            if (hunter == null) return -1;

            if (hunter.Fitness < Champion.Fitness) // if this dethrones the current champion
                return 0;

            int position = -1;
            double worstFitness = hunter.Fitness;

            for (int i = 0; i < Cfg.TournamentSize; ++i)
            {
                int index = R.NG.Next(Cfg.EntityCount);
                while (index == parentIndex)
                    index = R.NG.Next(Cfg.EntityCount);

                if (Entities[index].Fitness > hunter.Fitness &&
                    hunter.Compatibility(Entities[index]) < double.Epsilon) // if this is a better version of an existing structure
                {
                    position = index;
                    break;
                }
                else if (Entities[index].Fitness > worstFitness)
                {
                    worstFitness = Entities[index].Fitness;
                    position = index;
                }
            }

            if (position == -1)
            {
                if (hunter.Fitness < Entities[parentIndex].Fitness || 
                    (Entities[parentIndex].ChildCount > 2 && parentIndex != 0))
                    position = parentIndex;
                else
                {
                    var targetCulture = parentIncubator.RandomCulture(this);
                    targetCulture.AttemptImigration(hunter);
                }
            }

            return position;
        }

        public void AttemptImigration(Entity hunter)
        {
            int position = R.NG.Next(Entities.Count);

            hunter.Evaluate(null, Cfg.FitnessFunction, true, null); // assimilate into the target culture

            if (Entities[position].Mean > hunter.Mean)
                Entities[position] = hunter;
        }

        public Task<Entity> EvaluateAll(List<Datum> features, bool cumulative)
        {
            if (overlord != null && !overlord.IsCompleted)
                return overlord;

            overlord = Task.Run(() =>
            {
                for (int i = 0; i < Cfg.EntityCount; ++i)
                {
                    Entities[i].Evaluate(features, Cfg.FitnessFunction, cumulative, Cfg.SuccessFunction);

                    if (Entities[i].Fitness < Champion.Fitness)
                    {
                        var t = Entities[0];
                        Entities[0] = Entities[i];
                        Entities[i] = t;
                    }
                }

                return Champion;
            });

            return overlord;
        }

        public Task<Entity> Develop(List<Datum> features)
        {
            if(overlord != null && !overlord.IsCompleted)
                return overlord;

            overlord = Task.Run(() =>
            {
                int mateIndex;

                Entity mother = null;
                Entity father = null;

                mateIndex = R.NG.Next(Cfg.EntityCount);
                father = Entities[mateIndex];

                int competition = R.NG.Next(Cfg.EntityCount);
                if (Entities[competition].Fitness < father.Fitness)
                {
                    mateIndex = competition;
                    father = Entities[mateIndex];
                }

                mother = Tournament(father, mateIndex);
                var child = mother.Copulate(father, Cfg);

                var p = Cfg.InternalMutationRates;
                var maxChildren = Math.Max(mother.ChildCount, father.ChildCount);

                p.MutationRate = p.MutationRate * maxChildren;
                child.Mutate(p, Cfg.GlobalMutationRate * maxChildren);

                child.Evaluate(features, Cfg.FitnessFunction, false, Cfg.SuccessFunction);

                int prey = Prey(child, mateIndex);
                if (prey > -1)
                    Entities[prey] = child;

                return Champion;
            });

            return overlord;
        }
    }
}