using System.Collections.Generic;

namespace EvolutionalNeuralNetwork.XOR
{
    public class XORDataCollection : DataCollection
    {
        public XORDataCollection()
        {
            TrainingInput = new List<List<double>>
            {
                new List<double> {0, 0},
                new List<double> {0, 1},
                new List<double> {1, 0},
                new List<double> {1, 1}
            };

            TrainingOutput = new List<List<double>>
            {
                new List<double> {0},
                new List<double> {1},
                new List<double> {1},
                new List<double> {0}
            };

            InputWidth = TrainingInput[0].Count;
            OutputWidth = TrainingOutput[0].Count;
        }
    }
}
