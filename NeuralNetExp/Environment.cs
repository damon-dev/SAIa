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
        private List<Task<Chromosome>> breedTasks;

        public Environment()
        {
            observers = new List<IObserver<List<Gene>>>();
            isRunning = false;
        }

        public IDisposable Subscribe(IObserver<List<Gene>> observer)
        {
            // Check whether observer is already registered. If not, add it
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }

            return new Unsubscriber<List<Gene>>(observers, observer);
        }

        public Task Start(DataCollection data)
        {
            if (isRunning)
                return null;

            isRunning = true;

            breedTasks = new List<Task<Chromosome>>();

            // generate population
            population = new Population(2, 1, data);

            for (int i = 0; i < 6; ++i)
                breedTasks.Add(Breed());

            return Run();
        }

        public void Stop()
        {
            isRunning = false;

            for(int i = observers.Count - 1; i >= 0; --i)
            {
                observers[i].OnCompleted();
            }
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
       private Task<Chromosome> Breed()
        {
            return Task.Run(() =>
            {
                int motherIndex, fatherIndex;

                Chromosome mother = null;
                Chromosome father = null;

                motherIndex = population.Tournament(null);
                mother = population.Members[motherIndex];

                fatherIndex = population.Tournament(mother);
                father = population.Members[fatherIndex];

                var child = Chromosome.CrossOver(mother, father);

                child.EvaluateFitness();

                if (child.FitnessValue > population.Members[0].FitnessValue)
                    population.Members[0] = child;
                else if (mother.FitnessValue >= father.FitnessValue)
                    population.Members[fatherIndex] = child;
                else
                    population.Members[motherIndex] = child;

                return population.Members[0];
            });
        }
    }

    internal class Unsubscriber<List> : IDisposable
    {
        private List<IObserver<List<Gene>>> _observers;
        private IObserver<List<Gene>> _observer;

        internal Unsubscriber(List<IObserver<List<Gene>>> observers, IObserver<List<Gene>> observer)
        {
            this._observers = observers;
            this._observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);
        }
    }
}