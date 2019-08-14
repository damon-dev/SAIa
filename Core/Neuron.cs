using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public struct MutationProbabilities
    {
        public double MutationRate { get; set; }
        public double NeuronCreation { get; set; }
        public double NeuronDeletion { get; set; }
        public double[] SynapseCreation { get; set; }
        public double SynapseDeletion { get; set; }
        public double SynapseAlteration { get; set; }
    }

    public class Neuron
    {
        public Guid Identifier { get; private set; }
        public Dictionary<Neuron, double> DendriteStrength { get; set; }
        public List<Neuron> Dendrites{ get; set; }
        public List<Neuron> Axons { get; set; }
        public double Bias { get; set; }
        public long Depth { get; set; }
        public double Signal { get; set; }

        private Random rand;
        private Cluster parentCluster;
    
        private static int[] pow10;
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

        public Neuron(Guid guid, Cluster _parentCluster, Random _rand)
        {
            Identifier = guid;
            DendriteStrength = new Dictionary<Neuron, double>();
            Dendrites = new List<Neuron>();
            Axons = new List<Neuron>();

            Clean();

            parentCluster = _parentCluster;
            rand = _rand;
        }

        public void Clean()
        {
            Signal = 0;
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
                signal += forceValue.Value;
            }
            else
            {
                foreach (var dendrite in Dendrites)
                {
                    if (dendrite.Depth == incomingSignalDepth)
                        signal += dendrite.Signal * DendriteStrength[dendrite];
                }
                if (DendriteStrength.ContainsKey(this))
                    signal += Signal * DendriteStrength[this];
            }

            return Activate(signal, incomingSignalDepth);
        }

        private bool Activate(double signal, long depth)
        {
            double potential = signal - ThresholdAtDepth(depth);

            if (potential > double.Epsilon)
            {
                Signal = potential;
                threshold = signal;
                Depth = depth + 1;
                return true;
            }

            return false;
        }

        public bool IsInputNeuron()
        {
            return DendriteStrength.ContainsKey(parentCluster.GetNeuron(Cluster.InputGuid));
        }

        public bool IsOutputNeuron()
        {
            return parentCluster.GetNeuron(Cluster.OutputGuid).DendriteStrength.ContainsKey(this);
        }

        // the IO reference neuron
        public bool IsRoot()
        {
            return Identifier == Cluster.InputGuid ||
                   Identifier == Cluster.OutputGuid;
        }

        public void Mutate(MutationProbabilities p)
        {
            double chance = rand.NextDouble();
            if (chance > p.MutationRate)
                return;

            if (IsRoot())
                return;

            double percent = rand.NextDouble();
            if (percent < p.SynapseDeletion / 2)
            {
                if (Dendrites.Count > 0)
                {
                    var dendrite = RandomDendrite();
                    while (dendrite.IsRoot() && Dendrites.Count > 1)
                        dendrite = RandomDendrite();

                    RemoveDendrite(dendrite);
                }
            }

            percent = rand.NextDouble();
            if (percent < p.SynapseDeletion / 2)
            {
                if (Axons.Count > 0)
                {
                    var axon = RandomAxon();
                    while (axon.IsRoot() && Axons.Count > 1)
                        axon = RandomAxon();

                    RemoveAxon(axon);
                }
            }

            percent = rand.NextDouble();
            if (percent < p.SynapseAlteration / 2) // alters an existing dendrite
            {
                var neuron = RandomDendrite();
                if (neuron != null)
                {
                    if (IsInputNeuron() && Dendrites.Count > 1)
                    {
                        while (neuron.IsRoot())
                            neuron = RandomDendrite();
                    }

                    if (!neuron.IsRoot())
                    {
                        if (neuron.Identifier == Identifier || rand.Next(Dendrites.Count + 1) == Dendrites.Count)
                            Bias = RandomSynapseStrength();
                        else
                            DendriteStrength[neuron] = RandomSynapseStrength();
                    }
                }
            }

            percent = rand.NextDouble();
            if (percent < p.SynapseCreation[0] / 2)
            {
                Neuron neuron;

                if (Dendrites.Count == 0)
                    neuron = parentCluster.RandomNeuron();
                else
                {
                    percent = rand.NextDouble();
                    if (percent < p.SynapseCreation[1]) // creates dendrite on an existing dendrite depth
                        neuron = RandomStepUp(this, 1);
                    else if (percent < p.SynapseCreation[2]) // creates dendrite on a level above
                        neuron = RandomStepUp(this, 2);
                    else if (percent < p.SynapseCreation[3]) // creates dendrite on same level as this neuron
                        neuron = RandomStepUp(this, 0);
                    else // creates dendrite to a random neuron
                        neuron = parentCluster.RandomNeuron();
                }

                if (neuron.IsRoot())
                    neuron = parentCluster.RandomNeuron();

                CreateDendrite(neuron, RandomSynapseStrength());
            }

            percent = rand.NextDouble();
            if (percent < p.SynapseAlteration / 2) // alters an existing axon
            {
                var neuron = RandomAxon();
                if (neuron != null)
                {
                    if (IsOutputNeuron() && Axons.Count > 1)
                    {
                        while (neuron.IsRoot())
                            neuron = RandomAxon();
                    }

                    if (!neuron.IsRoot())
                    {
                        neuron.DendriteStrength[this] = RandomSynapseStrength();
                    }
                }
            }

            percent = rand.NextDouble();
            if (percent < p.SynapseCreation[0] / 2)
            {
                Neuron neuron;

                if (Axons.Count == 0)
                    neuron = parentCluster.RandomNeuron();
                else
                {
                    percent = rand.NextDouble();
                    if (percent < p.SynapseCreation[1]) // creates axon on an existing axon depth
                        neuron = RandomStepDown(this, 1);
                    else if (percent < p.SynapseCreation[2]) // creates axon on a level bellow
                        neuron = RandomStepDown(this, 2);
                    else if (percent < p.SynapseCreation[3]) // creates axon on same level as this neuron
                        neuron = RandomStepDown(this, 0);
                    else // creates axon to a random neuron
                        neuron = parentCluster.RandomNeuron();
                }

                if (neuron.IsRoot())
                    neuron = parentCluster.RandomNeuron();

                CreateAxon(neuron, RandomSynapseStrength());
            }

            percent = rand.NextDouble();
            if (percent < p.NeuronCreation)
            {
                percent = rand.NextDouble();
                if (percent < 0.4 && !IsInputNeuron() && !IsOutputNeuron()) // creates neuron on the same depth as this one
                    SpawnNeuronFromNeuron();
                else if (percent < 0.7 && !IsInputNeuron())
                    SpawnNeuronFromDendrite(RandomDendrite());
                else if (!IsOutputNeuron())
                    SpawnNeuronFromAxon(RandomAxon());
            }

            percent = rand.NextDouble();
            if (percent < p.NeuronDeletion)
            {
                // remove neuron
                RemoveNeuron();
            }
        }

        public void ForceCreateDendrite(Neuron source, double strength)
        {
            if (DendriteStrength.ContainsKey(source))
                DendriteStrength[source] = strength;
            else
            {
                DendriteStrength.Add(source, strength);
                Dendrites.Add(source);
                source.Axons.Add(this);
            }
        }

        public void CreateDendrite(Neuron source, double strength)
        {
            if (source == null || IsRoot() || source.IsRoot())
                return;

            ForceCreateDendrite(source, strength);
        }

        public void RemoveDendrite(Neuron origin)
        {
            if (origin == null || IsRoot() || origin.IsRoot())
                return;

            DendriteStrength.Remove(origin);
            Dendrites.Remove(origin);
            origin.Axons.Remove(this);
        }

        public void CreateAxon(Neuron destination, double strength)
        {
            if (destination == null || IsRoot() || destination.IsRoot())
                return;

            if (destination.DendriteStrength.ContainsKey(this))
                destination.DendriteStrength[this] = strength;
            else
            {
                destination.DendriteStrength.Add(this, strength);
                destination.Dendrites.Add(this);
                Axons.Add(destination);
            }
        }

        public void RemoveAxon(Neuron destination)
        {
            if (destination == null || IsRoot() || destination.IsRoot())
                return;

            destination.DendriteStrength.Remove(this);
            destination.Dendrites.Remove(this);
            Axons.Remove(destination);
        }

        public void SpawnNeuronFromNeuron()
        {
            if (IsInputNeuron() || IsOutputNeuron())
                return;

            var neuron = new Neuron(Guid.NewGuid(), parentCluster, rand);

            if (parentCluster.RegisterNeuron(neuron))
            {
                var dendrite = RandomDendrite();
                var axon = RandomAxon();

                if (dendrite != null)
                {
                    DendriteStrength[dendrite] /= 2;
                    neuron.CreateDendrite(dendrite, DendriteStrength[dendrite]);
                }

                if (axon != null)
                {
                    neuron.CreateAxon(axon, axon.DendriteStrength[this]);
                }
            }
        }

        public void SpawnNeuronFromDendrite(Neuron dendrite)
        {
            if (dendrite == null || dendrite.IsRoot())
                return;

            var neuron = new Neuron(Guid.NewGuid(), parentCluster, rand);

            if (parentCluster.RegisterNeuron(neuron))
            {
                var oldStrength = DendriteStrength[dendrite];
                RemoveDendrite(dendrite);

                neuron.CreateDendrite(dendrite, 1);
                CreateDendrite(neuron, oldStrength);
            }
        }

        public void SpawnNeuronFromAxon(Neuron axon)
        {
            if (axon == null || axon.IsRoot())
                return;

            var neuron = new Neuron(Guid.NewGuid(), parentCluster, rand);

            if (parentCluster.RegisterNeuron(neuron))
            {
                var oldStrength = axon.DendriteStrength[this];
                RemoveAxon(axon);

                CreateAxon(neuron, 1);
                neuron.CreateAxon(axon, oldStrength);
            }
        }

        public void RemoveNeuron()
        {
            if (IsInputNeuron() || IsOutputNeuron()) return;

            if (parentCluster.UnregisterNeuron(this))
            {
                foreach (var dendrite in Dendrites)
                {
                    foreach (var axon in Axons)
                    {
                        if (dendrite.Identifier != Identifier && axon.Identifier != Identifier)
                            axon.CreateDendrite(dendrite, DendriteStrength[dendrite] * axon.DendriteStrength[this]);
                    }

                    dendrite.Axons.Remove(this);
                }

                foreach (var axon in Axons)
                {
                    axon.Dendrites.Remove(this);
                    axon.DendriteStrength.Remove(this);
                }
            }
        }

        private Neuron RandomDendrite()
        {
            if (Dendrites.Count == 0) return null;

            return Dendrites[rand.Next(Dendrites.Count)];
        }

        private Neuron RandomAxon()
        {
            if (Axons.Count == 0) return null;
            return Axons[rand.Next(Axons.Count)];
        }

        private Neuron RandomStepUp(Neuron neuron, int step)
        {
            if (step < 0 || neuron.Dendrites.Count == 0)
                return neuron.RandomAxon();

            return RandomStepUp(neuron.RandomDendrite(), step - 1);
        }

        private Neuron RandomStepDown(Neuron neuron, int step)
        {
            if (step < 0 || neuron.Axons.Count == 0)
                return neuron.RandomDendrite();

            return RandomStepDown(neuron.RandomAxon(), step - 1);
        }

        public static double RandomSynapseStrength(Random rand)
        {
            // random value between -1 and 1
            return rand.NextDouble() * 2 - 1;
        }

        public double RandomSynapseStrength()
        {
            // random value between -1 and 1
            return rand.NextDouble() * 2 - 1;
        }
    }
}
