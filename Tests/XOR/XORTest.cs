using Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Tests.XOR
{
    [TestClass]
    public class XORTest : IObserver<Entity>
    {
        List<List<double>> input;
        List<List<double>> expectedOutput;

        const int threadCount = 4;
        const int size = 50;
        IDisposable stopper;
        Entity currentChampion;
        Incubator incubator;

        [TestMethod]
        public void XOR()
        {
            var data = new XORDataCollection();

            data.FetchTrainingData(out input, out expectedOutput);
            
            incubator = new Incubator(data);

            stopper = incubator.Subscribe(this);

            incubator.Populate(false, 4, size);
            incubator.Start();
        }

        public void Display(Entity champion)
        {
            if (champion == null || champion.Fitness == double.PositiveInfinity) return;

            var cluster = new Cluster();
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

            Console.WriteLine($"Fitness: {champion.Fitness:0.00}    ");
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
                entity.Fitness < currentChampion.Fitness)
            {
                currentChampion = entity;
                Display(entity);
            }

            if (entity.Fitness < -10000)
                incubator.Stop(false);
        }
    }
}
