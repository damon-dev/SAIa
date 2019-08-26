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
            GlobalMutationRate = .03,
            InternalMutationRates = new NeuronMutationProbabilities
            {
                MutationRate = .005,
                NeuronCreation = .02,
                NeuronDeletion = .01,
                SynapseCreation = .3,
                SynapseDeletion = .2,
                SynapseAlteration = .6
            }
        };

        public static readonly CultureConfiguration Shrink = new CultureConfiguration
        {
            Mode = Modes.Shrink,
            GlobalMutationRate = .05,
            InternalMutationRates = new NeuronMutationProbabilities
            {
                MutationRate = .01,
                NeuronCreation = .01,
                NeuronDeletion = .03,
                SynapseCreation = .1,
                SynapseDeletion = .4,
                SynapseAlteration = .7
            }
        };

        public static readonly CultureConfiguration Grow = new CultureConfiguration
        {
            Mode = Modes.Grow,
            GlobalMutationRate = .01,
            InternalMutationRates = new NeuronMutationProbabilities
            {
                MutationRate = .001,
                NeuronCreation = .03,
                NeuronDeletion = .01,
                SynapseCreation = .4,
                SynapseDeletion = .1,
                SynapseAlteration = .5
            }
        };

        public Modes Mode { get; set; }
        public double GlobalMutationRate { get; set; }
        public NeuronMutationProbabilities InternalMutationRates { get; set; }
        public Func<List<double>, List<double>, bool> SuccessFunction { get; set; }
        public int TournamentSize { get; set; } = 10;
        public int MaxChildren { get; set; } = 20;
        public double SpeciationFactor = 10;

        public CultureConfiguration() { }

        public CultureConfiguration(CultureConfiguration cfg)
        {
            Mode = cfg.Mode;
            GlobalMutationRate = cfg.GlobalMutationRate;
            InternalMutationRates = cfg.InternalMutationRates;
            TournamentSize = cfg.TournamentSize;
            SuccessFunction = cfg.SuccessFunction;
            SpeciationFactor = cfg.SpeciationFactor;
            MaxChildren = cfg.MaxChildren;
        }
    }

    public class Culture
    {
        public CultureConfiguration Cfg { get; set; }
        public List<Entity> Entities { get; set; }

        public object SpeciesLock = new object();
        public Dictionary<Guid, (Entity Original, double SharedFitness, int Count)> SpeciesCatalog { get; set; }
        public Incubator HostIncubator { get; private set; }

        private Task<Entity> overlord;

        public Culture(Incubator incubator, CultureConfiguration _configuration)
        {
            Cfg = _configuration;
            HostIncubator = incubator;
            Entities = new List<Entity>();
            SpeciesCatalog = new Dictionary<Guid, (Entity, double, int)>();
        }

        private int Tournament()
        {
            // best fit
            int bestPosition = -1;
            double bestFitnes = double.PositiveInfinity;

            for (int i = 0; i < Cfg.TournamentSize; ++i)
            {
                int index = R.NG.Next(Entities.Count);
                double sharedFitness = Entities[index].SharedFitness();

                if (Entities[index].ChildCount < Cfg.MaxChildren
                && (sharedFitness < bestFitnes
                || (bestPosition > -1 && Math.Abs(sharedFitness - bestFitnes) < 0.00001 && Entities[bestPosition].Fitness > Entities[index].Fitness)))
                {
                    bestFitnes = sharedFitness;
                    bestPosition = index;
                }
            }

            return bestPosition;
        }

        private Entity Tournament(Entity mate, int mateIndex)
        {
            // best fit
            int position = -1;
            double bestFitnes = double.PositiveInfinity;

            int alrightPosition = -1;
            double alrightFitness = double.PositiveInfinity;

            for (int i = 0; i < Cfg.TournamentSize; ++i)
            {
                int index = R.NG.Next(Entities.Count);
                while(index == mateIndex)
                    index = R.NG.Next(Entities.Count);

                if (Entities[index].ChildCount < Cfg.MaxChildren
                && Entities[index].Fitness < bestFitnes 
                && Entities[index].Species == mate.Species)
                {
                    bestFitnes = Entities[index].Fitness;
                    position = index;
                }

                if (position == -1
                && Entities[index].ChildCount < Cfg.MaxChildren
                && Entities[index].Fitness < alrightFitness
                && mate.Compatibility(Entities[index]) < Cfg.SpeciationFactor)
                {
                    alrightFitness = Entities[index].Fitness;
                    alrightPosition = index;
                }
            }

            if (position == -1)
            {
                var targetCulture = HostIncubator.RandomCulture(this);
                position = R.NG.Next(targetCulture.Entities.Count);
                var entity = targetCulture.Entities[position];

                if (alrightPosition != -1)
                    return Entities[alrightPosition];
                else if (mate.Compatibility(entity) < Cfg.SpeciationFactor)
                    return entity;
                else
                    return mate; // if no mate has been found mutate the mate
            }
            else
                return Entities[position];
        }

        private int Prey(Entity hunter, int parentIndex)
        {
            if (hunter == null)
                return -1;

            if (hunter.Fitness < Entities[0].Fitness) // dethrones the current culture champion
                return 0;

            if (Entities[parentIndex].ChildCount > 2 && parentIndex != 0) // parent is old and can be replaced
                return parentIndex;

            int worstPosition = -1;
            double hunterSharedFitness = hunter.SharedFitness();
            double worstFitness = hunterSharedFitness;

            for (int i = 0; i < Cfg.TournamentSize; ++i)
            {
                int index = R.NG.Next(Entities.Count);
                while (index == parentIndex || index == 0)
                    index = R.NG.Next(Entities.Count);

                var newPrey = Entities[index];
                double preySharedFitness = newPrey.SharedFitness();

                if (preySharedFitness > worstFitness // found weaker species
                || (worstPosition > -1 && Math.Abs(preySharedFitness - worstFitness) < 0.00001 && newPrey.Fitness > Entities[worstPosition].Fitness) // found weaker individual of already found weaker species
                || (hunter.Species == newPrey.Species && hunter.Fitness < newPrey.Fitness && preySharedFitness >= worstFitness)) // found weaker individual in same species as hunter
                {
                    worstFitness = preySharedFitness;
                    worstPosition = index;
                }
            }

            if (worstPosition == -1)
            {
                if (hunter.Fitness < Entities[parentIndex].Fitness) // parental sacrifice
                    worstPosition = parentIndex;
                else
                {
                    var targetCulture = HostIncubator.RandomCulture(this);
                    targetCulture.AttemptInvasion(hunter);
                }
            }

            return worstPosition;
        }

        private void ReplaceEntity(Entity replacement, int position)
        {
            lock (SpeciesLock)
            {
                var (original, fitness, count) = SpeciesCatalog[Entities[position].Species];
                fitness = (fitness * count - Entities[position].Fitness) / (count - 1);
                SpeciesCatalog[Entities[position].Species] = (original, fitness, count - 1);

                if (SpeciesCatalog[Entities[position].Species].Count <= 0)
                    SpeciesCatalog.Remove(Entities[position].Species);

                Entities[position] = replacement;

                if (SpeciesCatalog.ContainsKey(replacement.Species))
                {
                    (original, fitness, count) = SpeciesCatalog[replacement.Species];
                    fitness = (fitness * count + replacement.Fitness) / (count + 1);
                    //fitness = Math.Min(fitness, replacement.Fitness);
                    SpeciesCatalog[replacement.Species] = (original, fitness, count + 1);
                }
                else
                    SpeciesCatalog.Add(replacement.Species, (replacement, replacement.Fitness, 1));
            }
        }

        private void AttemptInvasion(Entity hunter)
        {
            hunter.HostCulture = this;
            hunter.Speciate();

            lock (SpeciesLock)
            {
                if (Entities[0].Fitness > hunter.Fitness)
                {
                    ReplaceEntity(hunter, 0);
                }
                else
                {
                    int index = R.NG.Next(Entities.Count);

                    if (Entities[index].SharedFitness() > hunter.SharedFitness())
                        ReplaceEntity(hunter, index);
                }
            }
        }

        public void SpeciateAll()
        {
            SpeciesCatalog = new Dictionary<Guid, (Entity, double, int)>();

            for (int i = 0; i < Entities.Count; ++i)
            {
                Entities[i].Species = Guid.Empty;

                for (int j = 0; j < i; ++j)
                {
                    if (Entities[j].Compatibility(Entities[i]) < Cfg.SpeciationFactor)
                    {
                        var (original, fitness, count) = SpeciesCatalog[Entities[j].Species];
                        fitness = (fitness * count + Entities[i].Fitness) / (count + 1); // average
                        //fitness = Math.Min(fitness, Entities[i].Fitness); // best fitness
                        SpeciesCatalog[Entities[j].Species] = (original, fitness, count + 1);
                        Entities[i].Species = Entities[j].Species;
                        break;
                    }
                }

                if (Entities[i].Species == Guid.Empty)
                {
                    Entities[i].Species = Guid.NewGuid();
                    SpeciesCatalog.Add(Entities[i].Species, (Entities[i], Entities[i].Fitness, 1));
                }
            }
        }

        public Task<Entity> EvaluateAll(List<Datum> features, bool cumulative)
        {
            if (overlord != null && !overlord.IsCompleted)
                return overlord;

            overlord = Task.Run(() =>
            {
                for (int i = 0; i < Entities.Count; ++i)
                {
                    Entities[i].Evaluate(features, cumulative, Cfg.SuccessFunction);

                    if (Entities[i].Fitness < Entities[0].Fitness)
                    {
                        var t = Entities[0];
                        Entities[0] = Entities[i];
                        Entities[i] = t;
                    }
                }

                SpeciateAll();

                return Entities[0];
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

                mateIndex = Tournament();

                while (mateIndex == -1)
                    mateIndex = Tournament();

                father = Entities[mateIndex];

                mother = Tournament(father, mateIndex);

                var child = mother.Copulate(father, Cfg);
                child.HostCulture = this;

                var p = Cfg.InternalMutationRates;
                int genomeQuality = Math.Max(mother.ChildCount, father.ChildCount);

                bool hasMutated = false;
                p.MutationRate = p.MutationRate * genomeQuality;

                if (mother.Equals(father))
                    hasMutated = child.Mutate(p, 1);
                else
                    hasMutated = child.Mutate(p, Cfg.GlobalMutationRate * genomeQuality);

                child.Evaluate(features, false, Cfg.SuccessFunction);

                child.Speciate(mother, father);

                if (!hasMutated && mother.Species == father.Species)
                {
                    while (child.Species != mother.Species)
                    {
                        Cfg.SpeciationFactor += 0.3;
                        child.Speciate(mother, father);
                    }
                }

                int prey;
                prey = Prey(child, mateIndex);

                if (prey > -1)
                    ReplaceEntity(child, prey);

                return Entities[0];
            });

            return overlord;
        }
    }
}