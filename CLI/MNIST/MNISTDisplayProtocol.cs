using Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CLI.MNIST
{
    public class MNISTDisplayProtocol : DisplayProtocol
    {
        public MNISTDisplayProtocol(MNISTDataCollection dataCollection)
        {
            //data.FetchTestData(out input, out expectedOutput, 100);
            dataCollection.FetchTrainingData(out features, 100, false);
        }

        private int GetLabel(List<double> label)
        {
            for (int i = 0; i < label.Count; ++i)
                if (label[i] == 1)
                    return i;
            return -1;
        }

        public override void Display(Entity champion, CultureConfiguration cfg)
        {
            base.Display(champion, cfg);

            if (champion == null || champion.Fitness == double.PositiveInfinity) return;

            var cluster = new Cluster();
            cluster.GenerateFromStructure(champion.Genes);

            double totalSteps = 0;
            double[] errorRate = new double[10];
            double[] totalElements = new double[10];

            for (int i = 0; i < features.Count; i++)
            {
                var input = features[i].Input;
                var expectedOutput = features[i].Output;

                var preditcedOutput = cluster.Querry(input, out long steps);
                cluster.Nap();
                /*
                if (preditcedOutput != null)
                    errorRate[(int)(expectedOutput[0] * 10)] += (preditcedOutput[0] - expectedOutput[0]) * (preditcedOutput[0] - expectedOutput[0]);
                else
                    errorRate[(int)(expectedOutput[0] * 10)] += 1;
                totalElements[(int)(expectedOutput[0] * 10)]++;
                */
                int label = GetLabel(expectedOutput);
                if (preditcedOutput != null)
                {
                    int k = 0;
                    for (k = 0; k < preditcedOutput.Count; ++k)
                        if (preditcedOutput[k] >= preditcedOutput[label] && label != k)
                            break;

                    if (k == preditcedOutput.Count)
                        errorRate[label]++;
                }

                totalElements[label]++;
                totalSteps += steps;
            }

            totalSteps /= features.Count;

            for (int i = 0; i < 10; ++i)
            {
                // errorRate[i] /= totalElements[i];
                //Console.WriteLine($"{i} : {errorRate[i]:0.00000}");
                Console.WriteLine($"{i} : {errorRate[i]} / {totalElements[i]}    ");
            }

            Console.WriteLine($"Mean error: {champion.Fitness:0.00000}    ");
            Console.WriteLine($"Features used: {champion.FeaturesUsed}    ");
            Console.WriteLine($"Average steps: {totalSteps:0.00}    ");
            Console.SetCursorPosition(0, Console.CursorTop - 15);
        }
    }
}
