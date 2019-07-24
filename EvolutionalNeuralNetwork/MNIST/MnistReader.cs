using System.Collections.Generic;
using System.IO;

namespace EvolutionalNeuralNetwork.MNIST
{
    public static class MnistReader
    {
        private const string TrainImages = "MNIST/train-images.idx3-ubyte";
        private const string TrainLabels = "MNIST/train-labels.idx1-ubyte";
        private const string TestImages = "MNIST/t10k-images.idx3-ubyte";
        private const string TestLabels = "MNIST/t10k-labels.idx1-ubyte";

        public static IEnumerable<ByteImage> ReadTrainingData()
        {
            foreach (var item in Read(TrainImages, TrainLabels))
            {
                yield return item;
            }
        }

        public static IEnumerable<ByteImage> ReadTestData()
        {
            foreach (var item in Read(TestImages, TestLabels))
            {
                yield return item;
            }
        }

        private static IEnumerable<ByteImage> Read(string imagesPath, string labelsPath)
        {
            var labels = new BinaryReader(new FileStream(labelsPath, FileMode.Open));
            var images = new BinaryReader(new FileStream(imagesPath, FileMode.Open));

            int magicNumber = images.ReadBigInt32();
            int numberOfImages = images.ReadBigInt32();
            int width = images.ReadBigInt32();
            int height = images.ReadBigInt32();

            int magicLabel = labels.ReadBigInt32();
            int numberOfLabels = labels.ReadBigInt32();

            for (int i = 0; i < numberOfImages; i++)
            {
                yield return new ByteImage()
                {
                    Data = images.ReadBytes(width * height),
                    Label = labels.ReadByte()
                };
            }
        }
    }
}
