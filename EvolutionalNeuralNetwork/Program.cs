using EvolutionalNeuralNetwork.MNIST;
using EvolutionalNeuralNetwork.XOR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvolutionalNeuralNetwork
{
    class Program : IObserver<List<Gene>>
    {
        private const int threadCount = 5;
        private IDisposable stopper;
        private List<Gene> currentStructure;

        static void Main(string[] args)
        {
            var program = new Program();
            var environment = new Environment();

            var data = new XORDataCollection();
            var displayProtocol = new XORDisplayProtocol(data);

            program.stopper = environment.Subscribe(program);

            displayProtocol.Display(null);

            switch(displayProtocol.ReadResponse())
            {
                case Response.Load:
                    environment.Start(data, true, threadCount);
                    break;

                case Response.Train:
                    environment.Start(data, false, threadCount);
                    break;

                case Response.Quit:
                    return;

                default:
                    return;
            }

            var displayTask = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    displayProtocol.Display(program.currentStructure);
                }
            });

            Response response;
            while((response = displayProtocol.ReadResponse()) != Response.Quit)
            {
                switch (response)
                {
                    case Response.Train:
                        program.stopper = environment.Subscribe(program);
                        environment.Start(data, true, threadCount);
                        break;

                    case Response.Stop:
                        environment.Stop();
                        break;

                    default:
                        break;
                }
            }
            
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
