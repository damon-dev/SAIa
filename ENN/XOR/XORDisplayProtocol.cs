using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvolutionalNeuralNetwork.XOR
{
    public class XORDisplayProtocol : DisplayProtocol
    {
        public XORDisplayProtocol(XORDataCollection data)
        {
            data.FetchTrainingData(out input, out expectedOutput, 4, false);
        }

        public override void Display(Entity champion)
        {
            base.Display(champion);

            if (champion == null || champion.FitnessValue == double.PositiveInfinity) return;

            var cluster = new Cluster(new Random());
            cluster.GenerateFromStructure(champion.Genes);

            double totalSteps = 0;

            for (int i = 0; i < input.Count; i++)
            {
                var preditcedOutput = cluster.Querry(input[i], out int steps);
                cluster.Nap();

                Console.WriteLine($"{input[i][0]} ^ {input[i][1]} = {((preditcedOutput != null) ? preditcedOutput[0] : -1):0.00000}");
                totalSteps += steps;
            }

            totalSteps /= input.Count;

            Console.WriteLine($"Fitness: {champion.FitnessValue:0.00}    ");
            Console.WriteLine($"Steps: {totalSteps:0.00}      ");
            Console.SetCursorPosition(0, Console.CursorTop - input.Count - 4);
        }
    }
}
