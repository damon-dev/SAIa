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
        private Storage storage;
        private List<IObserver<Entity>> observers;
        private Dictionary<Task<Entity>, Culture> overlords;
        private List<Culture> cultures;

        public Incubator()
        {
            observers = new List<IObserver<Entity>>();

            if (MutationCatalog == null)
                MutationCatalog = new ConcurrentDictionary<(Guid, Guid), Guid>();

            storage = new Storage();
            isRunning = false;
        }

        public IDisposable Subscribe(IObserver<Entity> observer)
        {
            // Check whether observer is already registered. If not, add it
            if (!observers.Contains(observer))
                observers.Add(observer);

            return new Unsubscriber<Entity>(observers, observer);
        }

        public void Populate(bool fromSaved, int cultureSize, List<Agent> cultureSeeds)
        {
            List<Entity> entities;
            if (cultureSeeds?.Count < 1)
                throw new ArgumentException(nameof(cultureSeeds));

            int cultureCount = cultureSeeds.Count;

            //await world.RequestIOSizes(out int inputSize, out int outputSize);
            int inputSize = 4; int outputSize = 1; // TODO: temporary

            if (fromSaved)
                storage.LoadEntities(out entities);
            else
                entities = GenerateEntities(cultureSize * cultureCount, inputSize, outputSize);

            cultures = new List<Culture>();
            var cfg = new CultureConfiguration(CultureConfiguration.Shrink);
            //cultures.Add(new Culture(this, cultureSeeds[0], cfg));

            for (int i = 0; i < cultureCount; ++i)
            {
                cfg = new CultureConfiguration(CultureConfiguration.Balance);
                cultures.Add(new Culture(this, cultureSeeds[i], cfg));
            }

            //cfg = new CultureConfiguration(CultureConfiguration.Grow);
            //cultures.Add(new Culture(this, cultureSeeds[cultureSeeds.Count - 1], cfg));

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

            overlords = new Dictionary<Task<Entity>, Culture>();

            // let all of them play a game each
            foreach (var culture in cultures)
                overlords.Add(culture.EvaluateAll(), culture);

            await Run();
        }

        private async Task Run()
        {
            while (isRunning && overlords.Count > 0)
            {
                var completedRun = await Task.WhenAny(overlords.Keys);
                var bestEntity = await completedRun;

                foreach (var observer in observers)
                    observer.OnNext(bestEntity);

                var culture = overlords[completedRun];
                overlords.Remove(completedRun);

                if (bestEntity.Fitness < 200)
                    overlords.Add(culture.Develop(), culture);
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

                storage.SaveEntities(entities);
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
                //initialStructure.Add((Brain.BiasMark, iGuid, 0));
                initialStructure.Add((Brain.InputGuid, iGuid, 0));
            }

            // Linking output neurons to the reference node and the seed node
            foreach (var oGuid in outputGuids)
            {
                //initialStructure.Add((Brain.BiasMark, oGuid, 0));
                initialStructure.Add((oGuid, Brain.OutputGuid, 0));
            }

            for (int i = 0; i < entityCount; ++i)
            {
                var genes = new List<Gene>(initialStructure);
                for (int k = 0; k < inputGuids.Count; ++k)
                    for (int p = 0; p < outputGuids.Count; p++)
                    {
                        var randInput = inputGuids[k];
                        var randOutput = outputGuids[p];
                        genes.Add((randInput, randOutput, Neuron.RandomSynapseStrength()));
                    }

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