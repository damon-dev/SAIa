using Core;
using System;
using System.Linq;

namespace CLI.MNIST
{
    public class MNISTDisplayProtocol : DisplayProtocol
    {
        public MNISTDisplayProtocol(MNISTDataCollection data)
        {
            //data.FetchTestData(out input, out expectedOutput, 100);
            data.FetchTrainingData(out input, out expectedOutput, 100, false);
        }

        public override void Display(Entity champion)
        {
            base.Display(champion);

            if (champion == null || champion.FitnessValue == double.PositiveInfinity) return;

            var cluster = new Cluster(new Random());
            cluster.GenerateFromStructure(champion.Genes);

            double totalSteps = 0;
            double[] errorRate = new double[10];
            double[] totalElements = new double[10];

            for (int i = 0; i < input.Count; i++)
            {
                var preditcedOutput = cluster.Querry(input[i], out long steps);
                cluster.Nap();
                if (preditcedOutput != null)
                    errorRate[(int)(expectedOutput[i][0] * 10)] += (preditcedOutput[0] - expectedOutput[i][0]) * (preditcedOutput[0] - expectedOutput[i][0]);
                else
                    errorRate[(int)(expectedOutput[i][0] * 10)] += 1;
                totalElements[(int)(expectedOutput[i][0] * 10)]++;
                totalSteps += steps;
            }

            totalSteps /= input.Count;

            for (int i = 0; i < 10; ++i)
            {
                errorRate[i] /= totalElements[i];
                Console.WriteLine($"{i} : {errorRate[i]:0.00000}");
            }

            Console.WriteLine($"Mean error: {(errorRate.Sum() / 10):0.00000}    ");
            Console.WriteLine($"Fitness: {champion.FitnessValue:0.00}    ");
            Console.WriteLine($"Average steps: {totalSteps:0.00}    ");
            Console.SetCursorPosition(0, Console.CursorTop - 15);
        }
    }
}
