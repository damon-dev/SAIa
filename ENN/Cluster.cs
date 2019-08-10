using System;
using System.Collections.Generic;

namespace EvolutionalNeuralNetwork
{
    public class Cluster
    {
        public static readonly Guid InputGuid = new Guid("8478a94f-5a4a-4d6c-a3f8-16b7fb4ad2c6");
        public static readonly Guid OutputGuid = new Guid("7bd1acb4-07ba-4838-be56-237d3391b61f");
        public static readonly Guid SeedGuid = new Guid("e3ea29b5-493c-48a6-9c94-c7b418b6d732");
        public static readonly Guid BiasMark = new Guid("d579d9f1-cd6f-4236-9a66-69115ae170d3");
        public static readonly Guid RefactoryMark = new Guid("39414500-9063-470d-8ce3-744a15bbc0ff");

        public List<Gene> Structure { get; private set; }
        public int SynapseCount { get; private set; }
        public int InputSize => neurons[InputGuid].Connections.Count;
        public int OutputSize => neurons[OutputGuid].Dendrites.Count;
        public int NeuronCount => neuronGuids.Count - InputSize - OutputSize;

        private List<Guid> neuronGuids;
        private Dictionary<Guid, Neuron> neurons;

        private readonly Random rand;

        public Cluster(Random _rand)
        {
            neurons = new Dictionary<Guid, Neuron>();
            neuronGuids = new List<Guid>();
            rand = _rand;
        }

        public double RandomSynapseStrength()
        {
            // random value between -1 and 1
            return rand.NextDouble() * 2 - 1;
        }

        public List<double> Querry(List<double> inputs, out int steps)
        {
            Propagate(inputs, out steps);

            var outputNeurons = new List<Gene>();
            var outputs = new List<double>();

            foreach (var key in neurons[OutputGuid].Dendrites.Keys)
            {
                double response = key.Signal;

                outputNeurons.Add((key.Identifier, OutputGuid, response)); // adding signal instead of strength as its what represents the output
            }

            outputNeurons.Sort(); // makes sure the order is correct related to the input

            foreach (var neuron in outputNeurons)
                outputs.Add(neuron.Strength);

            return outputs;
        }

        public void Nap()
        {
            // BIG TODO: implement synaptic plasticity here
            foreach (var neuron in neurons.Values)
                neuron.Fire(0);
        }

        // No mutation
        public List<Gene> GenerateFromStructure(List<Gene> structure)
        {
            return GenerateFromStructure(structure, Mode.Balance, 0);
        }

        public List<Gene> GenerateFromStructure(List<Gene> structure, Mode mode, double mutationRate)
        {
            Structure = new List<Gene>(structure);
            SynapseCount = 0;

            Structure.Sort(); // makes sure the inputs are created in a consistent order

            neuronGuids = new List<Guid>();
            neurons = new Dictionary<Guid, Neuron>
            {
                // Adding the input refference
                { InputGuid, new Neuron(InputGuid, this, rand) },

                // Adding the output refference
                { OutputGuid, new Neuron(OutputGuid, this, rand) }
            };

            foreach (var elem in Structure)
            {
                Guid source = elem.Source;
                Guid dest = elem.Destination;
                double strength = elem.Strength;

                if (!neurons.ContainsKey(source))
                    if (source != BiasMark && source != RefactoryMark)
                        RegisterNeuron(new Neuron(source, this, rand));

                if (!neurons.ContainsKey(dest))
                    RegisterNeuron(new Neuron(dest, this, rand));

                if (source == BiasMark)
                    neurons[dest].Bias = strength;
                else if (source == RefactoryMark)
                    neurons[dest].Refactory = strength;
                else
                {
                    neurons[dest].ForceCreateDendrite(neurons[source], strength);
                    SynapseCount++;
                }
            }

            SynapseCount -= InputSize + OutputSize;

            if (rand.NextDouble() < mutationRate && NeuronCount > 0)
            {
                var list = new List<Guid>(neuronGuids);
                double gr = (NeuronCount * 0.15 + 1) / NeuronCount;

                var p = new MutationProbabilities();
                switch (mode)
                {
                    case Mode.Grow:
                        p = new MutationProbabilities
                        {
                            MutationRate = gr,
                            NeuronCreation = .4,
                            NeuronDeletion = .1,
                            ConnectionAlteration = .5,
                            ConnectionDeletion = .2,
                            DendriteAlteration = .6,
                            DendriteDeletion = .3,
                            RandomWalk = .8,
                            WalkErosion = 2,
                            Bias = .2,
                            Refactory = .1
                        };
                        break;

                    case Mode.Balance:
                        p = new MutationProbabilities
                        {
                            MutationRate = gr,
                            NeuronCreation = .1,
                            NeuronDeletion = .1,
                            ConnectionAlteration = .2,
                            ConnectionDeletion = .2,
                            DendriteAlteration = .3,
                            DendriteDeletion = .3,
                            RandomWalk = .6,
                            WalkErosion = 3,
                            Bias = .2,
                            Refactory = .1
                        };
                        break;

                    case Mode.Shrink:
                        p = new MutationProbabilities
                        {
                            MutationRate = gr,
                            NeuronCreation = .1,
                            NeuronDeletion = .4,
                            ConnectionAlteration = .2,
                            ConnectionDeletion = .5,
                            DendriteAlteration = .3,
                            DendriteDeletion = .6,
                            RandomWalk = 1,
                            WalkErosion = 10,
                            Bias = .2,
                            Refactory = .1
                        };
                        break;
                }

                foreach (var guid in list)
                    neurons[guid].Mutate(p);

                Structure = RecreateStructure();
            }

            return Structure;
        }

        public List<Gene> RecreateStructure()
        {
            var structure = new List<Gene>();
            SynapseCount = 0;

            foreach(var guid in neurons.Keys)
            {
                if (neurons[guid].IsImmutable()) continue;

                structure.Add((BiasMark, guid, neurons[guid].Bias));
                structure.Add((RefactoryMark, guid, neurons[guid].Refactory));
            }

            foreach (var dest in neurons.Keys)
                foreach (var source in neurons[dest].Dendrites)
                {
                    SynapseCount++;
                    structure.Add((source.Key.Identifier, dest, source.Value));
                }

            SynapseCount -= InputSize + OutputSize;
            structure.Sort();

            return structure;
        }

        public Neuron GetNeuron(Guid id)
        {
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
            int index = rand.Next(0, neuronGuids.Count);

            return neurons[neuronGuids[index]];
        }

        /// <summary>
        /// Fires all neurons in the cluster BFS style.
        /// </summary>
        /// <returns>True if the graph has a cycle, false if the graph is a tree.</returns>
        private void Propagate(List<double> inputs, out int steps)
        {
            var queue = new Queue<Neuron>();
            steps = 0;

            // baking the inputs into the input neuron axons
            for (int i = 0; i < neurons[InputGuid].Connections.Count; ++i)
            {
                var inputNeuron = neurons[InputGuid].Connections[i];

                inputNeuron.Fire(inputs[i]);

                foreach (var successor in inputNeuron.Connections)
                {
                    queue.Enqueue(successor);
                }
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.Fire())
                {
                    steps++;
                    Neuron mem = null;
                    foreach (var successor in current.Connections)
                    {
                        if (successor.Equals(current))
                            mem = successor;
                        else
                            queue.Enqueue(successor);
                    }
                    if (mem != null)
                        queue.Enqueue(mem);
                }
            }
        }
    }
}
