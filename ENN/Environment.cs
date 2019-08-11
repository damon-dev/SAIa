using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvolutionalNeuralNetwork
{

    public class Environment : IObservable<Entity>
    {
        public List<Entity> Entities { get; set; }
        
        private bool isRunning;
        private DataCollection data;
        private List<IObserver<Entity>> observers;
        private List<Culture> cultures;
        private List<Task<Entity>> overlords;

        public Environment(DataCollection _data)
        {
            observers = new List<IObserver<Entity>>();

            data = _data;
            isRunning = false;
        }

        public IDisposable Subscribe(IObserver<Entity> observer)
        {
            // Check whether observer is already registered. If not, add it
            if (!observers.Contains(observer))
                observers.Add(observer);

            return new Unsubscriber<Entity>(observers, observer);
        }

        public void Populate(bool fromSaved, int cultureCount, int cultureSize)
        {
            if (fromSaved)
            {
                data.LoadEntities(out List<Entity> entities);
                Entities = entities;
            }
            else
                GenerateEntities(cultureSize * cultureCount);

            Parallel.For(0, Entities.Count, (i) =>
            {
                Entities[i].EvaluateFitness();
            });

            cultures = new List<Culture>();
            for (int i = 0; i < cultureCount; ++i)
            {
                cultures.Add(new Culture(Entities, data, i * cultureSize, (i + 1) * cultureSize, cultureSize / 10 - 1));
                foreach (var observer in observers)
                    observer.OnNext(cultures[i].Champion);
            }
        }

        public Task Start()
        {
            if (isRunning)
                return null;

            isRunning = true;

            overlords = new List<Task<Entity>>();
            foreach(var culture in cultures)
                overlords.Add(culture.Develop(Mode.Grow, 1));

            return Run();
        }

        public void Stop(bool save)
        {
            isRunning = false;

            if (save)
                data.SaveEntities(Entities);

            for(int i = observers.Count - 1; i >= 0; --i)
                observers[i].OnCompleted();
        }

        private readonly Dictionary<int, (Mode, double)> modes = new Dictionary<int, (Mode, double)>
        {
            {0, (Mode.Balance, 0.2) },
            {1, (Mode.Balance, 0.25) },
            {2, (Mode.Shrink, 0.5) },
            {3, (Mode.Grow, 0.1) }
        };

        private async Task Run()
        {
            while (isRunning)
            {
                var completed = await Task.WhenAny(overlords);

                foreach (var observer in observers)
                    observer.OnNext(completed.GetAwaiter().GetResult());

                int index = overlords.IndexOf(completed);
                overlords[index] = cultures[index].Develop(modes[index].Item1, modes[index].Item2);
            }
        }

        private void GenerateEntities(int entityCount)
        {
            Entities = new List<Entity>();
            var initialStructure = new List<Gene>();
            var inputGuids = new List<Guid>();
            var outputGuids = new List<Guid>();
            var rand = new Random();
            var cluster = new Cluster(rand);
            int inputSize = data.InputWidth;
            int outputSize = data.OutputWidth;

            // Creating GUIDS for input neurons
            for (int i = 0; i < inputSize; ++i)
                inputGuids.Add(Guid.NewGuid());

            // Creating GUIDS for output neurons
            for (int i = 0; i < outputSize; ++i)
                outputGuids.Add(Guid.NewGuid());

            // Linking input neurons to the reference node and the seed node
            foreach (var iGuid in inputGuids)
                initialStructure.Add((Cluster.InputGuid, iGuid, 0));

            // Linking output neurons to the reference node and the seed node
            foreach (var oGuid in outputGuids)
                initialStructure.Add((oGuid, Cluster.OutputGuid, 0));

            for (int i = 0; i < entityCount; ++i)
            {
                Entities.Add(new Entity(initialStructure, data));

                Entities[i].Genes.Add((Cluster.BiasMark, Cluster.SeedGuid, cluster.RandomSynapseStrength()));
                Entities[i].Genes.Add((Cluster.RefactoryMark, Cluster.SeedGuid, Math.Abs(cluster.RandomSynapseStrength())));

                var randInput = inputGuids[rand.Next(inputGuids.Count)];
                var randOutput = outputGuids[rand.Next(outputGuids.Count)];
                Entities[i].Genes.Add((randInput, Cluster.SeedGuid, cluster.RandomSynapseStrength()));
                Entities[i].Genes.Add((Cluster.SeedGuid, randOutput, cluster.RandomSynapseStrength()));
            }
        }
    }

    internal class Unsubscriber<List> : IDisposable
    {
        private List<IObserver<Entity>> _observers;
        private readonly IObserver<Entity> _observer;

        internal Unsubscriber(List<IObserver<Entity>> observers, IObserver<Entity> observer)
        {
            _observers = observers;
            _observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);
        }
    }
}