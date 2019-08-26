using CLI.MNIST;
using CLI.XOR;
using Core;
using System;

namespace CLI
{
    class Program : IObserver<Entity>
    {
        private const int threadCount = 5;
        private const int size = 100;
        private IDisposable stopper;
        private Entity currentChampion;
        private MNISTDisplayProtocol displayProtocol;

        static void Main(string[] args)
        {
            var program = new Program();
            var data = new MNISTDataCollection();
            program.displayProtocol = new MNISTDisplayProtocol(data);
            Incubator incubator;

            program.displayProtocol.Display(null, null);

            switch(program.displayProtocol.ReadResponse())
            {
                case Response.Load:
                    incubator = new Incubator(data, true, threadCount, size);
                    program.stopper = incubator.Subscribe(program);
                    incubator.Start();
                    break;

                case Response.Train:
                    incubator = new Incubator(data, false, threadCount, size);
                    program.stopper = incubator.Subscribe(program);
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
            var cfg = CultureConfiguration.Shrink;

            if (currentChampion == null ||
                entity.Fitness < currentChampion.Fitness ||
                entity.FeaturesUsed > currentChampion.FeaturesUsed)
            {
                currentChampion = entity;
                displayProtocol.Display(entity, cfg);
            }
        }
    }
}
