using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EvolutionalNeuralNetwork;

namespace UnitTests
{
    [TestClass]
    public class ClusterTest
    {
        List<Guid> NeuronGuids;

        public ClusterTest()
        {
            NeuronGuids = new List<Guid>();

            for (int i = 0; i < 10; ++i)
                NeuronGuids.Add(Guid.NewGuid());
        }

        [TestMethod]
        public void Creation()
        {
            // basic cluster with 1 input and 1 output
            var structure = new List<Gene>
            {
                (Cluster.InputGuid, NeuronGuids[0], 0),
                (NeuronGuids[0], NeuronGuids[1], 0.5),
                (NeuronGuids[1], Cluster.OutputGuid, 0)
            };

            var cluster = new Cluster(structure);

            foreach(var s in structure)
            {
                var source = cluster.Neurons[s.Source];
                var dest = cluster.Neurons[s.Destination];
                Assert.AreEqual(s.Strength, dest.Dendrites[source]);
            }
        }

        [TestMethod]
        public void Querry()
        {
            // cluster with 2 input and 1 output
            var structure = new List<Gene>
            {
                (Cluster.InputGuid, NeuronGuids[0], 0),
                (Cluster.InputGuid, NeuronGuids[1], 0),
                (NeuronGuids[0], NeuronGuids[2], 0.5),
                (NeuronGuids[1], NeuronGuids[2], 0.5),
                (NeuronGuids[2], Cluster.OutputGuid, 0)
            };

            var cluster = new Cluster(structure);

            var result = cluster.Querry(new List<double>{ 4, 2 });

            Assert.AreEqual(3, result);

            result = cluster.Querry(new List<double> { 4 });

            Assert.AreEqual(3, result); // previous input of 2 is remembered

            result = cluster.Querry(new List<double> { 6, 2, 3 });

            Assert.AreEqual(4, result);

            // TODO: fix mutation rate to be per neuron not per cluster
        }
    }
}
