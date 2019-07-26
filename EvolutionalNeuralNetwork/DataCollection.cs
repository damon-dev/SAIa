using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EvolutionalNeuralNetwork
{
    public class DataCollection
    {
        private const string genealogyPath = "Geneaology/culture.json";

        public int InputWidth { get; protected set; }
        public int OutputWidth { get; protected set; }

        protected List<List<double>> TrainingInput;
        protected List<List<double>> TrainingOutput;
        protected List<List<double>> TestInput;
        protected List<List<double>> TestOutput;

        public DataCollection()
        {
            TrainingInput = new List<List<double>>();
            TrainingOutput = new List<List<double>>();
            TestInput = new List<List<double>>();
            TestOutput = new List<List<double>>();
        }

        public virtual void FetchTrainingData(out List<List<double>> input, out List<List<double>> output, int count)
        {
            input = TrainingInput.Take(count).ToList();
            output = TrainingOutput.Take(count).ToList();
        }

        public virtual void FetchTestData(out List<List<double>> input, out List<List<double>> output, int count)
        {
            input = TestInput.Take(count).ToList();
            output = TestOutput.Take(count).ToList();
        }

        public bool LoadCulture(out Culture culture)
        {
            try
            {
                CultureContainer container = null;

                // deserialize JSON directly from a file
                using (var file = File.OpenText(genealogyPath))
                {
                    var serializer = new JsonSerializer();
                    container = (CultureContainer)serializer.Deserialize(file, typeof(CultureContainer));
                }

                var entities = container.entities.Select(e => new Entity(e.Genes, this, e.FitnessValue)).ToList();
                culture = new Culture(this, entities);

                return true;
            }
            catch (Exception)
            {
                culture = null;
                return false;
            }
        }

        public bool SaveCulture(Culture culture)
        {
            try
            {
                if (culture == null) return false;

                var container = new CultureContainer(culture.Entities);
                Directory.CreateDirectory("Geneaology");
                // serialize JSON directly to a file
                using (var file = File.CreateText(genealogyPath))
                {
                    var serializer = new JsonSerializer();
                    serializer.Serialize(file, container);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    class CultureContainer
    {
        public Entity[] entities;

        public CultureContainer(List<Entity> list)
        {
            entities = list?.ToArray();
        }
    }
}
