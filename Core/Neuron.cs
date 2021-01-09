using System;
using System.Collections.Generic;

namespace Core
{
    public struct NeuronMutationProbabilities
    {
        public double NeuronMutationsPercentage { get; set; }
        public double NeuronCreation { get; set; }
        public double NeuronDeletion { get; set; }
        public double SynapseCreation { get; set; }
        public double SynapseDeletion { get; set; }
        public double SynapseAlteration { get; set; }
    }

    public class Neuron
    {
        public Guid Identifier { get; private set; }
        public Dictionary<Neuron, double> DendriteWeights { get; set; }
        public List<Neuron> Dendrites { get; set; }
        public List<Neuron> Axons { get; set; }
        public double Bias { get; set; }
        public double MemoryLossRate { get; set; } // https://en.wikipedia.org/wiki/Exponential_smoothing alpha factor
        public long Depth { get; set; }
        public double? Signal { get; set; }

        private Brain parentCluster;
        private static int[] pow10;
        private double memory;
        private double threshold;

        private double ThresholdAtDepth(long signalDepth)
        {
            if (pow10 == null)
            {
                pow10 = new int[10];
                pow10[0] = 1;
                for (int i = 1; i < 10; ++i)
                    pow10[i] = pow10[i - 1] * 10;
            }

            long cycleLength = signalDepth - Depth;
            // if its -1 then the neuron already fired from being called at that depth
            // if its 0 then the neuron fired at the same time as the incoming signal (absolute refactory period)
            if (cycleLength <= 0) return double.PositiveInfinity;
            if (cycleLength > 10) return 0;

            return threshold / pow10[(int)cycleLength - 1];
        }

        public Neuron(Guid guid, Brain _parentCluster)
        {
            Identifier = guid;
            DendriteWeights = new Dictionary<Neuron, double>();
            Dendrites = new List<Neuron>();
            Axons = new List<Neuron>();

            MemoryLossRate = 0.75;
            Clean();

            parentCluster = _parentCluster;
        }

        public void Clean()
        {
            Signal = null;
            threshold = 0;
            Depth = int.MinValue;
        }

        // returns true if activation was successful
        public bool Fire(long incomingSignalDepth, double? forceValue = null)
        {
            // if its -1 then the neuron already fired from being called at that depth
            // if its 0 then the neuron fired at the same time as the incoming signal (absolute refactory period)
            if (incomingSignalDepth - Depth <= 0) return false;

            // send this to gpu
            double signal = Bias;

            if (forceValue != null)
            {
                signal = forceValue.Value;
            }
            else
            {
                foreach (var dendrite in Dendrites)
                {
                    if (dendrite.Depth == incomingSignalDepth)
                        signal += dendrite.Signal.GetValueOrDefault() * DendriteWeights[dendrite];
                }
                if (DendriteWeights.ContainsKey(this))
                    signal += memory * DendriteWeights[this];
            }

            return Activate(signal, incomingSignalDepth);
        }

        private bool Activate(double signal, long depth)
        {
            double thresholdAtDepth = ThresholdAtDepth(depth);
            double potential = signal - thresholdAtDepth;

            if (potential > double.Epsilon)
            {
                memory = MemoryLossRate * signal + (1 - MemoryLossRate) * memory;

                Signal = signal;
                threshold = thresholdAtDepth + signal;
                Depth = depth + 1;
                return true;
            }

            return false;
        }

        public bool IsInputNeuron()
        {
            return DendriteWeights.ContainsKey(parentCluster.GetNeuron(Brain.InputGuid));
        }

        public bool IsOutputNeuron()
        {
            return parentCluster.GetNeuron(Brain.OutputGuid).DendriteWeights.ContainsKey(this);
        }

        // the IO reference neuron
        public bool IsRoot()
        {
            return Identifier == Brain.InputGuid ||
                   Identifier == Brain.OutputGuid;
        }

        public void Mutate(NeuronMutationProbabilities p, double mutationRate)
        {
            double chance = R.NG.NextDouble();
            if (chance > mutationRate)
                return;

            if (IsRoot())
                return;

            double percent = R.NG.NextDouble();
            if (percent < p.NeuronCreation) // creates new neuron
            {
                percent = R.NG.NextDouble();
                if (percent < .5 && Dendrites.Count > 0)
                {
                    int i;
                    Neuron dendrite = null;
                    for (i = 0, dendrite = RandomDendrite();
                        !dendrite.IsRoot() && i < 3;
                        i++, dendrite = RandomDendrite()) ;

                    SpawnNeuronFromDendrite(dendrite);
                }
                else if (Axons.Count > 0)
                {
                    int i;
                    Neuron axon = null;
                    for (i = 0, axon = RandomAxon();
                        !axon.IsRoot() && i < 3;
                        i++, axon = RandomAxon()) ;

                    SpawnNeuronFromAxon(axon);
                }
            }

            percent = R.NG.NextDouble();
            if (percent < p.SynapseDeletion / 2) // deletes dendrite
            {
                if (Dendrites.Count > 0)
                {
                    var dendrite = RandomDendrite();
                    RemoveDendrite(dendrite);
                }
            }

            percent = R.NG.NextDouble();
            if (percent < p.SynapseDeletion / 2) // deletes axon
            {
                if (Axons.Count > 0)
                {
                    var axon = RandomAxon();
                    RemoveAxon(axon);
                }
            }

            percent = R.NG.NextDouble();
            if (percent < p.SynapseAlteration / 2) // alters an existing dendrite
            {
                var neuron = RandomDendrite();
                if (neuron != null)
                {
                    if (!neuron.IsRoot())
                    {
                        if (neuron.Identifier == Identifier || R.NG.Next(Dendrites.Count + 1) == Dendrites.Count)
                            Bias = RandomSynapseStrength();
                        else
                            DendriteWeights[neuron] = RandomSynapseStrength();
                    }
                }
            }

            percent = R.NG.NextDouble();
            if (percent < p.SynapseAlteration / 2) // alters an existing axon
            {
                var neuron = RandomAxon();
                if (neuron != null)
                {
                    if (!neuron.IsRoot())
                    {
                        neuron.DendriteWeights[this] = RandomSynapseStrength();
                    }
                }
            }

            percent = R.NG.NextDouble();
            if (percent < p.SynapseCreation / 2) // dendrite creation
            {
                int i = 0;
                Neuron neuron = parentCluster.RandomNeuron();
                //TODO: Remove hardcoded values
                if (Dendrites.Count != 0)
                {
                    percent = R.NG.NextDouble();
                    if (percent < 0.51879) // creates dendrite on an existing dendrite depth
                        for (i = 0, neuron = RandomStepUp(this, 1);
                            i < 3 && DendriteWeights.ContainsKey(neuron);
                            ++i, neuron = RandomStepUp(this, 1)) ;

                    if (percent < 0.78793 || DendriteWeights.ContainsKey(neuron)) // creates dendrite on a level above
                        for (i = 0, neuron = RandomStepUp(this, 2);
                            i < 3 && DendriteWeights.ContainsKey(neuron);
                            ++i, neuron = RandomStepUp(this, 2)) ;

                    if (percent < 0.92756 || DendriteWeights.ContainsKey(neuron)) // creates dendrite on same level as this neuron
                        for (i = 0, neuron = RandomStepUp(this, 0);
                            i < 3 && DendriteWeights.ContainsKey(neuron);
                            ++i, neuron = RandomStepUp(this, 0)) ;

                    else if (DendriteWeights.ContainsKey(neuron))// creates dendrite to a random neuron
                        for (i = 0, neuron = parentCluster.RandomNeuron();
                            i < 3 && DendriteWeights.ContainsKey(neuron);
                            ++i, neuron = parentCluster.RandomNeuron()) ;
                }

                if (neuron.IsRoot())
                    neuron = parentCluster.RandomNeuron();

                CreateDendrite(neuron, RandomSynapseStrength());
            }

            percent = R.NG.NextDouble();
            if (percent < p.SynapseCreation / 2) // axon creation
            {
                Neuron neuron = parentCluster.RandomNeuron();
                int i = 0;

                if (Axons.Count != 0)
                {
                    percent = R.NG.NextDouble();
                    if (percent < 0.51879) // creates axon on an existing axon depth
                        for (i = 0, neuron = RandomStepDown(this, 1);
                            i < 3 && neuron.DendriteWeights.ContainsKey(this);
                            ++i, neuron = RandomStepDown(this, 1)) ;

                    if (percent < 0.78793 || neuron.DendriteWeights.ContainsKey(this)) // creates axon on a level bellow
                        for (i = 0, neuron = RandomStepDown(this, 2);
                            i < 3 && neuron.DendriteWeights.ContainsKey(this);
                            ++i, neuron = RandomStepDown(this, 2)) ;

                    if (percent < 0.92756 || neuron.DendriteWeights.ContainsKey(this)) // creates axon on same level as this neuron
                        for (i = 0, neuron = RandomStepDown(this, 0);
                            i < 3 && neuron.DendriteWeights.ContainsKey(this);
                            ++i, neuron = RandomStepDown(this, 0)) ;

                    if (neuron.DendriteWeights.ContainsKey(this)) // creates axon to a random neuron
                        for (i = 0, neuron = parentCluster.RandomNeuron();
                            i < 3 && neuron.DendriteWeights.ContainsKey(this);
                            ++i, neuron = parentCluster.RandomNeuron()) ;
                }

                if (neuron.IsRoot())
                    neuron = parentCluster.RandomNeuron();

                CreateAxon(neuron, RandomSynapseStrength());
            }

            percent = R.NG.NextDouble();
            if (percent < p.NeuronDeletion)
            {
                // remove neuron
                RemoveNeuron();
            }
        }

        public void ForceCreateDendrite(Neuron source, double strength)
        {
            if (DendriteWeights.ContainsKey(source))
                DendriteWeights[source] = strength;
            else
            {
                DendriteWeights.Add(source, strength);
                Dendrites.Add(source);
                source.Axons.Add(this);
            }
        }

        public void CreateDendrite(Neuron source, double strength)
        {
            // input neurons can't have more than one dendrite
            if (source == null || IsRoot() || source.IsRoot() || IsInputNeuron())
                return;

            ForceCreateDendrite(source, strength);
        }

        public void RemoveDendrite(Neuron origin)
        {
            if (origin == null || IsRoot() || origin.IsRoot())
                return;

            DendriteWeights.Remove(origin);
            Dendrites.Remove(origin);
            origin.Axons.Remove(this);
        }

        public void CreateAxon(Neuron destination, double strength)
        {
            if (destination == null || IsRoot() || destination.IsRoot() || IsOutputNeuron())
                return;

            if (destination.DendriteWeights.ContainsKey(this))
                destination.DendriteWeights[this] = strength;
            else
            {
                destination.DendriteWeights.Add(this, strength);
                destination.Dendrites.Add(this);
                Axons.Add(destination);
            }
        }

        public void RemoveAxon(Neuron destination)
        {
            if (destination == null || IsRoot() || destination.IsRoot())
                return;

            destination.DendriteWeights.Remove(this);
            destination.Dendrites.Remove(this);
            Axons.Remove(destination);
        }

        public void SpawnNeuronFromDendrite(Neuron dendrite)
        {
            if (dendrite == null || dendrite.IsRoot())
                return;

            var guid = Guid.NewGuid();

            guid = Incubator.MutationCatalog.GetOrAdd((dendrite.Identifier, Identifier), guid);

            var neuron = new Neuron(guid, parentCluster);

            if (parentCluster.RegisterNeuron(neuron))
            {
                var oldStrength = DendriteWeights[dendrite];
                RemoveDendrite(dendrite);

                neuron.CreateDendrite(dendrite, 1);
                CreateDendrite(neuron, oldStrength);
            }
        }

        public void SpawnNeuronFromAxon(Neuron axon)
        {
            if (axon == null || axon.IsRoot())
                return;

            var guid = Guid.NewGuid();

            guid = Incubator.MutationCatalog.GetOrAdd((Identifier, axon.Identifier), guid);

            var neuron = new Neuron(guid, parentCluster);

            if (parentCluster.RegisterNeuron(neuron))
            {
                var oldStrength = axon.DendriteWeights[this];
                RemoveAxon(axon);

                CreateAxon(neuron, 1);
                neuron.CreateAxon(axon, oldStrength);
            }
        }

        public void RemoveNeuron()
        {
            if (IsInputNeuron() || IsOutputNeuron() || IsRoot()) return;

            if (parentCluster.UnregisterNeuron(this))
            {
                foreach (var dendrite in Dendrites)
                {
                    foreach (var axon in Axons)
                    {
                        if (dendrite.Identifier != Identifier && axon.Identifier != Identifier)
                            axon.CreateDendrite(dendrite, DendriteWeights[dendrite] * axon.DendriteWeights[this]);
                    }

                    dendrite.Axons.Remove(this);
                }

                foreach (var axon in Axons)
                {
                    axon.Dendrites.Remove(this);
                    axon.DendriteWeights.Remove(this);
                }
            }
        }

        private Neuron RandomDendrite()
        {
            if (Dendrites.Count == 0) return null;

            return Dendrites[R.NG.Next(Dendrites.Count)];
        }

        private Neuron RandomAxon()
        {
            if (Axons.Count == 0) return null;
            return Axons[R.NG.Next(Axons.Count)];
        }

        private Neuron RandomStepUp(Neuron neuron, int step)
        {
            if (step < 0 || neuron.Dendrites.Count == 0 || neuron.IsRoot())
                return neuron.RandomAxon();

            return RandomStepUp(neuron.RandomDendrite(), step - 1);
        }

        private Neuron RandomStepDown(Neuron neuron, int step)
        {
            if (step < 0 || neuron.Axons.Count == 0 || neuron.IsRoot())
                return neuron.RandomDendrite();

            return RandomStepDown(neuron.RandomAxon(), step - 1);
        }

        public static double RandomSynapseStrength()
        {
            // random value between -1 and 1
            return R.NG.NextDouble() * 2 - 1;
        }
    }
}
