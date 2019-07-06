using System;
using System.Collections.Generic;

namespace EvolutionalNeuralNetwork
{
    class Program
    {
        static void Main(string[] args)
        {
            // create training set for XOR

            int dataTrainSize = 100;
            var inputTrain = new List<double[]>();
            var outputTrain = new List<double>();
            var rand = new Random();

            for (int i = 0; i < dataTrainSize; ++i)
            {
                int a = rand.Next(0, 2);
                int b = rand.Next(0, 2);

                inputTrain.Add(new double[] { a, b });
                outputTrain.Add(a ^ b);
            }

            // create test set for XOR

            int dataTestSize = 10;
            var inputTest = new List<double[]>();
            var outputTest= new List<double>();

            for (int i = 0; i < dataTestSize; ++i)
            {
                int a = rand.Next(0, 2);
                int b = rand.Next(0, 2);

                inputTest.Add(new double[] { a, b });
                outputTest.Add(a ^ b);
            }

            // train
            var gAlg = new Environment();

            int runs = 50000;
            var bestStructure = gAlg.Run(runs, inputTrain, outputTrain);

            Console.WriteLine($"Ready!");
            var champ = new Cluster(bestStructure);

            do
            {
                var inp = Console.ReadLine();
                string[] inpsp = inp.Split(' ');
                int a = int.Parse(inpsp[0]);
                int b = int.Parse(inpsp[1]);

                double result = champ.Querry(new List<double> { a, b });
                Console.WriteLine(result);
            }
            while (true);
        }
    }
}
