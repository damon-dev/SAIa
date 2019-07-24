using System.Collections.Generic;
using System.Linq;

namespace EvolutionalNeuralNetwork
{
    public class DataCollection
    {
        public int InputWidth { get; protected set; }
        public int OutputWidth { get; protected set; }

        protected List<List<double>> TrainingInput;
        protected List<List<double>> TrainingOutput;
        protected List<List<double>> TestInput;
        protected List<List<double>> TestOutput;

        public DataCollection()
        {
            TrainingInput = new List<List<double>>();
            TrainingOutput = new List<List<double>>();
            TestInput = new List<List<double>>();
            TestOutput = new List<List<double>>();
        }

        public virtual void FetchTrainingData(out List<List<double>> input, out List<List<double>> output, int count)
        {
            input = TrainingInput.Take(count).ToList();
            output = TrainingOutput.Take(count).ToList();
        }

        public virtual void FetchTestData(out List<List<double>> input, out List<List<double>> output, int count)
        {
            input = TestInput.Take(count).ToList();
            output = TestOutput.Take(count).ToList();
        }
    }
}
