using System;
using System.Collections.Generic;
using System.Linq;

namespace EvolutionalNeuralNetwork
{
    public struct MutationProbabilities
    {
        public double MutationRate { get; set; }
        public double NeuronCreation { get; set; }
        public double NeuronDeletion { get; set; } 
        public double DendriteAlteration { get; set; } 
        public double DendriteDeletion { get; set; } 
        public double AxonAlteration { get; set; }
        public double AxonDeletion { get; set; }
        public double RandomWalk { get; set; }
        public double WalkErosion { get; set; }
        public double Bias { get; set; }
        public double Refactory { get; set; }
    }

    public class Neuron
    {
        public Guid Identifier { get; private set; }
        public Dictionary<Neuron, double> Dendrites { get; set; }
        public List<Neuron> Axons { get; set; }
        public double Bias { get; set; }
        public double Refactory { get; set; } // relative refactory period recovery rate (greater = faster)
        public double Depth { get; set; }
        public double Signal { get; set; }

        private double Threshold(double incomingSignalDepth)
        {
            double cycleLength = incomingSignalDepth - Depth;
            if (cycleLength <= 0) return double.PositiveInfinity;

            return Signal / cycleLength * (Refactory + double.Epsilon);
        }

        private Random rand;
        private Cluster parentCluster;

        public Neuron(Guid guid, Cluster _parentCluster, Random _rand)
        {
            Identifier = guid;
            Dendrites = new Dictionary<Neuron, double>();
            Axons = new List<Neuron>();
            Depth = -1;
            Signal = 0;
            Refactory = 1;

            parentCluster = _parentCluster;
            rand = _rand;
        }

        // returns true if activation was successful
        public bool Fire(double incomingSignalDepth, double? forceValue = null)
        {
            if (forceValue != null)
            {
                Signal = forceValue.Value;
                Depth = incomingSignalDepth + 1;

                return true;
            }

            // send this to gpu
            double signal = Bias;

            foreach (var den in Dendrites)
            {
                if (den.Key.Depth == incomingSignalDepth)
                    signal += den.Key.Signal * den.Value;
            }
            if (Dendrites.ContainsKey(this)) // EXPERIMENTAL
                signal += Signal * Dendrites[this];
            

            return Activate(signal, incomingSignalDepth);
        }

        private bool Activate(double signal, double depth)
        {
            signal -= Threshold(depth);

            if (signal > 0)
            {
                Signal = signal;
                Depth = depth + 1;
                return true;
            }

            return false;
        }

        //  Either an IO neuron or the IO reference neuron
        public bool IsImmutable()
        {
            return Dendrites.Keys.Any(k => k.Identifier == Cluster.InputGuid) ||
                   Axons.Any(s => s.Identifier == Cluster.OutputGuid) ||
                   Identifier == Cluster.InputGuid ||
                   Identifier == Cluster.OutputGuid;
        }

        public void Mutate(MutationProbabilities p)
        {
            double chance = rand.NextDouble();
            if (chance > p.MutationRate)
                return;

            // Neurons connectind to the input/output reference should never mutate
            if (IsImmutable())
                return;

            double percent = rand.NextDouble();
            if (percent < p.NeuronDeletion)
            {
                // remove neuron
                RemoveNeuron();
                return;
            }

            percent = rand.NextDouble();
            if (percent < p.NeuronCreation)
            {   // new neuron
                CreateNeuron();
            }

            percent = rand.NextDouble();
            if (percent < p.DendriteDeletion)
            {
                RemoveDendrite(RandomDendrite(this));
            }

            percent = rand.NextDouble();
            if (percent < p.AxonDeletion)
            {
                RandomAxon(this)?.RemoveDendrite(this);
            }

            percent = rand.NextDouble();
            if (percent < p.DendriteAlteration)
            {
                double walkP = p.RandomWalk;
                Neuron neuron;

                percent = rand.NextDouble();
                if (percent < walkP) // alters an existing dendrite
                {
                    neuron = RandomDendrite(this);
                    if (neuron != null) Dendrites[neuron] = parentCluster.RandomSynapseStrength();
                    walkP /= p.WalkErosion;
                }

                percent = rand.NextDouble();
                if (percent < walkP) // creates dendrite on an existing dendrite level
                {
                    neuron = RandomStepUp(this, 1);
                    if (neuron != null) CreateDendrite(neuron, parentCluster.RandomSynapseStrength());
                    walkP /= p.WalkErosion;
                }

                percent = rand.NextDouble();
                if (percent < walkP) // creates dendrite on same level as this neuron
                {
                    neuron = RandomStepUp(this, 0);
                    if (neuron != null) CreateDendrite(neuron, parentCluster.RandomSynapseStrength());
                    walkP /= p.WalkErosion;
                }

                percent = rand.NextDouble();
                if (percent < walkP) // creates dendrite on an existing connection level
                {
                    neuron = RandomStepDown(this, 1);
                    if (neuron != null) CreateDendrite(neuron, parentCluster.RandomSynapseStrength());
                    walkP /= p.WalkErosion;
                }
                percent = rand.NextDouble();
                if (percent < walkP) // creates dendrite to a random neuron
                {
                    neuron = parentCluster.RandomNeuron();
                    CreateDendrite(neuron, parentCluster.RandomSynapseStrength());
                }
            }

            percent = rand.NextDouble();
            if (percent < p.AxonAlteration)
            {
                double walkP = p.RandomWalk;
                Neuron neuron;

                percent = rand.NextDouble();
                if (percent < walkP) // alters an existing connection
                {
                    neuron = RandomAxon(this);
                    if (neuron != null) neuron.Dendrites[this] = parentCluster.RandomSynapseStrength();
                    walkP /= p.WalkErosion;
                }

                percent = rand.NextDouble();
                if (percent < walkP) // creates connection on an existing connection level
                {
                    neuron = RandomStepDown(this, 1);
                    neuron?.CreateDendrite(this, parentCluster.RandomSynapseStrength());
                    walkP /= p.WalkErosion;
                }

                percent = rand.NextDouble();
                if (percent < walkP) // creates connection on same level as this neuron
                {
                    neuron = RandomStepDown(this, 0);
                    neuron?.CreateDendrite(this, parentCluster.RandomSynapseStrength());
                    walkP /= p.WalkErosion;
                }
                 
                percent = rand.NextDouble();
                if (percent < walkP) // creates connection on an existing dendrite level
                {
                    neuron = RandomStepUp(this, 1);
                    neuron?.CreateDendrite(this, parentCluster.RandomSynapseStrength());
                    walkP /= p.WalkErosion;
                }
                
                if (percent < walkP)// creates connection to a random neuron
                {
                    neuron = parentCluster.RandomNeuron();
                    neuron?.CreateDendrite(this, parentCluster.RandomSynapseStrength());
                }
            }

            percent = rand.NextDouble();
            if (percent < p.Bias)
            {
                // mutated bias
                Bias = parentCluster.RandomSynapseStrength();
            }

            percent = rand.NextDouble();
            if (percent < p.Refactory)
            {
                // mutated retention
                Refactory = Math.Abs(parentCluster.RandomSynapseStrength());
            }
        }

        public void ForceCreateDendrite(Neuron origin, double strength)
        {
            if (Dendrites.ContainsKey(origin))
                Dendrites[origin] = strength;
            else
            {
                Dendrites.Add(origin, strength);
                origin.Axons.Add(this);
            }
        }

        public void CreateDendrite(Neuron origin, double strength)
        {
            if (origin.Identifier == Cluster.InputGuid ||
                origin.Identifier == Cluster.OutputGuid ||
                this.Identifier == Cluster.InputGuid ||
                this.Identifier == Cluster.OutputGuid)
                return;

            ForceCreateDendrite(origin, strength);
        }

        public void RemoveDendrite(Neuron origin)
        {
            if (origin != null && Dendrites.ContainsKey(origin))
            {
                Dendrites.Remove(origin);
                origin.Axons.Remove(this);
            }
        }

        public void CreateNeuron()
        {
            var neuron = new Neuron(Guid.NewGuid(), parentCluster, rand);

            if (parentCluster.RegisterNeuron(neuron))
            {
                var sourceDendrite = RandomDendrite(this);
                var topDendrite = RandomDendrite(sourceDendrite);
                var destDendrite = RandomAxon(topDendrite);

                var sourceConnection = RandomAxon(this);
                var bottomConnection = RandomAxon(sourceConnection);
                var destConnection = RandomDendrite(bottomConnection);

                if (destConnection == null || destDendrite == null) return;

                neuron.CreateDendrite(destDendrite, Dendrites[sourceDendrite]);
                destConnection.CreateDendrite(neuron, sourceConnection.Dendrites[this]);
            }
        }

        public void RemoveNeuron()
        {
            if (parentCluster.UnregisterNeuron(this))
            {
                foreach (var den in Dendrites)
                {

                    foreach (var con in Axons)
                    {
                        if (!((den.Key.IsImmutable() && con.IsImmutable()) ||
                               den.Key.Equals(this) ||
                               con.Equals(this) ||
                               con.Dendrites.ContainsKey(den.Key)))
                            con.CreateDendrite(den.Key, (den.Value + con.Dendrites[this]) / 2);
                    }
                    
                    den.Key.Axons.Remove(this);
                }

                foreach (var con in Axons)
                {
                    con.Dendrites.Remove(this);
                }
            }
        }

        private Neuron RandomDendrite(Neuron neuron)
        {
            if (neuron == null || neuron.Dendrites.Keys.Count == 0) return null;

            var list = neuron.Dendrites.Keys.ToList();
            return list[rand.Next(list.Count)];
        }

        private Neuron RandomAxon(Neuron neuron)
        {
            if (neuron == null || neuron.Axons.Count == 0) return null;

            var list = neuron.Axons;
            return list[rand.Next(list.Count)];
        }

        private Neuron RandomStepUp(Neuron neuron, int step)
        {
            if (step < 0)
                return RandomAxon(neuron);

            return RandomStepUp(RandomDendrite(neuron), step - 1);
        }

        private Neuron RandomStepDown(Neuron neuron, int step)
        {
            if (step < 0)
                return RandomDendrite(neuron);

            return RandomStepDown(RandomAxon(neuron), step - 1);
        }
    }
}
