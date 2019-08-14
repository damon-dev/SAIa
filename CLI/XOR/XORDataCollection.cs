using Core;
using System.Collections.Generic;

namespace CLI.XOR
{
    public class XORDataCollection : Data
    {
        public XORDataCollection()
        {
            Training = new List<Datum>
            {
                new Datum(new List<double> {0, 0}, new List<double> {0}),
                new Datum(new List<double> {0, 1}, new List<double> {1}),
                new Datum(new List<double> {1, 0}, new List<double> {1}),
                new Datum(new List<double> {1, 1}, new List<double> {0})
            };
        }
    }
}
