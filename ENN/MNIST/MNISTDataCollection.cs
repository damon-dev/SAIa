using System;
using System.Collections.Generic;
using System.IO;

namespace EvolutionalNeuralNetwork.MNIST
{
    public class MNISTDataCollection : DataCollection
    {
        private readonly List<int>[] trainingKeep;

        public MNISTDataCollection()
        {
            trainingKeep = new List<int>[10];
            for (int i = 0; i < 10; ++i)
                trainingKeep[i] = new List<int>();

            foreach (var image in MnistReader.ReadTrainingData())
            {
                TrainingInput.Add(ProcessImage(image.Data));
                TrainingOutput.Add(ProcessLabel(image.Label));
                trainingKeep[image.Label].Add(TrainingInput.Count - 1);
            }
        
            foreach (var image in MnistReader.ReadTestData())
            {
                TestInput.Add(ProcessImage(image.Data));
                TestOutput.Add(ProcessLabel(image.Label));
            }

            InputWidth = TrainingInput[0].Count;
            OutputWidth = TrainingOutput[0].Count;
        }

        public override void FetchTrainingData(out List<List<double>> input, out List<List<double>> output, int count)
        {
            input = new List<List<double>>();
            output = new List<List<double>>();

            var rand = new Random();
            var used = new HashSet<int>();

            if (count <= 0 || count > 100) count = 100;

            for (int k = 0; k < 10; k++)
            {
                for (int i = 0; i < count / 10; ++i)
                {
                    int r = rand.Next(trainingKeep[k].Count);
                    int index = trainingKeep[k][r];
                    while (used.Contains(index))
                    {
                        r = rand.Next(trainingKeep[k].Count);
                        index = trainingKeep[k][r];
                    }

                    used.Add(index);
                    input.Add(TrainingInput[index]);
                    output.Add(TrainingOutput[index]);
                }
            }
        }

        public override void FetchTestData(out List<List<double>> input, out List<List<double>> output, int count)
        {
            input = new List<List<double>>();
            output = new List<List<double>>();

            if (count <= 0) count = TestInput.Count;

            for (int i = 0; i < count; ++i)
            {
                input.Add(TestInput[i]);
                output.Add(TestOutput[i]);
            }
        }

        private List<double> ProcessImage(byte[] data)
        {
            var list = new List<double>();

            for (int i = 0; i < data.Length; ++i)
                list.Add(data[i] / (double)255);

            return list;
        }

        private List<double> ProcessLabel(byte label)
        {
            /*
            var list = new List<double>();

            for (int i = 0; i < 10; ++i)
            {
                if (i == label)
                    list.Add(1);
                else
                    list.Add(0);
            }


            return list;
            */

            return new List<double> { (double)label / 10 };
        }
    }
}
