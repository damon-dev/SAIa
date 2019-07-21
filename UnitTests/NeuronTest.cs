using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EvolutionalNeuralNetwork;

namespace UnitTests
{
    [TestClass]
    public class NeuronTest
    {
        [TestMethod]
        public void Creation()
        {
            var neuron = new Neuron(Guid.NewGuid());

            Assert.IsNotNull(neuron.Identifier);
            Assert.IsNotNull(neuron.Dendrites);
            Assert.IsNotNull(neuron.Connections);
            Assert.AreEqual(1, neuron.Threshold); // has to be this way to facilitate bias nodes
        }

        [TestMethod]
        public void SynapseCreation()
        {
            var origin = new Neuron(Guid.NewGuid()); 
            var target = new Neuron(Guid.NewGuid());

            target.CreateSynapse(origin, 0.2);

            Assert.IsTrue(origin.Connections.Exists(t => t.Equals(target)));
            Assert.IsTrue(target.Dendrites.ContainsKey(origin));
            Assert.AreEqual(target.Dendrites[origin], 0.2);

            target.CreateSynapse(origin, -0.4);

            Assert.AreEqual(target.Dendrites[origin], -0.4);
        }

        [TestMethod]
        public void Activation()
        {
            var origins = new List<Neuron>();
            var target = new Neuron(Guid.NewGuid());

            for (int i = 0; i < 10; ++i)
            {
                origins.Add(new Neuron(Guid.NewGuid()));
                target.CreateSynapse(origins[i], 0.1 * ((i % 2)*2 - 1));
            }

            Assert.IsFalse(target.Fire());
            Assert.AreEqual(0, target.Threshold);

            target.Dendrites[origins[0]] += 0.1;

            Assert.IsTrue(target.Fire());
            Assert.AreEqual(0.1, target.Threshold);

            target.Dendrites[origins[0]] -= 0.2;

            Assert.IsFalse(target.Fire());
            Assert.AreEqual(0, target.Threshold);
        }
    }
}
