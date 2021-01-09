using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core
{
    public class Storage
    {
        // TODO: proper pathing
        private string genealogyPath;

        public Storage(string storagePath = "Geneaology/culture.json")
        {
            genealogyPath = storagePath;
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
                    throw new ArgumentOutOfRangeException(nameof(count));

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
