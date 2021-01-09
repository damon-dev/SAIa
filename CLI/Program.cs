using Core;
using OpenAI.SDK;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CLI
{
    class Program : IObserver<Entity>
    {
        private const int threadCount = 3;
        private const int size = 100;
        private IDisposable stopper;
        private Entity currentChampion;
        private Display display;

        static void Main(string[] args)
        {
            var program = new Program();
            program.display = new Display();
            Incubator incubator;

            Console.WriteLine("Starting training.");
            //var service = new ApiService();
            var agents = new List<Agent>();

            for (int i = 0; i < threadCount; ++i)
                agents.Add(new CartpoleAgent());

            incubator = new Incubator();
            program.stopper = incubator.Subscribe(program);
            incubator.Populate(false, size, agents);
            var d = program.display.Animate();
            incubator.Start().GetAwaiter().GetResult();

            Console.ReadLine();
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
                entity.Fitness > currentChampion.Fitness)
            {
                currentChampion = entity;
                display.Champion = entity;
            }
        }
    }
}
