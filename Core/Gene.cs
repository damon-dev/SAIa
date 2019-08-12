using System;
using System.Runtime.Serialization;

namespace Core
{
    /// <summary>
    /// Describes a edge with its weight in the graph.
    /// </summary>
    /// 
    [Serializable]
    public struct Gene : IComparable<Gene>, IEquatable<Gene> ,ISerializable
    {
        public Guid Source { get; set; }
        public Guid Destination { get; set; }
        public double Strength { get; set; }

        public bool Equals(Gene other)
        {
            return Source.Equals(other.Source) && Destination.Equals(other.Destination);
        }

        public int CompareTo(Gene other)
        {
            if (Source == other.Source)
            {
                if (Destination == other.Destination)
                    return Strength.CompareTo(other.Strength);

                return Destination.CompareTo(other.Destination);
            }

            return Source.CompareTo(other.Source);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Source), Source.ToString(), typeof(string));
            info.AddValue(nameof(Destination), Destination.ToString(), typeof(string));
            info.AddValue(nameof(Strength), Strength);
        }

        public static implicit operator Gene((Guid src, Guid dest, double str) s)
        {
            var g = new Gene
            {
                Source = s.src,
                Destination = s.dest,
                Strength = s.str
            };

            return g;
        }

        public Gene(SerializationInfo info, StreamingContext context)
        {
            Source = new Guid((string)info.GetValue(nameof(Source), typeof(string)));
            Destination = new Guid((string)info.GetValue(nameof(Destination), typeof(string)));
            Strength = (double)info.GetValue(nameof(Strength), typeof(double));
        }
    }
}
