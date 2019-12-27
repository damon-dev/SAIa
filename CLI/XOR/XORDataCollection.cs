using Core;
using System;
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

            InputFeatureCount = 2;
            OutputFeatureCount = 1;

            SuccessCondition = (expected, predicted) =>
            {
                if (Math.Abs(predicted[0] - expected[0]) < .001)
                    return true;

                return false;
            };
        }
    }
}
