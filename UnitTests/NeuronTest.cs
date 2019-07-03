using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeuralNetExp;

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
            Assert.IsNotNull(neuron.Synapses);
            Assert.AreEqual(0, neuron.Axon);
        }

        [TestMethod]
        public void SynapseCreation()
        {
            var neuron = new Neuron(Guid.NewGuid());

            // TODO continue here
        }
    }
}
