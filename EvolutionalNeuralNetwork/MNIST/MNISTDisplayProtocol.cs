using System;
using System.Collections.Generic;
using System.Linq;

namespace EvolutionalNeuralNetwork.MNIST
{
    public class MNISTDisplayProtocol : DisplayProtocol
    {
        public MNISTDisplayProtocol(MNISTDataCollection data)
        {
            data.FetchTestData(out input, out expectedOutput, 1000);
        }

        public override void Display(List<Gene> structure)
        {
            if (structure == null)
            {
                for (int i = 0; i < 10; ++i)
                    Console.WriteLine($"{i} : {0:0.00}");

                Console.WriteLine($"Average time: {0:0.00000}");
                Console.SetCursorPosition(0, Console.CursorTop - 11);
            }
            else
            {
                var cluster = new Cluster(new Random());
                cluster.GenerateFromStructure(structure);

                double totalTime = 0;
                var goodHits = new double[10];
                var totalHits = new int[10];

                for (int i = 0; i < input.Count; i++)
                {
                    var preditcedOutput = cluster.Querry(input[i], out TimeSpan time);
                    cluster.Nap();

                    int position = 0;
                    double maxim = preditcedOutput[0];
                    for (int k = 1; k < preditcedOutput.Count; ++k)
                    {
                        if (preditcedOutput[k] > maxim)
                        {
                            maxim = preditcedOutput[k];
                            position = k;
                        }
                    }

                    if (expectedOutput[i][position] == 1 && preditcedOutput[position] > 0)
                    {
                        goodHits[position]++;
                        totalHits[position]++;
                    }
                    else
                    {
                        for (int k = 0; k < expectedOutput[i].Count; ++k)
                        {
                            if (expectedOutput[i][k] == 1)
                            {

                                totalHits[k]++;
                                break;
                            }
                        }
                    }

                    totalTime += time.TotalSeconds;
                }

                totalTime /= input.Count;

                for (int i = 0; i < 10; ++i)
                    Console.WriteLine($"{i} : {goodHits[i] / totalHits[i]:0.00}");

                Console.WriteLine($"Average time: {totalTime:0.00000}");
                Console.SetCursorPosition(0, Console.CursorTop - 11);
            }
        }
    }
}
