using EvolutionalNeuralNetwork.XOR;
using EvolutionalNeuralNetwork.MNIST;
using System;

namespace EvolutionalNeuralNetwork
{
    class Program : IObserver<Entity>
    {
        private const int threadCount = 4;
        private const int size = 50;
        private IDisposable stopper;
        private Entity currentChampion;
        private MNISTDisplayProtocol displayProtocol;

        static void Main(string[] args)
        {
            var program = new Program();
            var data = new MNISTDataCollection();
            program.displayProtocol = new MNISTDisplayProtocol(data);
            var environment = new Environment(data);

            program.stopper = environment.Subscribe(program);

            program.displayProtocol.Display(null);

            switch(program.displayProtocol.ReadResponse())
            {
                case Response.Load:
                    environment.Populate(true, threadCount, size);
                    environment.Start();
                    break;

                case Response.Train:
                    environment.Populate(false, threadCount, size);
                    environment.Start();
                    break;

                default:
                    return;
            }

            Response response;
            while((response = program.displayProtocol.ReadResponse()) != Response.Quit)
            {
                switch (response)
                {
                    case Response.Train:
                        program.stopper = environment.Subscribe(program);
                        environment.Start();
                        break;

                    case Response.Stop:
                        environment.Stop(true);
                        break;

                    default:
                        break;
                }
            }

            environment.Stop(false);
        }

        public void OnCompleted()
        {
            stopper.Dispose();
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(Entity entity)
        {
            if (currentChampion == null ||
                entity.FitnessValue < currentChampion.FitnessValue)
            {
                currentChampion = entity;
                displayProtocol.Display(entity);
            }
        }
    }
}
