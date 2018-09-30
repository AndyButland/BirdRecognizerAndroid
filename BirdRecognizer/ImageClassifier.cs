namespace BirdRecognizer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Android.App;
    using Android.Graphics;
    using Org.Tensorflow.Contrib.Android;

    public class ImageClassifier
    {
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
            const int InputSize = 227;
            const int ColorDimensions = 3;  // R,G,B

            var outputNames = new[] { OutputName };
            var floatValues = GetBitmapPixels(bitmap, InputSize, ColorDimensions);
            var outputs = new float[_labels.Count];

            _inferenceInterface.Feed(InputName, floatValues, 1, InputSize, InputSize, ColorDimensions);
            _inferenceInterface.Run(outputNames);
            _inferenceInterface.Fetch(OutputName, outputs);

            var results = outputs
                .Select((x, i) => new ImageClassificationResult(_labels[i], x))
                .ToList();

            return results.OrderByDescending(t => t.Probability).First();
        }

        private static float[] GetBitmapPixels(Bitmap bitmap, int inputSize, int colorDimensions)
        {
            var floatValues = new float[inputSize * inputSize * colorDimensions];

            using (var scaledBitmap = Bitmap.CreateScaledBitmap(bitmap, inputSize, inputSize, false))
            {
                using (var resizedBitmap = scaledBitmap.Copy(Bitmap.Config.Argb8888, false))
                {
                    var intValues = new int[inputSize * inputSize];
                    resizedBitmap.GetPixels(intValues, 0, resizedBitmap.Width, 0, 0, resizedBitmap.Width, resizedBitmap.Height);
                    resizedBitmap.Recycle();
                }

                scaledBitmap.Recycle();
            }

            return floatValues;
        }
    }
}