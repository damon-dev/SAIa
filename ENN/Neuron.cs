using System;
using System.Collections.Generic;
using System.Linq;

namespace EvolutionalNeuralNetwork
{
    public class Neuron
    {
        public const double MutationRate = 0.01;

        public Guid Identifier { get; private set; }
        public DateTime LastFired { get; private set; }
        public double Signal { get; private set; }
        public Dictionary<Neuron, double> Dendrites { get; set; }
        public List<Neuron> Connections { get; set; }
        public double Bias { get; set; }
        public double Recovery { get; set; } // relative refactory period recovery rate (greater = faster)

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
                double denominator = time.TotalMilliseconds * time.TotalMilliseconds * (Recovery + double.Epsilon);

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
            Recovery = 1;

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

        public void Mutate()
        {
            double chance = rand.NextDouble();
            if (chance > MutationRate)
                return;

            // Neurons connectind to the input/output reference should never mutate
            if (IsImmutable())
                return;

            int index;

            int percent = rand.Next(0, 100);
            if (percent < 10)
            {
                // remove neuron
                RemoveNeuron();
                return;
            }

            percent = rand.Next(0, 100);
            if (percent < 10)
            {   // new neuron
                CreateClone();
            }

            percent = rand.Next(0, 100);
            if (percent < 30)
            {   // remove dendrite
                if (Dendrites.Keys.Count > 0)
                {
                    var dendrites = Dendrites.Keys.ToList();
                    index = rand.Next(0, dendrites.Count);
                    RemoveSynapse(dendrites[index]);
                }
            }

            percent = rand.Next(0, 100);
            if (percent < 30)
            {   // remove synapse
                if (Connections.Count > 0)
                {
                    index = rand.Next(0, Connections.Count);
                    Connections[index].RemoveSynapse(this);
                }
            }

            percent = rand.Next(0, 100);
            if (percent < 60)
            {   // dendrite value change
                index = rand.Next(0, Dendrites.Keys.Count + 1);

                if (index == Dendrites.Keys.Count)
                    Bias = parentCluster.RandomSynapseStrength();
                else
                {
                    var dendrite = Dendrites.Keys.ElementAt(index);
                    Dendrites[dendrite] = parentCluster.RandomSynapseStrength();
                }
            }

            percent = rand.Next(0, 100);
            if (percent < 30)
            {   // new dendrite
                CreateSynapse(parentCluster.RandomNeuron(), parentCluster.RandomSynapseStrength());
            }

            percent = rand.Next(0, 100);
            if (percent < 30)
            {   // new synapse
                parentCluster.RandomNeuron().CreateSynapse(this, parentCluster.RandomSynapseStrength());
            }

            percent = rand.Next(0, 100);
            if (percent < 10)
            {
                // mutated retention
                Recovery = Math.Abs(parentCluster.RandomSynapseStrength());
            }
        }

        public void CreateSynapse(Neuron origin, double strength)
        {
            if (Dendrites.ContainsKey(origin))
                Dendrites[origin] = strength;
            else
            {
                Dendrites.Add(origin, strength);
                origin.Connections.Add(this);
            }
        }

        public void RemoveSynapse(Neuron origin)
        {
            if (origin != null && Dendrites.ContainsKey(origin))
            {
                Dendrites.Remove(origin);
                origin.Connections.Remove(this);
            }
        }

        public void CreateClone()
        {
            var neuron = new Neuron(Guid.NewGuid(), parentCluster, rand);

            if (parentCluster.RegisterNeuron(neuron))
            {
                foreach (var den in Dendrites)
                    neuron.CreateSynapse(den.Key, parentCluster.RandomSynapseStrength());

                foreach (var syn in Connections)
                    syn.CreateSynapse(neuron, parentCluster.RandomSynapseStrength());
            }
        }

        public void RemoveNeuron()
        {
            if (parentCluster.UnregisterNeuron(this))
            {
                foreach (var den in Dendrites)
                    den.Key.Connections.Remove(this);

                foreach (var syn in Connections)
                    syn.Dendrites.Remove(this);
            }
        }
    }
}
