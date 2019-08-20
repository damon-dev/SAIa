using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core
{
    public class Datum
    {
        public List<double> Input { get; set; }
        public List<double> Output { get; set; }

        public Datum()
        {
            Input = new List<double>();
            Output = new List<double>();
        }

        public Datum(List<double> input, List<double> output)
        {
            Input = input;
            Output = output;
        }
    }

    public class Data
    {
        private const string genealogyPath = "Geneaology/culture.json";

        protected List<Datum> Training;
        protected List<Datum> Test;

        public int InputFeatureCount { get; set; }
        public int OutputFeatureCount { get; set; }

        public Data()
        {
            Training = new List<Datum>();
            Test = new List<Datum>();
        }

        public virtual void FetchTrainingData(out List<Datum> data, int count = 0, bool random = false)
        {
            if (count <= 0 || count > Training.Count)
                count = Training.Count;

            if (count > Training.Count / 2 && random)
                throw new ArgumentException(nameof(count), "If random is true, no more than half of the training samples are allowed to be requested.");

            data = new List<Datum>();

            var used = new HashSet<int>();

            for (int i = 0; i < count; ++i)
            {
                int index;
                if (random)
                {
                    index = R.NG.Next(Training.Count);
                    while (used.Contains(index))
                        index = R.NG.Next(Training.Count);
                }
                else
                    index = i;

                used.Add(index);
                data.Add(Training[index]);
            }
        }

        public virtual void FetchTestData(out List<Datum> data, int count)
        {
            if (count <= 0 || count > Test.Count)
                count = Test.Count;

            data = Test.Take(count).ToList();
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

                entities = container.Entities.Take(count).ToList();
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
