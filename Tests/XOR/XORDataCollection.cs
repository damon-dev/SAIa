using Core;
using System.Collections.Generic;

namespace Tests.XOR
{
    public class XORDataCollection : Data
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

            InputFeatureCount = TrainingInput[0].Count;
            OutputFeatureCount = TrainingOutput[0].Count;
        }
    }
}
