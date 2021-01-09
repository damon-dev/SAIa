using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Entity
    {
        private List<Gene> _genes;
        [JsonProperty]
        public List<Gene> Genes
        {
            get
            {
                return _genes;
            }
            private set
            {
                _genes = value;
                _genes.Sort();
            }
        }
        [JsonProperty]
        public double Fitness { get; private set; } // the higher the fitness is the better
        [JsonProperty]
        public int ChildCount { get; private set; }
        [JsonProperty]
        public Guid Species { get; set; }

        public Culture HostCulture { get; set; }

        public int NeuronCount { get; private set; }
        public int SynapseCount { get; private set; }

        public int InputSize { get; private set; }
        public int OutputSize { get; private set; }

        public Entity() { }

        public Entity(List<Gene> structure)
        {
            Genes = new List<Gene>(structure);

            var brain = new Brain();
            brain.GenerateFromStructure(Genes);

            NeuronCount = brain.NeuronCount;
            SynapseCount = brain.SynapseCount;

            InputSize = brain.InputSize;
            OutputSize = brain.OutputSize;
        }

        // NEAT rule of speciacion
        public double SharedFitness()
        {
            if (!HostCulture.SpeciesCatalog.TryGetValue(Species, out var species) 
            || species.Count <= 0)
                return Fitness;

            return species.SharedFitness / species.Count;
        }

        /// <summary>
        /// Evaluates the fitness of the entity in a given world.
        /// </summary>
        /// <param name="worldLink">Link to the environment the entity needs to be evaluated in.</param>
        /// <param name="numberOfEvaluations">Represents the count of this current evaluation w.r.t moving average of fitness.</param>
        public async Task Evaluate(int numberOfEvaluations, Agent agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            var brain = new Brain();
            brain.GenerateFromStructure(Genes);

            for (int i = 0; i < numberOfEvaluations; ++i)
            {
                bool agentActive = await agent.ActivateAgent();

                if (agentActive)
                {
                    await brain.Hijack(agent);

                    Fitness = Fitness + (agent.CurrentPerformance - Fitness) / (i + 1);
                    //if (agent.CurrentPerformance > Fitness) Fitness = agent.CurrentPerformance;

                    brain.Nap();
                }
                else
                    i--;
            }
        }

        public bool Mutate(NeuronMutationProbabilities p, double mutationRate)
        {
            var brain = new Brain();
            brain.GenerateFromStructure(Genes);

            Genes = brain.Mutate(p, mutationRate, out bool hasMutated);

            if (hasMutated)
            {
                if (InputSize != brain.InputSize || OutputSize != brain.OutputSize)
                    throw new Exception("Mutation experienced a critical error!");

                NeuronCount = brain.NeuronCount;
                SynapseCount = brain.SynapseCount;
            }

            return hasMutated;
        }

        public void Speciate()
        {
            int lowestCount = int.MaxValue;
            double factor;
            List<(Entity original, double sharedFitness, int count, int noImprovementCount)> catalog;

            lock (HostCulture.SpeciesLock)
            {
                catalog = new List<(Entity, double, int, int)>(HostCulture.SpeciesCatalog.Values);
                factor = HostCulture.Cfg.SpeciationFactor;
            }

            foreach (var (original, sharedFitness, count, noImprovementCount) in catalog)
            {
                double compatibility = Compatibility(original);

                if (compatibility < factor
                && count < lowestCount)
                {
                    lowestCount = count;
                    Species = original.Species;
                }
            }

            if (Species == Guid.Empty)
                Species = Guid.NewGuid();
        }

        public void Speciate(Entity strongMate)
        {
            Entity original = null;
            if (HostCulture.SpeciesCatalog.TryGetValue(strongMate.Species, out (Entity, double, int, int) species))
                original = species.Item1;

            if (original == null) // if the species went exting while the evaluation took place
            {
                if (Compatibility(strongMate) < HostCulture.Cfg.SpeciationFactor)
                    Species = strongMate.Species;
            }
            else
            {
                if (Compatibility(original) < HostCulture.Cfg.SpeciationFactor)
                    Species = strongMate.Species;
            }

            if (Species == Guid.Empty)
                Speciate();
        }

        // percentage of genes that are unique between the mates (NEAT rule)
        public double Compatibility(Entity other, double excessC = 1, double disjointC = 1, double commonC = 0.4)
        {
            if (other == null)
                return double.MaxValue;
            // todo: Gene number normalisation is ommited
            double wDistance = 0;

            var thisGenes = this.Genes;
            var otherGenes = other.Genes;

            int m = 0, f = 0;
            while (m < thisGenes.Count && f < otherGenes.Count)
            {
                var X = thisGenes[m];
                var Y = otherGenes[f];
                if (X.Source == Y.Source)
                {
                    if (X.Destination == Y.Destination)
                    {
                        wDistance += commonC * Math.Abs(X.Strength - Y.Strength);

                        m++;
                        f++;
                    }
                    else if (X.Destination.CompareTo(Y.Destination) < 0)
                    {
                        wDistance += disjointC;
                        m++;
                    }
                    else
                    {
                        wDistance += disjointC;
                        f++;
                    }
                }
                else if (X.Source.CompareTo(Y.Source) < 0)
                {
                    wDistance += disjointC;
                    m++;
                }
                else
                {
                    wDistance += disjointC;
                    f++;
                }
            }

            while (m < thisGenes.Count)
            {
                var gene = thisGenes[m++];
                wDistance += excessC;
            }

            while (f < otherGenes.Count)
            {
                var gene = otherGenes[f++];
                wDistance += excessC;
            }

            return wDistance;
        }

        public Entity Copulate(Entity weakling, CultureConfiguration cfg)
        {
            Entity strong = this;

            if (strong.Equals(weakling))
            {
                strong.ChildCount++;
                return new Entity(strong.Genes);
            }

            strong.ChildCount++;
            weakling.ChildCount++;

            var strongGenes = new List<Gene>(strong.Genes);
            var weakGenes = new List<Gene>(weakling.Genes);

            var commonGenes = new List<Gene>();
            var strongUniqueGenes = new List<Gene>();
            var weakUniqueGenes = new List<Gene>();

            int m = 0, f = 0;
            while (m < strongGenes.Count && f < weakGenes.Count)
            {
                var X = strongGenes[m];
                var Y = weakGenes[f];
                if (X.Source == Y.Source)
                {
                    if (X.Destination == Y.Destination)
                    {
                        // randomly crosses the strength from the parents
                        var dbl = R.NG.NextDouble();
                        double str = (dbl < 0.5) ? X.Strength : Y.Strength;
                        commonGenes.Add((X.Source, Y.Destination, str));

                        m++;
                        f++;
                    }
                    else if (X.Destination.CompareTo(Y.Destination) < 0)
                    {
                        strongUniqueGenes.Add(X);
                        m++;
                    }
                    else
                    {
                        weakUniqueGenes.Add(Y);
                        f++;
                    }
                }
                else if (X.Source.CompareTo(Y.Source) < 0)
                {

                    strongUniqueGenes.Add(X);
                    m++;
                }
                else
                {
                    weakUniqueGenes.Add(Y);
                    f++;
                }
            }

            while (m < strongGenes.Count)
            {
                var gene = strongGenes[m++];
                strongUniqueGenes.Add(gene);
            }

            while (f < weakGenes.Count)
            {
                var gene = weakGenes[f++];
                weakUniqueGenes.Add(gene);
            }

            /*
            switch (cfg.Mode)
            {
                case Modes.Grow:
                    return MoreLike(strongGenes, strongUniqueGenes, weakUniqueGenes, commonGenes);

                case Modes.Balance:
                    return ExclusiveMoreLike(strongGenes, strongUniqueGenes, commonGenes);

                case Modes.Shrink:
                    return AbsoluteMoreLike(strongGenes, strongUniqueGenes, commonGenes);

                default: return null;
            }
            */
            return ExclusiveMoreLike(strongGenes, strongUniqueGenes, commonGenes);
        }

        // crosses only the common genes
        private Entity AbsoluteMoreLike(List<Gene> dominant, List<Gene> common)
        {
            var baby = new Entity(common);
            /*
            int i = 0;
            int b = 0;

            for (i = 0; i < dominant.Count && b < baby.Genes.Count; ++i)
            {
                var X = dominant[i];
                var B = baby.Genes[b];
                if (X.Source == B.Source && X.Destination == B.Destination)
                {
                    B.Strength -= CalculateOffset(B.Strength, X.Strength);
                    b++;
                }
            }
            */
            return baby;
        }

        // crosses the common genes and adds the leftover genes from the dominant
        private Entity ExclusiveMoreLike(List<Gene> dominant, List<Gene> uniqueDominant, List<Gene> common)
        {
            var baby = AbsoluteMoreLike(dominant, common);

            foreach(var gene in uniqueDominant)
            {
                //gene.Strength -= CalculateOffset(gene.Strength, gene.Strength / 2);
                baby.Genes.Add(gene);
            }

            return baby;
        }

        // crosses all the genes with bias towards dominant
        private Entity MoreLike(List<Gene> dominant, List<Gene> uniqueDominant, List<Gene> uniqueRecesive, List<Gene> common)
        {
            var baby = ExclusiveMoreLike(dominant, uniqueDominant, common);

            for (int i = 0; i < uniqueRecesive.Count; ++i)
            {
                var gene = uniqueRecesive[i];
                gene.Strength /= 2;
                gene.Strength -= CalculateOffset(gene.Strength, 0);
                baby.Genes.Add(gene);
            }

            return baby;
        }
        
        // how much needs to be substracted from x to get randomly closer to target
        private double CalculateOffset(double x, double target)
        {
            return R.NG.NextDouble() * (x - target);
        }
    }
}