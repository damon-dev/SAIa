using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvolutionalNeuralNetwork
{

    public class Environment : IObservable<List<Gene>>
    {
        private Population population;
        private bool isRunning;
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

        public Task Start(DataCollection data)
        {
            if (isRunning)
                return null;

            isRunning = true;

            breedTasks = new List<Task<Entity>>();

            // generate population
            population = new Population(data);

            for (int i = 0; i < 5; ++i)
                breedTasks.Add(Breed());

            return Run();
        }

        public void Stop()
        {
            isRunning = false;

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

                motherIndex = population.Tournament(null, rand);
                mother = population.Members[motherIndex];

                fatherIndex = population.Tournament(mother, rand);
                father = population.Members[fatherIndex];

                var child = mother.CrossOver(father, rand);

                child.EvaluateFitness(rand);

                if (child.FitnessValue > population.Members[0].FitnessValue)
                    population.Members[0] = child;
                else
                {
                    int prey = population.Victim(rand);
                    if (child.FitnessValue > population.Members[prey].FitnessValue)
                        population.Members[prey] = child;

                    else if (child.FitnessValue > population.Members[fatherIndex].FitnessValue)
                        population.Members[fatherIndex] = child;
                    else if (child.FitnessValue > population.Members[motherIndex].FitnessValue)
                        population.Members[motherIndex] = child;
                    else if (population.Members[fatherIndex].FitnessValue < mother.FitnessValue)
                        population.Members[fatherIndex] = child;
                    else if (population.Members[motherIndex].FitnessValue < father.FitnessValue)
                        population.Members[motherIndex] = child;
                }

                return population.Members[0];
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