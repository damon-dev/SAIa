using System;
using System.Collections.Generic;

namespace EvolutionalNeuralNetwork
{
    // Neuron 0 is the neuron containing the input in the axon
    // Neuron 1 is the neuron containing the output in the dendrites
    public class Cluster
    {
        public static readonly Guid InputGuid = new Guid("8478a94f-5a4a-4d6c-a3f8-16b7fb4ad2c6");
        public static readonly Guid OutputGuid = new Guid("7bd1acb4-07ba-4838-be56-237d3391b61f");

        public List<Gene> Structure;

        public Dictionary<Guid, Neuron> Neurons;

        public Cluster(List<Gene> structure)
        {
            Structure = new List<Gene>(structure);

            var labels = new HashSet<int>();

            Neurons = new Dictionary<Guid, Neuron>();

            GenerateFromStructure();
        }

        public double Querry(List<double> inputs)
        {
            // this is hacked improve later

            for (int i = 0; i < inputs.Count && i < Neurons[InputGuid].Synapses.Count; ++i)
            {
                var inputNeuron = Neurons[InputGuid].Synapses[i];

                inputNeuron.Dendrites[Neurons[InputGuid]] = inputs[i];
            }

            Propagate();

            Neuron outputNeuron = null;
            foreach (var key in Neurons[OutputGuid].Dendrites.Keys)
            {
                outputNeuron = key;
                break;
            }
            if (outputNeuron == null) return 0;

            return outputNeuron.Axon;
        }

        private void GenerateFromStructure()
        {
            // Adding the input
            Neurons.Add(InputGuid, new Neuron(InputGuid));

            // Adding the output
            Neurons.Add(OutputGuid, new Neuron(OutputGuid));

            foreach(var elem in Structure)
            {
                Guid source = elem.Source;
                Guid dest = elem.Destination;
                double strength = elem.Strength;

                if (!Neurons.ContainsKey(source))
                    Neurons.Add(source, new Neuron(source));

                if (!Neurons.ContainsKey(dest))
                    Neurons.Add(dest, new Neuron(dest));

                Neurons[dest].CreateSynapse(Neurons[source], strength);
            }
        }

        /// <summary>
        /// Fires all neurons in the cluster BFS style.
        /// </summary>
        /// <returns>True if the graph has a cycle, false if the graph is a tree.</returns>
        private void Propagate()
        {
            var fired = new HashSet<Neuron>();
            var queue = new Queue<Neuron>();

            fired.Add(Neurons[InputGuid]);
            fired.Add(Neurons[OutputGuid]);

            foreach(var successor in Neurons[InputGuid].Synapses)
                queue.Enqueue(successor);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (fired.Contains(current)) continue;

                if (current.Fire())
                {
                    fired.Add(current);

                    foreach (var successor in current.Synapses)
                    {
                        if (!fired.Contains(successor))
                            queue.Enqueue(successor);
                    }
                } 
            }
        }
    }
}
