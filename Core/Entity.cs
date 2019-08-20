using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Core
{
    [JsonObject]
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
        public double Mean { get; private set; }
        [JsonProperty]
        public int FeaturesUsed { get; private set; }
        [JsonProperty]
        public int ChildCount { get; private set; }
        [JsonProperty]
        public bool Positive { get; private set; }

        public double Fitness { get; private set; } // the smaller the better
        public int NeuronCount { get; private set; }
        public int SynapseCount { get; private set; }
        public int InputSize { get; private set; }
        public int OutputSize { get; private set; }

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

            Mean = double.PositiveInfinity;
            Fitness = double.PositiveInfinity;
        }

        public double ComputeFitness(Func<Entity, int, double> fitnessFunction)
        {
            if (FeaturesUsed == 0)
                return double.PositiveInfinity;

            return fitnessFunction(this, FeaturesUsed);
        }

        public void Evaluate(List<Datum> features, Func<Entity, int, double> fitnessFunction, bool cumulative, 
            Func<List<double>, List<double>, bool> successCondition)
        {
            if (cumulative)
                Mean = Mean * FeaturesUsed;
            else
                Mean = FeaturesUsed = 0;

            if (features?.Count > 0)
            {
                var cluster = new Cluster();
                cluster.GenerateFromStructure(Genes);

                if (!cumulative)
                    Positive = true;

                for (int i = 0; i < features.Count; ++i)
                {
                    var input = features[i].Input;
                    var expectedOutput = features[i].Output;

                    double squareSum = 0;

                    var predictedOutput = cluster.Querry(input, out long steps);
                    cluster.Nap();

                    if (steps == -1)
                    {
                        Mean = double.PositiveInfinity;
                        Positive = false;
                        return;
                    }
                    else
                    {
                        for (int j = 0; j < expectedOutput.Count; ++j)
                            squareSum += (predictedOutput[j] - expectedOutput[j]) * (predictedOutput[j] - expectedOutput[j]);
                        
                        Positive = Positive && successCondition(expectedOutput, predictedOutput);
                        squareSum /= expectedOutput.Count;
                    }

                    Mean += squareSum;
                }

                FeaturesUsed += features.Count;
            }

            if (FeaturesUsed > 0)
            {
                Mean /= FeaturesUsed;
                Fitness = fitnessFunction(this, FeaturesUsed);
            }
            else
            {
                Mean = double.PositiveInfinity;
                Fitness = double.PositiveInfinity;
            }
        }

        public void Mutate(NeuronMutationProbabilities p, double mutationRate)
        {
            var cluster = new Cluster();
            cluster.GenerateFromStructure(Genes);
            Genes = cluster.Mutate(p, mutationRate);
            NeuronCount = cluster.NeuronCount;
            SynapseCount = cluster.SynapseCount;

            // TODO: these shouldn't change ever, check if do
            InputSize = cluster.InputSize;
            OutputSize = cluster.OutputSize;
        }

        // percentage of genes that are unique between the mates
        public double Compatibility(Entity mate)
        {
            double commonGenes = 0;

            var motherGenes = this.Genes;
            var fatherGenes = mate.Genes;

            int m = 0, f = 0;
            while (m < motherGenes.Count && f < fatherGenes.Count)
            {
                var X = motherGenes[m];
                var Y = fatherGenes[f];
                if (X.Source == Y.Source)
                {
                    if (X.Destination == Y.Destination)
                    {
                        commonGenes++;
                        m++;
                        f++;
                    }
                    else if (X.Destination.CompareTo(Y.Destination) < 0)
                        m++;
                    else
                        f++;
                }
                else if (X.Source.CompareTo(Y.Source) < 0)
                    m++;
                else
                    f++;
            }

            return (motherGenes.Count + fatherGenes.Count - 2 * commonGenes) / 
                   (motherGenes.Count + fatherGenes.Count);
        }

        public Entity Copulate(Entity father, CultureConfiguration fatherCfg)
        {
            Entity mother = this;

            if (mother.ComputeFitness(fatherCfg.FitnessFunction) > father.Fitness)
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