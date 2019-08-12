using Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Tests.XOR
{
    [TestClass]
    public class XORTest : IObserver<Entity>
    {
        Data data;
        List<List<double>> input;
        List<List<double>> expectedOutput;

        const int threadCount = 4;
        const int size = 50;
        IDisposable stopper;
        Entity currentChampion;

        [TestMethod]
        public void XOR()
        {
            var data = new MNISTDataCollection();

            data.FetchTrainingData(out input, out expectedOutput);

            program.displayProtocol = new MNISTDisplayProtocol(data);
            var incubator = new Incubator(data);

            program.stopper = incubator.Subscribe(program);

            program.displayProtocol.Display(null);

            switch (program.displayProtocol.ReadResponse())
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
            while ((response = program.displayProtocol.ReadResponse()) != Response.Quit)
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

        public void Display(Entity champion)
        {
            base.Display(champion);

            if (champion == null || champion.FitnessValue == double.PositiveInfinity) return;

            var cluster = new Cluster(new Random());
            cluster.GenerateFromStructure(champion.Genes);

            double totalSteps = 0;

            for (int i = input.Count - 1; i >= 0; i--)
            {
                var preditcedOutput = cluster.Querry(input[i], out long steps);
                cluster.Nap();

                Console.WriteLine($"{input[i][0]} ^ {input[i][1]} = {((preditcedOutput != null) ? preditcedOutput[0] : -1):0.00000}");
                totalSteps += steps;
            }

            totalSteps /= input.Count;

            Console.WriteLine($"Fitness: {champion.FitnessValue:0.00}    ");
            Console.WriteLine($"Steps: {totalSteps:0.00}      ");
            Console.SetCursorPosition(0, Console.CursorTop - input.Count - 4);
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
