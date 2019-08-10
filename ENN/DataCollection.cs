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

        public virtual void FetchTrainingData(out List<List<double>> input, out List<List<double>> output, int count, bool random)
        {
            input = TrainingInput.Take(count).ToList();
            output = TrainingOutput.Take(count).ToList();
        }

        public virtual void FetchTestData(out List<List<double>> input, out List<List<double>> output, int count)
        {
            input = TestInput.Take(count).ToList();
            output = TestOutput.Take(count).ToList();
        }

        public bool LoadEntities(out List<Entity> entities, int count = 0)
        {
            try
            {
                EntityContainer container = null;

                // deserialize JSON directly from a file
                using (var file = File.OpenText(genealogyPath))
                {
                    var serializer = new JsonSerializer();
                    container = (EntityContainer)serializer.Deserialize(file, typeof(EntityContainer));
                }

                if (count <= 0 || count > container.Entities.Length)
                    count =  container.Entities.Length;

                entities = container.Entities.Select(e => new Entity(e.Genes, this, e.FitnessValue))
                                                 .Take(count)
                                                 .ToList();
                return true;
            }
            catch (Exception)
            {
                entities = null;
                return false;
            }
        }

        public bool SaveEntities(List<Entity> entities)
        {
            try
            {
                if (entities == null) return false;

                var container = new EntityContainer(entities);

                Directory.CreateDirectory("Geneaology");
                // serialize JSON directly to a file
                using (var file = File.CreateText(genealogyPath))
                {
                    var serializer = new JsonSerializer
                    {
                        Formatting = Formatting.Indented
                    };
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

    class EntityContainer
    {
        public Entity[] Entities;

        public EntityContainer(List<Entity> list)
        {
            Entities = list?.ToArray();
        }
    }
}
