using System.Collections.Generic;

namespace EvolutionalNeuralNetwork
{
    public class DisplayProtocol
    {
        protected List<List<double>> input;
        protected List<List<double>> expectedOutput;

        public virtual void Display(List<Gene> structure)
        {
        }
    }
}
