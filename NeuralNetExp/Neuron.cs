using System;
using System.Collections.Generic;
using System.Linq;

namespace EvolutionalNeuralNetwork
{
    public class Neuron
    {
        public const double MutationRate = 0.01;

        public Guid Identifier { get; private set; }
        public Dictionary<Neuron, double> Dendrites { get; set; }
        public List<Neuron> Connections { get; set; }
        public DateTime LastFired { get; private set; }
        public double Bias { get; set; }
        public double Retention { get; set; } // the greater it is the longer the neuron preserves a value
        public double TrueAxon { get; private set; }
        public double Axon
        {
            get
            {
                var diff = DateTime.UtcNow - LastFired;

                if (Retention <= 0) return 0;

                double coef = diff.TotalDays * diff.TotalDays * 1/Retention;
                if (coef <= 1) return TrueAxon;

                return TrueAxon / coef;
            }
            set
            {
                LastFired = DateTime.UtcNow;
                TrueAxon = value;
            }
        }

        private Random rand;
        private Cluster parentCluster;

        public Neuron(Guid guid, Cluster _parentCluster)
        {
            Identifier = guid;
            Dendrites = new Dictionary<Neuron, double>();
            Connections = new List<Neuron>();
            Axon = 0; // important to have at 0

            parentCluster = _parentCluster;
            rand = new Random();
        }

        // returns true if activation was successful
        public bool Fire(double? forceValue = null)
        {
            if (forceValue != null)
            {
                Axon = forceValue.Value;

                return true;
            }

            double signal = Bias;
            
            foreach (var den in Dendrites)
                signal += den.Key.Axon * den.Value;

            return Activate(signal);
        }

        public bool Activate(double signal)
        {
            if (signal > Axon)
            {
                Axon = Math.Max(0, signal);
                return Axon > 0;
            }

            return false;
        }

        public void Mutate()
        {
            double chance = rand.NextDouble();
            if (chance > MutationRate)
                return;

            // Neurons connectind to the input/output refference should never mutate
            if (Dendrites.Keys.Any(k => k.Identifier == Cluster.InputGuid) ||
                Connections.Any(s => s.Identifier == Cluster.OutputGuid) ||
                Identifier == Cluster.InputGuid ||
                Identifier == Cluster.OutputGuid)
                return;

            int index;

            int percent = rand.Next(0, 100);
            if (percent < 5)
            {
                // remove neuron
                RemoveNeuron();
                return;
            }

            percent = rand.Next(0, 100);
            if (percent < 5)
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
            if (percent < 80)
            {   // dendrite value change
                index = rand.Next(0, Dendrites.Keys.Count + 1);

                if (index == Dendrites.Keys.Count)
                    Bias = Cluster.RandomSynapseStrength();
                else
                {
                    var dendrite = Dendrites.Keys.ElementAt(index);
                    Dendrites[dendrite] = Cluster.RandomSynapseStrength();
                }
            }

            percent = rand.Next(0, 100);
            if (percent < 30)
            {   // new dendrite
                CreateSynapse(parentCluster.RandomNeuron(), Cluster.RandomSynapseStrength());
            }

            percent = rand.Next(0, 100);
            if (percent < 30)
            {   // new synapse
                parentCluster.RandomNeuron().CreateSynapse(this, Cluster.RandomSynapseStrength());
            }

            percent = rand.Next(0, 100);
            if (percent < 10)
            {
                // mutated retention
                Retention = Math.Abs(Cluster.RandomSynapseStrength());
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
            var neuron = new Neuron(Guid.NewGuid(), parentCluster);

            parentCluster.AddNeuron(neuron);

            foreach (var den in Dendrites)
                neuron.CreateSynapse(den.Key, den.Value);

            foreach(var syn in Connections)
                syn.CreateSynapse(neuron, syn.Dendrites[this]);
        }

        public void RemoveNeuron()
        {
            foreach (var den in Dendrites)
                den.Key.Connections.Remove(this);

            foreach (var syn in Connections)
                syn.Dendrites.Remove(this);

            parentCluster.RemoveNeuron(this);
        }
    }
}
