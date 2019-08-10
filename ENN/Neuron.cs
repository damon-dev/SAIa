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
        public double ConnectionAlteration { get; set; }
        public double ConnectionDeletion { get; set; }
        public double RandomWalk { get; set; }
        public double WalkErosion { get; set; }
        public double Bias { get; set; }
        public double Refactory { get; set; }
    }

    public class Neuron
    {
        public Guid Identifier { get; private set; }
        public DateTime LastFired { get; private set; }
        public double Signal { get; private set; }
        public Dictionary<Neuron, double> Dendrites { get; set; }
        public List<Neuron> Connections { get; set; }
        public double Bias { get; set; }
        public double Refactory { get; set; } // relative refactory period recovery rate (greater = faster)

        private double _axon;
        private double Axon
        {
            get
            {
                double temp = _axon - Signal;
                if (temp >= 0)
                {
                    _axon = temp;
                    return Signal;
                }

                return 0;
            }
            set
            {
                LastFired = DateTime.UtcNow;
                Signal = value;
                _axon = Signal * Connections.Count;
            }
        }

        private double Threshold
        {
            get
            {
                var time = DateTime.UtcNow - LastFired;
                double denominator = time.TotalMilliseconds * time.TotalMilliseconds * (Refactory + double.Epsilon);

                return (denominator <= 0) ? double.MaxValue : Signal / denominator;
            }
        }

        private Random rand;
        private Cluster parentCluster;

        public Neuron(Guid guid, Cluster _parentCluster, Random _rand)
        {
            Identifier = guid;
            Dendrites = new Dictionary<Neuron, double>();
            Connections = new List<Neuron>();
            Axon = 0;
            Refactory = 1;

            parentCluster = _parentCluster;
            rand = _rand;
        }

        // returns true if activation was successful
        public bool Fire(double? forceValue = null)
        {
            if (forceValue != null)
            {
                Axon = forceValue.Value;

                return true;
            }

            // send this to gpu
            double signal = Bias;
            
            foreach (var den in Dendrites)
                signal += den.Key.Axon * den.Value;
            //

            return Activate(signal);
        }

        public bool Activate(double signal)
        {
            signal -= Threshold;

            if (signal > 0)
            {
                Axon = signal;
                return true;
            }

            return false;
        }

        //  Either an IO neuron or the IO reference neuron
        public bool IsImmutable()
        {
            return Dendrites.Keys.Any(k => k.Identifier == Cluster.InputGuid) ||
                   Connections.Any(s => s.Identifier == Cluster.OutputGuid) ||
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
            if (percent < p.ConnectionDeletion)
            {
                RandomConnection(this)?.RemoveDendrite(this);
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
            if (percent < p.ConnectionAlteration)
            {
                double walkP = p.RandomWalk;
                Neuron neuron;

                percent = rand.NextDouble();
                if (percent < walkP) // alters an existing connection
                {
                    neuron = RandomConnection(this);
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
                origin.Connections.Add(this);
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
                origin.Connections.Remove(this);
            }
        }

        public void CreateNeuron()
        {
            var neuron = new Neuron(Guid.NewGuid(), parentCluster, rand);

            if (parentCluster.RegisterNeuron(neuron))
            {
                var sourceDendrite = RandomDendrite(this);
                var topDendrite = RandomDendrite(sourceDendrite);
                var destDendrite = RandomConnection(topDendrite);

                var sourceConnection = RandomConnection(this);
                var bottomConnection = RandomConnection(sourceConnection);
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

                    foreach (var con in Connections)
                    {
                        if (!((den.Key.IsImmutable() && con.IsImmutable()) ||
                               den.Key.Equals(this) ||
                               con.Equals(this) ||
                               con.Dendrites.ContainsKey(den.Key)))
                            con.CreateDendrite(den.Key, (den.Value + con.Dendrites[this]) / 2);
                    }
                    
                    den.Key.Connections.Remove(this);
                }

                foreach (var con in Connections)
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

        private Neuron RandomConnection(Neuron neuron)
        {
            if (neuron == null || neuron.Connections.Count == 0) return null;

            var list = neuron.Connections;
            return list[rand.Next(list.Count)];
        }

        private Neuron RandomStepUp(Neuron neuron, int step)
        {
            if (step < 0)
                return RandomConnection(neuron);

            return RandomStepUp(RandomDendrite(neuron), step - 1);
        }

        private Neuron RandomStepDown(Neuron neuron, int step)
        {
            if (step < 0)
                return RandomDendrite(neuron);

            return RandomStepDown(RandomConnection(neuron), step - 1);
        }
    }
}
