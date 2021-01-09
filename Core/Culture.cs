using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public enum Modes { Grow, Shrink, Balance }
    public class CultureConfiguration
    {
        public static readonly CultureConfiguration Balance = new CultureConfiguration
        {
            Mode = Modes.Balance,
            EntityMutationChance = .4,
            InternalMutationRates = new NeuronMutationProbabilities
            {
                NeuronMutationsPercentage = .05,
                NeuronCreation = .03,
                NeuronDeletion = .02,
                SynapseCreation = .05,
                SynapseDeletion = .04,
                SynapseAlteration = .8
            }
        };

        public static readonly CultureConfiguration Shrink = new CultureConfiguration
        {
            Mode = Modes.Shrink,
            EntityMutationChance = .2,
            InternalMutationRates = new NeuronMutationProbabilities
            {
                NeuronMutationsPercentage = .1,
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
            EntityMutationChance = .2,
            InternalMutationRates = new NeuronMutationProbabilities
            {
                NeuronMutationsPercentage = .01,
                NeuronCreation = .03,
                NeuronDeletion = .01,
                SynapseCreation = .4,
                SynapseDeletion = .1,
                SynapseAlteration = .5
            }
        };

        public Modes Mode { get; set; }
        public double EntityMutationChance { get; set; } 
        public NeuronMutationProbabilities InternalMutationRates { get; set; } 
        public int TournamentSize { get; set; } = 5;
        public double SpeciationFactor = 3; // TODO: needs to be saved
        public int ImprovementDeadline = 30;

        public CultureConfiguration() { }

        public CultureConfiguration(CultureConfiguration cfg)
        {
            Mode = cfg.Mode;
            EntityMutationChance = cfg.EntityMutationChance;
            InternalMutationRates = cfg.InternalMutationRates;
            TournamentSize = cfg.TournamentSize;
            SpeciationFactor = cfg.SpeciationFactor;
        }
    }

    public class Culture
    {
        public CultureConfiguration Cfg { get; set; }
        public List<Entity> Entities { get; set; }
        public Agent Agent { get; private set; }

        public object SpeciesLock = new object();
        public Dictionary<Guid, (Entity Original, double SharedFitness, int Count, int noImprovementCount)> SpeciesCatalog { get; set; }
        public Incubator HostIncubator { get; private set; }

        private Task<Entity> overlord;

        public Culture(Incubator incubator, Agent agent, CultureConfiguration _configuration)
        {
            Cfg = _configuration;
            HostIncubator = incubator;
            Entities = new List<Entity>();
            SpeciesCatalog = new Dictionary<Guid, (Entity, double, int, int)>();
            Agent = agent;
        }

        private int Tournament()
        {
            int bestPosition = -1;
            double bestFitnes = double.NegativeInfinity;

            for (int i = 0; i < Cfg.TournamentSize; ++i)
            {
                int index = R.NG.Next(Entities.Count);
                double sharedFitness = Entities[index].SharedFitness();
                int noImp = SpeciesCatalog[Entities[index].Species].noImprovementCount;

                if (noImp < Cfg.ImprovementDeadline
                && (sharedFitness > bestFitnes 
                    || (bestPosition > -1 && Math.Abs(sharedFitness - bestFitnes) < 0.00001 
                        && Entities[bestPosition].Fitness < Entities[index].Fitness))) 
                // finds a better species or a better entity from the species
                {
                    bestFitnes = sharedFitness;
                    bestPosition = index;
                }
            }

            return bestPosition;
        }

        /// <summary>
        /// Finds a partner thats fitter than mate.
        /// </summary>
        /// <param name="mate">The weaker mate</param>
        /// <param name="mateIndex">Mate position</param>
        /// <returns>Fitter entity</returns>
        private Entity Tournament(Entity mate, int mateIndex)
        {
            int position = -1;
            double bestFitnes = double.NegativeInfinity;

            int alrightPosition = -1;
            double alrightFitness = double.NegativeInfinity;

            for (int i = 0; i < Cfg.TournamentSize; ++i)
            {
                int index = R.NG.Next(Entities.Count);
                while(index == mateIndex)
                    index = R.NG.Next(Entities.Count);

                if (Entities[index].Fitness > bestFitnes
                && mate.Species == Entities[index].Species)
                {
                    bestFitnes = Entities[index].Fitness;
                    position = index;
                }

                if (position == -1
                && Entities[index].Fitness > alrightFitness
                && mate.Compatibility(Entities[index]) < Cfg.SpeciationFactor
                && SpeciesCatalog[Entities[index].Species].noImprovementCount < Cfg.ImprovementDeadline)
                {
                    alrightFitness = Entities[index].Fitness;
                    alrightPosition = index;
                }
            }

            if (position == -1)
            {
                var targetCulture = HostIncubator.RandomCulture(this);
                position = R.NG.Next(targetCulture.Entities.Count);
                var foreignEntity = targetCulture.Entities[position];

                if (alrightPosition != -1)
                    return Entities[alrightPosition];
                if (mate.Compatibility(foreignEntity) < Cfg.SpeciationFactor)
                    return foreignEntity;
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

            if (hunter.Fitness > Entities[0].Fitness) // dethrones the current culture champion
                return 0;

            if (Entities[parentIndex].ChildCount > 5
            && parentIndex != 0) // parent is old and can be replaced
                return parentIndex;

            int worstPosition = -1;
            double hunterSharedFitness = hunter.SharedFitness();
            double worstFitness = hunterSharedFitness;

            for (int i = 0; i < Cfg.TournamentSize; ++i)
            {
                int index = R.NG.Next(1, Entities.Count);
                while (index == parentIndex)
                    index = R.NG.Next(1, Entities.Count);

                var newPrey = Entities[index];
                double preySharedFitness = newPrey.SharedFitness();
                int preyNoImp = SpeciesCatalog[newPrey.Species].noImprovementCount;

                if (preyNoImp >= Cfg.ImprovementDeadline)
                {
                    worstFitness = preySharedFitness;
                    worstPosition = index;
                    break;
                }
                else if (preySharedFitness < worstFitness // found weaker species
                || (worstPosition > -1 && Math.Abs(preySharedFitness - worstFitness) < 0.00001 
                    && newPrey.Fitness < Entities[worstPosition].Fitness) // found weaker individual of species with same weakness
                || (hunter.Species == newPrey.Species 
                    && hunter.Fitness > newPrey.Fitness 
                    && preySharedFitness <= worstFitness)) // found weaker individual in same species as hunter
                {
                    worstFitness = preySharedFitness;
                    worstPosition = index;
                }
            }

            if (worstPosition == -1)
            {
                if (hunter.Fitness > Entities[parentIndex].Fitness) // parental sacrifice
                    worstPosition = parentIndex;
                else
                {
                    var targetCulture = HostIncubator.RandomCulture(this);
                    targetCulture.AttemptInvasion(hunter);
                }
            }

            return worstPosition;
        }

        private void AttemptInvasion(Entity hunter)
        {
            hunter.HostCulture = this;
            hunter.Speciate();

            lock (SpeciesLock)
            {
                if (Entities[0].Fitness < hunter.Fitness)
                {
                    ReplaceEntity(hunter, 0);
                }
                else
                {
                    int index = R.NG.Next(1, Entities.Count);

                    if (Entities[index].SharedFitness() < hunter.SharedFitness())
                        ReplaceEntity(hunter, index);
                }
            }
        }

        private void ReplaceEntity(Entity replacement, int position)
        {
            lock (SpeciesLock)
            {
                var (original, fitness, count, noImp) = SpeciesCatalog[Entities[position].Species];
                fitness = (fitness * count - Entities[position].Fitness) / (count - 1);
                SpeciesCatalog[Entities[position].Species] = (original, fitness, count - 1, noImp);

                if (SpeciesCatalog[Entities[position].Species].Count <= 0)
                    SpeciesCatalog.Remove(Entities[position].Species);

                Entities[position] = replacement;

                if (SpeciesCatalog.ContainsKey(replacement.Species))
                {
                    (original, fitness, count, noImp) = SpeciesCatalog[replacement.Species];
                    double oldFitness = fitness;
                    fitness = (fitness * count + replacement.Fitness) / (count + 1);
                    // counts the number of times a child spawned without the species improving
                    if (fitness <= oldFitness) noImp++;
                    else noImp = 0;
                    SpeciesCatalog[replacement.Species] = (original, fitness, count + 1, noImp);
                }
                else
                    SpeciesCatalog.Add(replacement.Species, (replacement, replacement.Fitness, 1, 0));
            }
        }

        public void SpeciateAll()
        {
            SpeciesCatalog = new Dictionary<Guid, (Entity, double, int, int)>();

            for (int i = 0; i < Entities.Count; ++i)
            {
                Entities[i].Species = Guid.Empty;

                for (int j = 0; j < i; ++j)
                {
                    if (Entities[j].Compatibility(Entities[i]) < Cfg.SpeciationFactor)
                    {
                        var (original, fitness, count, noImp) = SpeciesCatalog[Entities[j].Species];
                        fitness = (fitness * count + Entities[i].Fitness) / (count + 1);
                        SpeciesCatalog[Entities[j].Species] = (original, fitness, count + 1, noImp);
                        Entities[i].Species = Entities[j].Species;
                        break;
                    }
                }

                if (Entities[i].Species == Guid.Empty)
                {
                    Entities[i].Species = Guid.NewGuid();
                    SpeciesCatalog.Add(Entities[i].Species, (Entities[i], Entities[i].Fitness, 1, 0));
                }
            }
        }

        public Task<Entity> EvaluateAll()
        {
            if (overlord != null && !overlord.IsCompleted)
                return overlord;

            overlord = Task.Run(async () =>
            {
                for (int i = 0; i < Entities.Count; ++i)
                {
                    await Entities[i].Evaluate(1, Agent);

                    if (Entities[i].Fitness > Entities[0].Fitness)
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

        public Task<Entity> Develop()
        {
            if(overlord != null && !overlord.IsCompleted)
                return overlord;

            overlord = Task.Run(async () =>
            {
                int mateIndex;

                Entity mother = null;
                Entity father = null;

                mateIndex = Tournament();

                while (mateIndex == -1)
                    mateIndex = Tournament();

                father = Entities[mateIndex];
                mother = Tournament(father, mateIndex);
                if (father.Fitness > mother.Fitness) // mother has to be the fitter parrent
                {
                    var temp = father;
                    father = mother;
                    mother = temp;
                }
                bool asexual = mother.Equals(father);

                var child = mother.Copulate(father, Cfg);
                child.HostCulture = this;

                var p = Cfg.InternalMutationRates;
                int genomeQuality = Math.Max(mother.ChildCount, father.ChildCount);

                bool hasMutated = false;
                p.NeuronMutationsPercentage = p.NeuronMutationsPercentage * genomeQuality; // the more children the entities had the higher chance of mutation

                if (asexual) // always mutate if asexual conception 
                    hasMutated = child.Mutate(p, 1);
                else
                    hasMutated = child.Mutate(p, Cfg.EntityMutationChance * genomeQuality);

                await child.Evaluate(3, Agent);

                if (mother.Species == father.Species && !hasMutated)
                {
                    child.Species = mother.Species;
                }
                else
                {
                    child.Speciate(mother);

                    //for (int i = 0; i < 3 && (child.Species != mother.Species && asexual); ++i)
                    //{
                    //    Cfg.SpeciationFactor += 0.3;
                    //    child.Speciate(mother);
                    //}
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