using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core
{
    public class Data
    {
        // TODO: able to change from the program
        private const string genealogyPath = "Geneaology/culture.json";

        public int InputFeatureCount { get; protected set; }
        public int OutputFeatureCount { get; protected set; }

        protected List<List<double>> TrainingInput;
        protected List<List<double>> TrainingOutput;
        protected List<List<double>> TestInput;
        protected List<List<double>> TestOutput;

        public Data()
        {
            TrainingInput = new List<List<double>>();
            TrainingOutput = new List<List<double>>();
            TestInput = new List<List<double>>();
            TestOutput = new List<List<double>>();
        }

        public virtual void FetchTrainingData(out List<List<double>> input, out List<List<double>> output, int count = 0, bool random = false)
        {
            if (count <= 0 || count > TrainingInput.Count)
                count = TrainingInput.Count;

            if (count > TrainingInput.Count / 2 && random)
                throw new ArgumentException(nameof(count), "If random is true, no more than half of the training samples are allowed to be requested.");

            input = new List<List<double>>();
            output = new List<List<double>>();

            var rand = new Random();
            var used = new HashSet<int>();

            for (int i = 0; i < count; ++i)
            {
                int index;
                if (random)
                {
                    index = rand.Next(TrainingInput.Count);
                    while (used.Contains(index))
                        index = rand.Next(TrainingInput.Count);
                }
                else
                    index = i;

                used.Add(index);
                input.Add(TrainingInput[index]);
                output.Add(TrainingOutput[index]);
            }
        }

        public virtual void FetchTestData(out List<List<double>> input, out List<List<double>> output, int count)
        {
            if (count <= 0 || count > TestInput.Count)
                count = TestInput.Count;

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

        public bool SaveEntities(List<Entity> entities, int count = 0)
        {
            try
            {
                if (entities == null) return false;

                if (count <= 0 || count > entities.Count)
                    count = entities.Count;

                var container = new EntityContainer(entities.Take(count).ToList());

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
