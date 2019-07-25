using EvolutionalNeuralNetwork.MNIST;
using EvolutionalNeuralNetwork.XOR;
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
            var program = new Program();
            var environment = new Environment();

            var data = new MNISTDataCollection();
            var displayProtocol = new MNISTDisplayProtocol(data);

            program.stopper = environment.Subscribe(program);

            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    displayProtocol.Display(program.currentStructure);
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
                currentStructure = structure;
        }
    }
}
