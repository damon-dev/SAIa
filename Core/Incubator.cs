using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    // ／人◕ ‿‿ ◕人＼ Contract?
    public class Incubator : IObservable<Entity>
    {
        public static ConcurrentDictionary<(Guid, Guid), Guid> MutationCatalog;

        private bool isRunning;
        private Data dataCollection;
        private List<IObserver<Entity>> observers;
        private Dictionary<Task<Entity>, Culture> overlords;
        private List<Culture> cultures;

        public Incubator(Data _data, bool fromSaved, int cultureCount, int cultureSize)
        {
            observers = new List<IObserver<Entity>>();

            if (MutationCatalog == null)
                MutationCatalog = new ConcurrentDictionary<(Guid, Guid), Guid>();

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
            List<Entity> entities;

            if (fromSaved)
                dataCollection.LoadEntities(out entities);
            else
                entities = GenerateEntities(cultureSize * cultureCount, dataCollection.InputFeatureCount, dataCollection.OutputFeatureCount);

            // TODO: ******** TEMPORARY ********
            cultures = new List<Culture>();
            var cfg = new CultureConfiguration(CultureConfiguration.Shrink);
            cfg.SuccessFunction = dataCollection.SuccessCondition;
            cultures.Add(new Culture(this, cfg));

            cfg = new CultureConfiguration(CultureConfiguration.Balance);
            cfg.SuccessFunction = dataCollection.SuccessCondition;
            cultures.Add(new Culture(this, cfg));

            cfg = new CultureConfiguration(CultureConfiguration.Balance);
            cfg.SuccessFunction = dataCollection.SuccessCondition;
            cultures.Add(new Culture(this, cfg));

            cfg = new CultureConfiguration(CultureConfiguration.Balance);
            cfg.SuccessFunction = dataCollection.SuccessCondition;
            cultures.Add(new Culture(this, cfg));

            cfg = new CultureConfiguration(CultureConfiguration.Grow);
            cfg.SuccessFunction = dataCollection.SuccessCondition;
            cultures.Add(new Culture(this, cfg));
            // *********************************

            for (int i = 0; i < cultureCount; ++i)
            {
                for (int j = 0; j < cultureSize; ++j)
                {
                    cultures[i].Entities.Add(entities[cultureSize * i + j]);
                    entities[cultureSize * i + j].HostCulture = cultures[i];
                }
            }

            if (fromSaved)
            {
                foreach (var culture in cultures)
                    foreach (var observer in observers)
                        observer.OnNext(culture.Entities[0]);
            }
        }

        public async Task Start()
        {
            if (isRunning) return;

            isRunning = true;
            int startingFeatures = 10;

            overlords = new Dictionary<Task<Entity>, Culture>();
            dataCollection.FetchTrainingData(out List<Datum> features, startingFeatures, false);

            foreach (var culture in cultures)
                overlords.Add(culture.EvaluateAll(features, false), culture);

            for (int i = startingFeatures; i < 100; ++i)
            {
                await Run(features);

                if (isRunning)
                {
                    dataCollection.FetchTrainingData(out features, i + 1, false);
                    foreach (var culture in cultures)
                        overlords.Add(culture.EvaluateAll(new List<Datum> { features[i] }, true), culture);
                }
            }

            await Run(features);
        }

        private async Task Run(List<Datum> features)
        {
            while (isRunning && overlords.Count > 0)
            {
                var completed = await Task.WhenAny(overlords.Keys);
                var result = completed.GetAwaiter().GetResult();

                foreach (var observer in observers)
                    observer.OnNext(result);

                var culture = overlords[completed];
                overlords.Remove(completed);

                if (!result.Successful)
                    overlords.Add(culture.Develop(features), culture);
                else
                {
                    await Task.WhenAll(overlords.Keys);
                    overlords.Clear();
                }
            }
        }

        public void Stop(bool save)
        {
            isRunning = false;

            if (save)
            {
                var entities = new List<Entity>();
                for (int i = 0; i < cultures.Count; ++i)
                    entities.AddRange(cultures[i].Entities);

                dataCollection.SaveEntities(entities);
            }

            for (int i = observers.Count - 1; i >= 0; --i)
                observers[i].OnCompleted();
        }

        public Culture RandomCulture(Culture otherThanThis)
        {
            if (cultures.Count <= 1) return otherThanThis;
            int index = R.NG.Next(cultures.Count);
            while(cultures[index].Equals(otherThanThis))
                index = R.NG.Next(cultures.Count);

            return cultures[index];
        }

        private List<Entity> GenerateEntities(int entityCount, int inputSize, int outputSize)
        {
            var entities = new List<Entity>();
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
            {
                initialStructure.Add((Cluster.BiasMark, iGuid, 0));
                initialStructure.Add((Cluster.InputGuid, iGuid, 0));
            }

            // Linking output neurons to the reference node and the seed node
            foreach (var oGuid in outputGuids)
            {
                initialStructure.Add((Cluster.BiasMark, oGuid, 0));
                initialStructure.Add((oGuid, Cluster.OutputGuid, 0));
            }

            for (int i = 0; i < entityCount; ++i)
            {
                var randInput = inputGuids[R.NG.Next(inputGuids.Count)];
                var randOutput = outputGuids[R.NG.Next(outputGuids.Count)];
                var genes = new List<Gene>(initialStructure)
                {
                    (randInput, randOutput, Neuron.RandomSynapseStrength())
                };

                entities.Add(new Entity(genes));
            }

            return entities;
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