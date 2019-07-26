using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvolutionalNeuralNetwork
{

    public class Environment : IObservable<List<Gene>>
    {
        private Culture culture;
        private bool isRunning;
        private DataCollection data;
        private List<IObserver<List<Gene>>> observers;
        private List<Task<Entity>> breedTasks;

        public Environment()
        {
            observers = new List<IObserver<List<Gene>>>();
            isRunning = false;
        }

        public IDisposable Subscribe(IObserver<List<Gene>> observer)
        {
            // Check whether observer is already registered. If not, add it
            if (!observers.Contains(observer))
                observers.Add(observer);

            return new Unsubscriber<List<Gene>>(observers, observer);
        }

        public Task Start(DataCollection _data, bool fromSaved, int threadCount)
        {
            data = _data;

            if (isRunning)
                return null;

            isRunning = true;

            breedTasks = new List<Task<Entity>>();

            if (fromSaved)
                data.LoadCulture(out culture);
            else
                // generate culture
                culture = new Culture(data);

            for (int i = 0; i < threadCount; ++i)
                breedTasks.Add(Breed());

            return Run();
        }

        public void Stop()
        {
            isRunning = false;

            data.SaveCulture(culture);

            for(int i = observers.Count - 1; i >= 0; --i)
                observers[i].OnCompleted();
        }

        private async Task Run()
        {
            while (isRunning)
            {
                var completed = await Task.WhenAny(breedTasks);

                foreach (var observer in observers)
                    observer.OnNext(completed.GetAwaiter().GetResult().Genes);

                int index = breedTasks.IndexOf(completed);
                breedTasks[index] = Breed();
            }
        }

        // Returns the fittest member at the point the task completed
        private Task<Entity> Breed()
        {
            return Task.Run(() =>
            {
                int motherIndex, fatherIndex;

                Entity mother = null;
                Entity father = null;

                var rand = new Random();

                motherIndex = culture.Tournament(null, rand);
                mother = culture.Entities[motherIndex];

                fatherIndex = culture.Tournament(mother, rand);
                father = culture.Entities[fatherIndex];

                var child = mother.CrossOver(father, rand);

                child.EvaluateFitness(rand);

                if (child.FitnessValue > culture.Entities[0].FitnessValue)
                    culture.Entities[0] = child;
                else
                {
                    int prey = culture.Victim(rand);
                    if (child.FitnessValue > culture.Entities[prey].FitnessValue)
                        culture.Entities[prey] = child;

                    else if (child.FitnessValue > culture.Entities[fatherIndex].FitnessValue)
                        culture.Entities[fatherIndex] = child;
                    else if (child.FitnessValue > culture.Entities[motherIndex].FitnessValue)
                        culture.Entities[motherIndex] = child;
                    else if (culture.Entities[fatherIndex].FitnessValue < mother.FitnessValue)
                        culture.Entities[fatherIndex] = child;
                    else if (culture.Entities[motherIndex].FitnessValue < father.FitnessValue)
                        culture.Entities[motherIndex] = child;
                }

                return culture.Entities[0];
            });
        }
    }

    internal class Unsubscriber<List> : IDisposable
    {
        private List<IObserver<List<Gene>>> _observers;
        private readonly IObserver<List<Gene>> _observer;

        internal Unsubscriber(List<IObserver<List<Gene>>> observers, IObserver<List<Gene>> observer)
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