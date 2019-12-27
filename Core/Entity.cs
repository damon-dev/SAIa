using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public double Fitness { get; private set; }
        [JsonProperty]
        public int FeaturesUsed { get; private set; }
        [JsonProperty]
        public int ChildCount { get; private set; }
        [JsonProperty]
        public Guid Species { get; set; }

        public Culture HostCulture { get; set; }

        public int NeuronCount { get; private set; }
        public int SynapseCount { get; private set; }

        public int InputSize { get; private set; }
        public int OutputSize { get; private set; }

        public bool Successful { get; private set; }

        public Entity() { }

        public Entity(List<Gene> structure)
        {
            Genes = new List<Gene>(structure);

            var cluster = new Cluster();
            cluster.GenerateFromStructure(Genes);
            NeuronCount = cluster.NeuronCount;
            SynapseCount = cluster.SynapseCount;

            InputSize = cluster.InputSize;
            OutputSize = cluster.OutputSize;

            Fitness = 0;
        }

        public double SharedFitness()
        {
            if (!HostCulture.SpeciesCatalog.TryGetValue(Species, out var species) 
            || species.Count <= 0)
                return Fitness;

            return species.SharedFitness / species.Count;
        }

        public void Evaluate(List<Datum> features, bool cumulative,
            Func<List<double>, List<double>, bool> successCondition)
        {
            if (features == null || features.Count == 0) return;

            var cluster = new Cluster();
            cluster.GenerateFromStructure(Genes);

            double meanSquareSum;

            if (cumulative)
            {
                meanSquareSum = Fitness == 0 ? double.PositiveInfinity : FeaturesUsed / Fitness;
            }
            else
            {
                meanSquareSum = FeaturesUsed = 0;
                Successful = true;
            }

            for (int i = 0; i < features.Count; ++i)
            {
                var input = features[i].Input;
                var expectedOutput = features[i].Output;

                double squareSum = 0;

                var predictedOutput = cluster.Querry(input, out long steps);
                cluster.Nap();

                if (steps == -1)
                {
                    squareSum = 1;
                    Successful = false;
                }
                else
                {
                    for (int j = 0; j < expectedOutput.Count; ++j)
                        squareSum += (predictedOutput[j] - expectedOutput[j]) * (predictedOutput[j] - expectedOutput[j]);

                    Successful = Successful && successCondition(expectedOutput, predictedOutput);
                    squareSum /= expectedOutput.Count;
                }

                meanSquareSum += squareSum;
            }

            FeaturesUsed += features.Count;

            Fitness = meanSquareSum > 0 ? FeaturesUsed / meanSquareSum : double.PositiveInfinity;
        }

        public bool Mutate(NeuronMutationProbabilities p, double mutationRate)
        {
            var cluster = new Cluster();
            cluster.GenerateFromStructure(Genes);

            Genes = cluster.Mutate(p, mutationRate, out bool hasMutated);

            NeuronCount = cluster.NeuronCount;
            SynapseCount = cluster.SynapseCount;

            // TODO: these shouldn't change ever, check if do
            InputSize = cluster.InputSize;
            OutputSize = cluster.OutputSize;

            return hasMutated;
        }

        public void Speciate()
        {
            int lowestCount = int.MaxValue;
            double factor;
            List<(Entity original, double sharedFitness, int count)> catalog;

            lock (HostCulture.SpeciesLock)
            {
                catalog = new List<(Entity, double, int)>(HostCulture.SpeciesCatalog.Values);
                factor = HostCulture.Cfg.SpeciationFactor;
            }

            foreach (var (original, sharedFitness, count) in catalog)
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
            if (HostCulture.SpeciesCatalog.TryGetValue(strongMate.Species, out (Entity, double, int) species))
                original = species.Item1;

            if (original == null)
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

        // percentage of genes that are unique between the mates
        public double Compatibility(Entity other)
        {
            if (other == null)
                return double.MaxValue;

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
                        wDistance += Math.Abs(X.Strength - Y.Strength);

                        m++;
                        f++;
                    }
                    else if (X.Destination.CompareTo(Y.Destination) < 0)
                    {
                        wDistance += Math.Abs(X.Strength) + 1;
                        m++;
                    }
                    else
                    {
                        wDistance += Math.Abs(Y.Strength) + 1;
                        f++;
                    }
                }
                else if (X.Source.CompareTo(Y.Source) < 0)
                {
                    wDistance += Math.Abs(X.Strength) + 1;
                    m++;
                }
                else
                {
                    wDistance += Math.Abs(Y.Strength) + 1;
                    f++;
                }
            }

            while (m < thisGenes.Count)
            {
                var gene = thisGenes[m++];
                wDistance += Math.Abs(gene.Strength) + 1;
            }

            while (f < otherGenes.Count)
            {
                var gene = otherGenes[f++];
                wDistance += Math.Abs(gene.Strength) + 1;
            }

            return wDistance;
        }

        public Entity Copulate(Entity weak, CultureConfiguration cfg)
        {
            Entity strong = this;

            if (strong.Equals(weak))
            {
                strong.ChildCount++;
                return new Entity(Genes);
            }

            strong.ChildCount++;
            weak.ChildCount++;

            var strongGenes = new List<Gene>(strong.Genes);
            var weakGenes = new List<Gene>(weak.Genes);

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
                        commonGenes.Add((X.Source, Y.Destination,
                            (X.Strength + Y.Strength) / 2));

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
        }

        private Entity AbsoluteMoreLike(List<Gene> dominant, List<Gene> uniqueDominant, List<Gene> common)
        {
            var baby = new Entity(common);

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

            return baby;
        }

        private Entity ExclusiveMoreLike(List<Gene> dominant, List<Gene> uniqueDominant, List<Gene> common)
        {
            var baby = AbsoluteMoreLike(dominant, uniqueDominant, common);

            for (int i = 0; i < uniqueDominant.Count; ++i)
            {
                var gene = uniqueDominant[i];
                gene.Strength -= CalculateOffset(gene.Strength, gene.Strength / 2);
                baby.Genes.Add(gene);
            }

            return baby;
        }

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