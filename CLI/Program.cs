using CLI.MNIST;
using Core;
using System;

namespace CLI
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
            var incubator = new Incubator(data);

            program.stopper = incubator.Subscribe(program);

            program.displayProtocol.Display(null);

            switch(program.displayProtocol.ReadResponse())
            {
                case Response.Load:
                    incubator.Populate(true, threadCount, size);
                    incubator.Start();
                    break;

                case Response.Train:
                    incubator.Populate(false, threadCount, size);
                    incubator.Start();
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
                        program.stopper = incubator.Subscribe(program);
                        incubator.Start();
                        break;

                    case Response.Stop:
                        incubator.Stop(true);
                        break;

                    default:
                        break;
                }
            }

            incubator.Stop(false);
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
