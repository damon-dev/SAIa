using System;
using System.Collections.Generic;

namespace Core
{
    public class Cluster
    {
        public static readonly Guid InputGuid = new Guid("8478a94f-5a4a-4d6c-a3f8-16b7fb4ad2c6");
        public static readonly Guid OutputGuid = new Guid("7bd1acb4-07ba-4838-be56-237d3391b61f");
        public static readonly Guid SeedGuid = new Guid("e3ea29b5-493c-48a6-9c94-c7b418b6d732");
        public static readonly Guid BiasMark = new Guid("d579d9f1-cd6f-4236-9a66-69115ae170d3");

        // Structural description of the cluster, always sorted
        private List<Gene> _structure;
        public List<Gene> Structure
        {
            get
            {
                return _structure;
            }
            private set
            {
                _structure = value;
                _structure.Sort();
            }
        }

        public int SynapseCount { get; private set; }
        public int InputSize => neurons[InputGuid].Axons.Count;
        public int OutputSize => neurons[OutputGuid].Dendrites.Count;
        public int NeuronCount => neuronGuids.Count;

        private List<Guid> neuronGuids;
        private Dictionary<Guid, Neuron> neurons;

        private readonly Random rand;

        public Cluster()
        {
            neurons = new Dictionary<Guid, Neuron>();
            neuronGuids = new List<Guid>();
            rand = new Random();
        }

        public void GenerateFromStructure(List<Gene> structure)
        {
            Structure = new List<Gene>(structure);

            neuronGuids = new List<Guid>();
            neurons = new Dictionary<Guid, Neuron>
            {
                // Adding the input refference
                { InputGuid, new Neuron(InputGuid, this, rand) },

                // Adding the output refference
                { OutputGuid, new Neuron(OutputGuid, this, rand) }
            };

            SynapseCount = 0;
            foreach (var elem in Structure)
            {
                Guid source = elem.Source;
                Guid dest = elem.Destination;
                double strength = elem.Strength;

                if (!neurons.ContainsKey(source))
                    if (source != BiasMark)
                        RegisterNeuron(new Neuron(source, this, rand));

                if (!neurons.ContainsKey(dest))
                    RegisterNeuron(new Neuron(dest, this, rand));

                if (source == BiasMark)
                    neurons[dest].Bias = strength;
                else
                {
                    neurons[dest].ForceCreateDendrite(neurons[source], strength);
                    SynapseCount++;
                }
            }

            SynapseCount -= InputSize + OutputSize;
        }

        public List<Gene> RefreshStructure()
        {
            var structure = new List<Gene>();
            SynapseCount = 0;

            foreach (var guid in neurons.Keys)
            {
                if (neurons[guid].IsRoot()) continue;

                structure.Add((BiasMark, guid, neurons[guid].Bias));
            }

            foreach (var dest in neurons.Keys)
                foreach (var source in neurons[dest].DendriteStrength)
                {
                    structure.Add((source.Key.Identifier, dest, source.Value));
                    SynapseCount++;
                }

            SynapseCount -= InputSize + OutputSize;

            return structure;
        }

        public List<Gene> Mutate(Mode mode, double mutationRate)
        {
            if (rand.NextDouble() < mutationRate)
            {
                var list = new List<Guid>(neuronGuids);
                double gr = (NeuronCount * .015 + 1) / NeuronCount;

                var p = new MutationProbabilities();
                switch (mode)
                {
                    case Mode.Grow:
                        p = new MutationProbabilities
                        {
                            MutationRate = gr,
                            NeuronCreation = .3,
                            NeuronDeletion = .1,
                            SynapseCreation = new double[] { .4, .4, .7, .9 },
                            SynapseDeletion = .2,
                            SynapseAlteration = .3
                        };
                        break;

                    case Mode.Balance:
                        p = new MutationProbabilities
                        {
                            MutationRate = gr,
                            NeuronCreation = .2,
                            NeuronDeletion = .2,
                            SynapseCreation = new double[] { .3, .4, .7, .9 },
                            SynapseDeletion = .3,
                            SynapseAlteration = .4
                        };
                        break;

                    case Mode.Shrink:
                        p = new MutationProbabilities
                        {
                            MutationRate = gr,
                            NeuronCreation = .1,
                            NeuronDeletion = .3,
                            SynapseCreation = new double[] { .2, .4, .7, .9 },
                            SynapseDeletion = .4,
                            SynapseAlteration = .5
                        };
                        break;
                }

                foreach (var guid in list)
                    neurons[guid].Mutate(p);

                Structure = RefreshStructure();
            }

            return Structure;
        }

        public Neuron GetNeuron(Guid id)
        {
            if (!neurons.ContainsKey(id)) return null;

            return neurons[id];
        }

        public bool RegisterNeuron(Neuron neuron)
        {
            if (neurons.ContainsKey(neuron.Identifier)) return false;

            neurons.Add(neuron.Identifier, neuron);
            neuronGuids.Add(neuron.Identifier);

            return true;
        }

        public bool UnregisterNeuron(Neuron neuron)
        {
            if (!neurons.ContainsKey(neuron.Identifier)) return false;

            neurons.Remove(neuron.Identifier);
            neuronGuids.Remove(neuron.Identifier);

            return true;
        }

        public Neuron RandomNeuron()
        {
            if (neuronGuids.Count == 0) return null;

            int index = rand.Next(0, neuronGuids.Count);

            return neurons[neuronGuids[index]];
        }

        public List<double> Querry(List<double> inputs, out long steps)
        {
            Propagate(inputs, out steps);

            if (steps >= 0)
            {
                var outputNeurons = new List<Gene>();
                var outputs = new List<double>();

                foreach (var neuron in neurons[OutputGuid].Dendrites)
                {
                    double response = neuron.Signal;

                    outputNeurons.Add((neuron.Identifier, OutputGuid, response)); // adding signal instead of strength as its what represents the output
                }

                outputNeurons.Sort(); // makes sure the order is correct related to the input

                foreach (var neuron in outputNeurons)
                    outputs.Add(neuron.Strength);

                return outputs;
            }
            else
                return null;
        }

        public void Nap()
        {
            // BIG TODO: implement synaptic plasticity here
            foreach (var neuron in neurons.Values)
                neuron.Clean();
        }

        /// <summary>
        /// Fires all neurons in the cluster BFS style.
        /// </summary>
        /// <returns>True if the graph has a cycle, false if the graph is a tree.</returns>
        private void Propagate(List<double> inputs, out long steps)
        {
            var queue = new Queue<(Neuron next, long depth)>();
            steps = 0;

            // baking the inputs into the input neuron axons
            for (int i = 0; i < neurons[InputGuid].Axons.Count; ++i)
            {
                var inputNeuron = neurons[InputGuid].Axons[i];

                if (inputNeuron.Fire(-1, inputs[i]))
                {
                    steps++;
                    foreach (var successor in inputNeuron.Axons)
                    {
                        queue.Enqueue((successor, inputNeuron.Depth));
                    }
                }
            }

            while (queue.Count > 0 && queue.Peek().depth < NeuronCount * 2)
            {
                var nextNeuron = queue.Peek().next;
                var fromDepth = queue.Dequeue().depth;

                if (nextNeuron.Fire(fromDepth))
                {
                    steps++;
                    foreach (var successor in nextNeuron.Axons)
                    {
                        queue.Enqueue((successor, nextNeuron.Depth));
                    }
                }
            }

            if (queue.Count > 0)
                steps = -1;
        }
    }
}
