using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvolutionalNeuralNetwork
{
    class Program : IObserver<List<Gene>>
    {
        private IDisposable stopper;
        private List<Gene> currentStructure;

        static void Main(string[] args)
        {
            var data = new DataCollection();
            var program = new Program();
            var environment = new Environment();

            program.stopper = environment.Subscribe(program);

            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    if (program.currentStructure != null)
                        program.Display(program.currentStructure);
                }
            });

            environment.Start(data);

            Console.ReadLine();

            environment.Stop();
        }

        public void OnCompleted()
        {
            stopper.Dispose();
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(List<Gene> structure)
        {
            if (!structure.Equals(currentStructure))
            {
                currentStructure = structure;
            }
        }

        private void Display(List<Gene> structure)
        {
            var cluster = new Cluster();
            structure = new List<Gene>(structure);

            var input = new List<List<double>>
            {
                new List<double> { 0, 0 },
                new List<double> { 0, 1 },
                new List<double> { 1, 0 },
                new List<double> { 1, 1 }
            };
            var output = new List<List<double>>();
            double totalTime = 0;

            cluster.GenerateFromStructure(structure);
            output.Add(cluster.Querry(input[0], out TimeSpan time));
            cluster.Nap();
            totalTime += time.TotalSeconds;

            output.Add(cluster.Querry(input[1], out time));
            cluster.Nap();
            totalTime += time.TotalSeconds;

            output.Add(cluster.Querry(input[2], out time));
            cluster.Nap();
            totalTime += time.TotalSeconds;

            output.Add(cluster.Querry(input[3], out time));
            totalTime += time.TotalSeconds;

            totalTime /= 4;

            for (int i = 0; i < input.Count; ++i)
            {
                Console.WriteLine($"{input[i][0]} ^ {input[i][1]} = {output[i][0]:0.00000}");
            }

            Console.WriteLine($"Average time: {totalTime:0.00000}");
            Console.SetCursorPosition(0, Console.CursorTop - 5);
        }
    }
}
