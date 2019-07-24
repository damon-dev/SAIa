using System;
using System.Collections.Generic;

namespace EvolutionalNeuralNetwork.XOR
{
    public class XORDisplayProtocol : DisplayProtocol
    {
        public XORDisplayProtocol(XORDataCollection data)
        {
            data.FetchTrainingData(out input, out expectedOutput, 4);
        }

        public override void Display(List<Gene> structure)
        {
            if (structure == null) return;

            var cluster = new Cluster(new Random());
            cluster.GenerateFromStructure(structure);

            double totalTime = 0;

            for (int i = 0; i < input.Count; i++)
            {
                var preditcedOutput = cluster.Querry(input[i], out TimeSpan time);
                cluster.Nap();

                Console.WriteLine($"{input[i][0]} ^ {input[i][1]} = {preditcedOutput[0]:0.00000}");
                totalTime += time.TotalSeconds;
            }

            totalTime /= input.Count;

            Console.WriteLine($"Average time: {totalTime:0.00000}");
            Console.SetCursorPosition(0, Console.CursorTop - input.Count - 1);
        }
    }
}
