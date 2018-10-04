namespace BirdRecognizer
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Android.App;
    using Android.Graphics;
    using Org.Tensorflow.Contrib.Android;

    public class ImageClassifier
    {
        private const int InputSize = 227;
        private const int ColorDimensions = 3;  // R,G,B

        private readonly List<string> _labels;
        private readonly TensorFlowInferenceInterface _inferenceInterface;

        public ImageClassifier()
        {
            const string ModelFileName = "model.pb";
            const string LabelsFileName = "labels.txt";

            var assets = Application.Context.Assets;

            _inferenceInterface = new TensorFlowInferenceInterface(assets, ModelFileName);

            using (var sr = new StreamReader(assets.Open(LabelsFileName)))
            {
                var content = sr.ReadToEnd();
                _labels = LoadLabels(content);
            }
        }

        private static List<string> LoadLabels(string content)
        {
            return content.Split('\n')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        public ImageClassificationResult RecognizeImage(Bitmap bitmap)
        {
            const string InputName = "Placeholder";
            const string OutputName = "loss";

            var outputNames = new[] { OutputName };
            var floatValues = GetBitmapPixels(bitmap:);
            var outputs = new float[_labels.Count];

            _inferenceInterface.Feed(InputName, floatValues, 1, InputSize, InputSize, ColorDimensions);
            _inferenceInterface.Run(outputNames);
            _inferenceInterface.Fetch(OutputName, outputs);

            var results = outputs
                .Select((x, i) => new ImageClassificationResult(_labels[i], x))
                .ToList();

            return results.OrderByDescending(t => t.Probability).First();
        }

        private static float[] GetBitmapPixels(Bitmap bitmap)
        {
            var floatValues = new float[InputSize * InputSize * ColorDimensions];

            using (var scaledBitmap = Bitmap.CreateScaledBitmap(bitmap, InputSize, InputSize, false))
            {
                using (var resizedBitmap = scaledBitmap.Copy(Bitmap.Config.Argb8888, false))
                {
                    var intValues = new int[InputSize * InputSize];
                    resizedBitmap.GetPixels(intValues, 0, resizedBitmap.Width, 0, 0, resizedBitmap.Width, resizedBitmap.Height);

                    for (var i = 0; i < intValues.Length; ++i)
                    {
                        SetColorDimensionValues(floatValues, i, intValues[i]);
                    }

                    resizedBitmap.Recycle();
                }

                scaledBitmap.Recycle();
            }

            return floatValues;
        }

        private static void SetColorDimensionValues(float[] floatValues, int index, int pixelValue)
        {
            var indexOffset = index * ColorDimensions;
            floatValues[indexOffset + 0] = pixelValue & 0xFF;
            floatValues[indexOffset + 1] = (pixelValue >> 8) & 0xFF;
            floatValues[indexOffset + 2] = (pixelValue >> 16) & 0xFF;
        }
    }
}