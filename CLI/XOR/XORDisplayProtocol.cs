using Core;
using System;

namespace CLI.XOR
{
    public class XORDisplayProtocol : DisplayProtocol
    {

        public XORDisplayProtocol(XORDataCollection dataCollection)
        {
            //data.FetchTestData(out input, out expectedOutput, 100);
            dataCollection.FetchTrainingData(out features, 10, false);
        }

        public override void Display(Entity champion, CultureConfiguration cfg)
        {
            base.Display(champion, cfg);

            if (champion == null || champion.Fitness == double.PositiveInfinity) return;

            var cluster = new Cluster();
            cluster.GenerateFromStructure(champion.Genes);

            double totalSteps = 0;

            for (int i = features.Count - 1; i >= 0; i--)
            {
                var input = features[i].Input;
                var expectedOutput = features[i].Output;
                var predictedOutput = cluster.Querry(input, out long steps);
                cluster.Nap();

                Console.WriteLine($"{input[0]} ^ {input[1]} = {((predictedOutput != null) ? predictedOutput[0] : -1):0.00000}");
                totalSteps += steps;
            }

            totalSteps /= features.Count;

            Console.WriteLine($"Steps: {totalSteps:0.00}      ");
            Console.SetCursorPosition(0, Console.CursorTop - features.Count - 3);
        }
    }
}
