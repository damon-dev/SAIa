using System;
using System.Collections.Generic;

namespace NeuralNetExp
{
    public class Neuron
    {
        public Guid Identifier { get; private set; }
        public Dictionary<Neuron, double> Dendrites { get; set; }
        public List<Neuron> Synapses { get; set; }
        public double Axon { get; set; }

        public Neuron(Guid guid)
        {
            Identifier = guid;
            Dendrites = new Dictionary<Neuron, double>();
            Synapses = new List<Neuron>();
        }

        public bool Fire()
        {
            double signal = 0;

            foreach(var den in Dendrites)
                signal += den.Key.Axon * den.Value;

            Axon = Activate(signal);

            return Axon > 0;
        }

        public double Activate(double signal)
        {
            // RElu

            if (signal < 0) return 0;

            return signal;
        }

        public void CreateSynapse(Neuron origin, double strength)
        {
            if (Dendrites.ContainsKey(origin))
                Dendrites[origin] = strength;
            else
            {
                Dendrites.Add(origin, strength);
                origin.Synapses.Add(this);
            }
        }
    }
}
