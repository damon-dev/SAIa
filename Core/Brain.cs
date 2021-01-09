using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public class Brain
    {
        public static readonly Guid InputGuid = new Guid("8478a94f-5a4a-4d6c-a3f8-16b7fb4ad2c6");
        public static readonly Guid OutputGuid = new Guid("7bd1acb4-07ba-4838-be56-237d3391b61f");
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
        public int InputSize => neurons[InputGuid].Axons.Count - 1;
        public int OutputSize => neurons[OutputGuid].Dendrites.Count;
        public int NeuronCount => activeNeuronGuids.Count;

        private readonly int perceptionFrequency;
        private List<Guid> activeNeuronGuids;
        private Dictionary<Guid, Neuron> neurons;

        public Brain(int _perceptionFrequency = 10)
        {
            neurons = new Dictionary<Guid, Neuron>();
            activeNeuronGuids = new List<Guid>();
            perceptionFrequency = _perceptionFrequency;
        }

        public void GenerateFromStructure(List<Gene> structure)
        {
            Structure = new List<Gene>(structure);

            activeNeuronGuids = new List<Guid>();
            neurons = new Dictionary<Guid, Neuron>
            {
                // Adding the input refference
                { InputGuid, new Neuron(InputGuid, this) },

                // Adding the output refference
                { OutputGuid, new Neuron(OutputGuid, this) }
            };

            SynapseCount = 0;
            foreach (var elem in Structure)
            {
                Guid source = elem.Source;
                Guid dest = elem.Destination;
                double strength = elem.Strength;

                if (!neurons.ContainsKey(source))
                    if (source != BiasMark)
                        RegisterNeuron(new Neuron(source, this));

                if (!neurons.ContainsKey(dest))
                    RegisterNeuron(new Neuron(dest, this));

                if (source == BiasMark)
                    neurons[dest].Bias = strength;
                else
                {
                    neurons[dest].ForceCreateDendrite(neurons[source], strength);
                    SynapseCount++;
                }
            }

            // creating the repetition cycle on the root node (for requesting new perceptions)
            CreateReinforcementCycle();

            SynapseCount -= InputSize + OutputSize;
        }

        private void CreateReinforcementCycle()
        {
            Neuron lastNeuron = neurons[InputGuid];
            Neuron neuron;
            for (int i = 0; i < perceptionFrequency; ++i)
            {
                neuron = new Neuron(Guid.NewGuid(), this);
                neuron.ForceCreateDendrite(lastNeuron, 1);
                lastNeuron = neuron;
            }
            neurons[InputGuid].ForceCreateDendrite(lastNeuron, 1);
        }

        public List<Gene> RecreateStructure()
        {
            var structure = new List<Gene>();
            SynapseCount = 0;

            foreach (var guid in neurons.Keys)
            {
                if (neurons[guid].IsRoot() || neurons[guid].IsInputNeuron() || neurons[guid].IsOutputNeuron()) continue;

                structure.Add((BiasMark, guid, neurons[guid].Bias));
            }

            foreach (var dest in neurons.Keys)
                if (dest != InputGuid) // no need to register input cyclers
                {
                    foreach (var source in neurons[dest].DendriteWeights)
                    {

                        structure.Add((source.Key.Identifier, dest, source.Value));
                        SynapseCount++;
                    }
                }

            SynapseCount -= InputSize + OutputSize;

            return structure;
        }

        public List<Gene> Mutate(NeuronMutationProbabilities p, double mutationRate, out bool hasMutated)
        {
            hasMutated = false;

            if (R.NG.NextDouble() < mutationRate)
            {
                var list = new List<Guid>(activeNeuronGuids);
                double gr = (NeuronCount * p.NeuronMutationsPercentage + 1) / NeuronCount; // rate ot total neurons that can mutate overall

                foreach (var guid in list)
                    neurons[guid].Mutate(p, gr);

                Structure = RecreateStructure();

                hasMutated = true;
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
            activeNeuronGuids.Add(neuron.Identifier);

            return true;
        }

        public bool UnregisterNeuron(Neuron neuron)
        {
            if (!neurons.ContainsKey(neuron.Identifier)) return false;

            neurons.Remove(neuron.Identifier);
            activeNeuronGuids.Remove(neuron.Identifier);

            return true;
        }

        public Neuron RandomNeuron()
        {
            if (activeNeuronGuids.Count == 0) return null;

            int index = R.NG.Next(activeNeuronGuids.Count);

            return neurons[activeNeuronGuids[index]];
        }

        public void Nap()
        {
            // BIG TODO: implement synaptic plasticity here
            foreach (var n in neurons.Values)
                n.Clean();

            // cleaning the reinforcement cycle
            Neuron neuron = neurons[InputGuid].Axons[neurons[InputGuid].Axons.Count - 1];
            while(!neuron.IsRoot())
            {
                neuron.Clean();
                neuron = neuron.Axons[0];
            }
        }

        /// <summary>
        /// Fires all neurons in the cluster BFS style.
        /// </summary>
        public async Task Hijack(Agent agent)
        {
            var queue = new Queue<(Neuron next, long depth)>();

            queue.Enqueue((neurons[InputGuid], -1));

            while (queue.Count > 0 && agent.IsActive) // && queue.Peek().depth < NeuronCount * 2)
            {
                var nextNeuron = queue.Peek().next;
                var fromDepth = queue.Dequeue().depth;

                // this polls the input everytime a cycle completes on the input reference node
                if (nextNeuron.Identifier == InputGuid)
                {
                    // baking the inputs into the input neuron axons
                    int i;

                    var signal = await agent.PerceiveEnvironment();

                    for (i = 0; i < nextNeuron.Axons.Count - 1; ++i) // last axon is the cycle one
                    {
                        var inputNeuron = nextNeuron.Axons[i];

                        if (inputNeuron.Fire(fromDepth, signal[i]))
                        {
                            foreach (var successor in inputNeuron.Axons)
                            {
                                queue.Enqueue((successor, inputNeuron.Depth));
                            }
                        }
                    }

                    var inputCycler = nextNeuron.Axons[i];
                    if (inputCycler.Fire(fromDepth, 1)) // propagates on the input cycle state machine (skips the InputRoot)
                    {
                        queue.Enqueue((inputCycler.Axons[0], inputCycler.Depth));
                    }
                }
                else if (nextNeuron.Identifier == OutputGuid)
                {
                    // baking the outputs into the actuators
                    int i;
                    var actionPotential = new List<double>();
                    for (i = 0; i < nextNeuron.Dendrites.Count; ++i)
                    {
                        var outputNeuron = nextNeuron.Dendrites[i];

                        if (outputNeuron.Depth == fromDepth)
                            actionPotential.Add(outputNeuron.Signal.Value);
                        else
                            actionPotential.Add(0);
                    }

                    await agent.Action(actionPotential);
                }
                else if (nextNeuron.Fire(fromDepth))
                {
                    foreach (var successor in nextNeuron.Axons)
                    {
                        queue.Enqueue((successor, nextNeuron.Depth));
                    }
                }
            }
        }
    }
}
