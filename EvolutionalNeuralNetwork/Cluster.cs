using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvolutionalNeuralNetwork
{
    public class Cluster
    {
        public static readonly Guid InputGuid = new Guid("8478a94f-5a4a-4d6c-a3f8-16b7fb4ad2c6");
        public static readonly Guid OutputGuid = new Guid("7bd1acb4-07ba-4838-be56-237d3391b61f");
        public static readonly Guid SeedGuid = new Guid("e3ea29b5-493c-48a6-9c94-c7b418b6d732");
        public static readonly Guid BiasMark = new Guid("d579d9f1-cd6f-4236-9a66-69115ae170d3");
        public static readonly Guid RecoveryMark = new Guid("39414500-9063-470d-8ce3-744a15bbc0ff");

        public List<Gene> Structure { get; private set; }

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

        public List<double> Querry(List<double> inputs, out TimeSpan time)
        {
            time =  Propagate(inputs);

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

        public List<Gene> GenerateFromStructure(List<Gene> structure, bool allowMutation = false)
        {
            Structure = new List<Gene>(structure);

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
                    if (source != BiasMark && source != RecoveryMark)
                        RegisterNeuron(new Neuron(source, this, rand));

                if (!neurons.ContainsKey(dest))
                    RegisterNeuron(new Neuron(dest, this, rand));

                if (source == BiasMark)
                    neurons[dest].Bias = strength;
                else if (source == RecoveryMark)
                    neurons[dest].Recovery = strength;
                else
                    neurons[dest].CreateSynapse(neurons[source], strength);
            }

            if (RegisterNeuron(new Neuron(SeedGuid, this, rand)))
            {
                neurons[SeedGuid].Bias = RandomSynapseStrength();
                neurons[SeedGuid].Recovery = Math.Abs(RandomSynapseStrength());

                Structure = RecreateStructure();
            }

            if (allowMutation)
            {
                var list = new List<Guid>(neuronGuids);

                foreach (var guid in list)
                    neurons[guid].Mutate();

                Structure = RecreateStructure();
            }

            return Structure;
        }

        public List<Gene> RecreateStructure()
        {
            var structure = new List<Gene>();

            foreach(var guid in neurons.Keys)
            {
                if (guid == InputGuid || guid == OutputGuid) continue;

                structure.Add((BiasMark, guid, neurons[guid].Bias));
                structure.Add((RecoveryMark, guid, neurons[guid].Recovery));
            }

            foreach(var dest in neurons.Keys)
                foreach(var source in neurons[dest].Dendrites)
                    structure.Add((source.Key.Identifier, dest, source.Value));

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
        private TimeSpan Propagate(List<double> inputs)
        {
            //var newQueue = new ConcurrentQueue<Neuron>();
            //ConcurrentQueue<Neuron> oldQueue;
            var timeStamp = DateTime.UtcNow;
            //var gpu = Gpu.Default;
            var queue = new Queue<Neuron>();

            // baking the inputs into the input neuron axons
            for (int i = 0; i < neurons[InputGuid].Connections.Count; ++i)
            //Parallel.For(0, neurons[InputGuid].Connections.Count, (i) =>
            {
                var inputNeuron = neurons[InputGuid].Connections[i];

                inputNeuron.Fire(inputs[i]);

                foreach (var successor in inputNeuron.Connections)
                    //newQueue.Enqueue(successor);
                    queue.Enqueue(successor);
            }//);
            
            //while (newQueue.Count > 0)
            while (queue.Count > 0)
            {
                //oldQueue = newQueue;
                //newQueue = new ConcurrentQueue<Neuron>();
                //int count = oldQueue.Count;

                //Parallel.For(0, count, (i) =>
                //{
                    //oldQueue.TryDequeue(out Neuron current);
                    var current = queue.Dequeue();
                    if (current.Fire() == true)
                    {
                        foreach (var successor in current.Connections)
                        {
                        //newQueue.Enqueue(successor);
                            queue.Enqueue(successor);
                        }
                    }
                //});
            }

            return DateTime.UtcNow - timeStamp;
        }
    }
}
