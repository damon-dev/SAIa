using System.Collections.Generic;

namespace EvolutionalNeuralNetwork
{
    // TODO: make modular
    public class DataCollection
    {
        readonly List<List<double>> Input;
        readonly List<List<double>> Output;

        public DataCollection()
        {
            Input = new List<List<double>>
            {
                new List<double> {0, 0},
                new List<double> {0, 1},
                new List<double> {1, 0},
                new List<double> {1, 1}
            };

            Output = new List<List<double>>
            {
                new List<double> {0},
                new List<double> {1},
                new List<double> {1},
                new List<double> {0}
            };
        }

        public List<List<double>> FetchInput()
        {
            return Input;
        }

        public List<List<double>> FetchOutput()
        {
            return Output;
        }
    }
}
