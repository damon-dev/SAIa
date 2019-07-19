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

            cluster.GenerateFromStructure(structure);

            var input = new List<List<double>>
            {
                new List<double> { 0, 0 },
                new List<double> { 0, 1 },
                new List<double> { 1, 0 },
                new List<double> { 1, 1 }
            };


            var output = new List<List<double>>();
            
            output.Add(cluster.Querry(input[0]));
            cluster.GenerateFromStructure(structure);
            output.Add(cluster.Querry(input[1]));
            cluster.GenerateFromStructure(structure);
            output.Add(cluster.Querry(input[2]));
            cluster.GenerateFromStructure(structure);
            output.Add(cluster.Querry(input[3]));
            
            /*
            var output = new List<List<double>>();
            output.Add(cluster.Querry(input[0]));
            output.Add(cluster.Querry(input[1]));
            output.Add(cluster.Querry(input[2]));
            output.Add(cluster.Querry(input[3]));
            */
            for (int i = 0; i < input.Count; ++i)
            {
                Console.WriteLine($"{input[i][0]} ^ {input[i][1]} = {output[i][0]}");
            }

            Console.SetCursorPosition(0, Console.CursorTop - 4);
        }
    }
}
