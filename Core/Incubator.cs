using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public class Incubator : IObservable<Entity>
    {
        public List<Entity> Entities { get; set; }
        
        private bool isRunning;
        private Data dataCollection;
        private List<IObserver<Entity>> observers;
        private Dictionary<Task<Entity>, Culture> overlords;

        private Dictionary<Culture, (Mode mode, double mutationRate)> cultures;

        public Incubator(Data _data, bool fromSaved, int cultureCount, int cultureSize)
        {
            observers = new List<IObserver<Entity>>();

            dataCollection = _data;
            isRunning = false;

            Populate(fromSaved, cultureCount, cultureSize);
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

            dataCollection.FetchTrainingData(out List<Datum> trainingData, 10, false);

            if (fromSaved)
            {
                dataCollection.LoadEntities(out List<Entity> entities);
                Entities = entities;
            }
            else
                GenerateEntities(cultureSize * cultureCount, trainingData[0].Input.Count, trainingData[0].Output.Count);

            // TEMPORARY
            var modes = new List<(Mode, double)>
            {
                (Mode.Balance, 0.2),
                (Mode.Shrink, 0.4),
                (Mode.Grow, 0.3),
                (Mode.Balance, 0.5),
            };
            // *********

            cultures = new Dictionary<Culture, (Mode, double)>();
            for (int i = 0; i < cultureCount; ++i)
            {
                var culture = new Culture(Entities, trainingData, i * cultureSize, (i + 1) * cultureSize, cultureSize / 10);
                cultures.Add(culture, modes[i]);

                if (fromSaved)
                    foreach (var observer in observers)
                        observer.OnNext(culture.Champion);
            }
        }

        public Task Start()
        {
            if (isRunning)
                return null;

            isRunning = true;

            overlords = new Dictionary<Task<Entity>, Culture>();
            foreach (var culture in cultures.Keys)
                overlords.Add(culture.EvaluateAll(), culture);

            return Run();
        }

        public void Stop(bool save)
        {
            isRunning = false;

            if (save)
                dataCollection.SaveEntities(Entities);

            for(int i = observers.Count - 1; i >= 0; --i)
                observers[i].OnCompleted();
        }

        private async Task Run()
        {
            while (isRunning)
            {
                var completed = await Task.WhenAny(overlords.Keys);

                foreach (var observer in observers)
                    observer.OnNext(completed.GetAwaiter().GetResult());

                var culture = overlords[completed];
                overlords.Remove(completed);
                overlords.Add(culture.Develop(cultures[culture].mode, cultures[culture].mutationRate), culture);
            }
        }

        private void GenerateEntities(int entityCount, int inputSize, int outputSize)
        {
            Entities = new List<Entity>();
            var initialStructure = new List<Gene>();
            var inputGuids = new List<Guid>();
            var outputGuids = new List<Guid>();

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
                Entities.Add(new Entity(initialStructure));

                Entities[i].Genes.Add((Cluster.BiasMark, Cluster.SeedGuid, Neuron.RandomSynapseStrength()));

                var randInput = inputGuids[R.NG.Next(inputGuids.Count)];
                var randOutput = outputGuids[R.NG.Next(outputGuids.Count)];
                Entities[i].Genes.Add((randInput, Cluster.SeedGuid, Neuron.RandomSynapseStrength()));
                Entities[i].Genes.Add((Cluster.SeedGuid, randOutput, Neuron.RandomSynapseStrength()));
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