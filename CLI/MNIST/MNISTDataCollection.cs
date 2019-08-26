using Core;
using System;
using System.Collections.Generic;

namespace CLI.MNIST
{
    public class MNISTDataCollection : Data
    {
        private readonly List<int>[] trainingKeep;

        public MNISTDataCollection()
        {
            trainingKeep = new List<int>[10];
            for (int i = 0; i < 10; ++i)
                trainingKeep[i] = new List<int>();

            foreach (var image in MnistReader.ReadTrainingData())
            {
                Training.Add(new Datum(ProcessImage(image.Data), ProcessLabel(image.Label)));
                trainingKeep[image.Label].Add(Training.Count - 1);
            }
        
            foreach (var image in MnistReader.ReadTestData())
            {
                Test.Add(new Datum(ProcessImage(image.Data), ProcessLabel(image.Label)));
            }

            InputFeatureCount = Training[0].Input.Count;
            OutputFeatureCount = Training[0].Output.Count;

            SuccessCondition = (expected, predicted) =>
            {
                int label = expected.IndexOf(1);
                for (int i = 0; i < predicted.Count; ++i)
                    if (predicted[i] >= predicted[label] && i != label)
                        return false;

                return true;
            };
        }

        public override void FetchTrainingData(out List<Datum> data, int count, bool random)
        {
            data = new List<Datum>();

            var rand = new Random();
            var used = new HashSet<int>();

            if (count <= 0 || count > Training.Count) count = Training.Count;

            for (int i = 0; i < count; ++i)
            {
                int index;
                int k = i % 10;
                if (random)
                {
                    int r = rand.Next(trainingKeep[k].Count);
                    index = trainingKeep[k][r];

                    while (used.Contains(index))
                    {
                        r = rand.Next(trainingKeep[k].Count);
                        index = trainingKeep[k][r];
                    }
                }
                else
                    index = trainingKeep[k][i / 10];

                used.Add(index);
                data.Add(Training[index]);
            }
        }

        public override void FetchTestData(out List<Datum> data, int count)
        {
            data = new List<Datum>();

            if (count <= 0 || count > Test.Count) count = Test.Count;

            for (int i = 0; i < count; ++i)
            {
                data.Add(Test[i]);
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
            
            var list = new List<double>();

            for (int i = 0; i < 10; ++i)
            {
                if (i == label)
                    list.Add(1);
                else
                    list.Add(0);
            }


            return list;
            

            //return new List<double> { (double)label / 10 };
        }
    }
}
