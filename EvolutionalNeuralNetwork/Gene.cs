using System;

namespace EvolutionalNeuralNetwork
{
    /// <summary>
    /// Describes a edge with its weight in the graph.
    /// </summary>
    public struct Gene : IComparable<Gene>
    {
        public Guid Source { get; set; }
        public Guid Destination { get; set; }
        public double Strength { get; set; }

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
    }
}
