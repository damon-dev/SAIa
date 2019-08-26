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

            Fitness = double.PositiveInfinity;
        }

        public double SharedFitness()
        {
            if (!HostCulture.SpeciesCatalog.TryGetValue(Species, out var species))
                return Fitness;

            return species.SharedFitness * species.Count;
        }

        public void Evaluate(List<Datum> features, bool cumulative, 
            Func<List<double>, List<double>, bool> successCondition)
        {
            if (cumulative)
                Fitness = Fitness * FeaturesUsed;
            else
                Fitness = FeaturesUsed = 0;

            if (features?.Count > 0)
            {
                var cluster = new Cluster();
                cluster.GenerateFromStructure(Genes);

                if (!cumulative)
                    Successful = true;

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

                    Fitness += squareSum;
                }

                FeaturesUsed += features.Count;
            }

            if (FeaturesUsed > 0)
            {
                Fitness /= FeaturesUsed;
            }
            else
            {
                Fitness = double.PositiveInfinity;
            }
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
            int smallestSpecies = int.MaxValue;

            List<(Entity original, double fitness, int count)> catalog;

            lock (HostCulture.SpeciesLock)
            {
                catalog = HostCulture.SpeciesCatalog.Values.ToList();
            }

            foreach (var species in catalog)
            {
                double compatibility = Compatibility(species.original);
                int count = species.count;

                if (compatibility < HostCulture.Cfg.SpeciationFactor
                && count > 0 && count < smallestSpecies) // finds the species with the least members
                {
                    smallestSpecies = count;
                    Species = species.original.Species;
                }
            }

            if (Species == Guid.Empty)
                Species = Guid.NewGuid();
        }

        public void Speciate(Entity mother, Entity father)
        {
            if (mother == null || father == null)
            {
                Speciate();
                return;
            }

            if (mother.Fitness > father.Fitness)
            {
                var t1 = mother;
                mother = father;
                father = t1;
            }

            Entity original = null;
            lock (HostCulture.SpeciesLock)
            {
                if (HostCulture.SpeciesCatalog.ContainsKey(mother.Species))
                    original = HostCulture.SpeciesCatalog[mother.Species].Original;
            }

            if (Compatibility(original) < HostCulture.Cfg.SpeciationFactor)
                Species = mother.Species;
            else
                Speciate();
        }

        // percentage of genes that are unique between the mates
        public double Compatibility(Entity other)
        {
            if (other == null)
                return double.MaxValue;

            double wDistance = 0;

            var motherGenes = this.Genes;
            var fatherGenes = other.Genes;

            int m = 0, f = 0;
            while (m < motherGenes.Count && f < fatherGenes.Count)
            {
                var X = motherGenes[m];
                var Y = fatherGenes[f];
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
                        wDistance += Math.Abs(X.Strength);
                        m++;
                    }
                    else
                    {
                        wDistance += Math.Abs(Y.Strength);
                        f++;
                    }
                }
                else if (X.Source.CompareTo(Y.Source) < 0)
                {
                    wDistance += Math.Abs(X.Strength);
                    m++;
                }
                else
                {
                    wDistance += Math.Abs(Y.Strength);
                    f++;
                }
            }

            while (m < motherGenes.Count)
            {
                var gene = motherGenes[m++];
                wDistance += Math.Abs(gene.Strength);
            }

            while (f < fatherGenes.Count)
            {
                var gene = fatherGenes[f++];
                wDistance += Math.Abs(gene.Strength);
            }

            return wDistance;
        }

        public Entity Copulate(Entity father, CultureConfiguration fatherCfg)
        {
            Entity mother = this;

            if (mother.Equals(father))
            {
                mother.ChildCount++;
                return new Entity(Genes);
            }

            if (mother.Fitness > father.Fitness)
            {
                var t1 = mother;
                mother = father;
                father = t1;
            }

            mother.ChildCount++;
            father.ChildCount++;

            var motherGenes = new List<Gene>(mother.Genes);
            var fatherGenes = new List<Gene>(father.Genes);

            var commonStructure = new List<Gene>();
            var motherUniqueStructure = new List<Gene>();
            var fatherUniqueStructure = new List<Gene>();

            int m = 0, f = 0;
            while (m < motherGenes.Count && f < fatherGenes.Count)
            {
                var X = motherGenes[m];
                var Y = fatherGenes[f];
                if (X.Source == Y.Source)
                {
                    if (X.Destination == Y.Destination)
                    {
                        commonStructure.Add((X.Source, Y.Destination,
                            (X.Strength + Y.Strength) / 2));

                        m++;
                        f++;
                    }
                    else if (X.Destination.CompareTo(Y.Destination) < 0)
                    {
                        motherUniqueStructure.Add(X);
                        m++;
                    }
                    else
                    {
                        fatherUniqueStructure.Add(Y);
                        f++;
                    }
                }
                else if (X.Source.CompareTo(Y.Source) < 0)
                {

                    motherUniqueStructure.Add(X);
                    m++;
                }
                else
                {
                    fatherUniqueStructure.Add(Y);
                    f++;
                }
            }

            while (m < motherGenes.Count)
            {
                var gene = motherGenes[m++];
                motherUniqueStructure.Add(gene);
            }

            while (f < fatherGenes.Count)
            {
                var gene = fatherGenes[f++];
                fatherUniqueStructure.Add(gene);
            }

            // father is the weaker one
            switch (fatherCfg.Mode)
            {
                case Modes.Grow:
                    // more like mother
                    return MoreLike(motherGenes, motherUniqueStructure, fatherUniqueStructure, commonStructure);

                case Modes.Balance:
                    // exclusive more like mother
                    return ExclusiveMoreLike(motherGenes, motherUniqueStructure, commonStructure);

                case Modes.Shrink:
                    // absolute more like mother
                    return AbsoluteMoreLike(motherGenes, motherUniqueStructure, commonStructure);

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