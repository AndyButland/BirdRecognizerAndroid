namespace BirdRecognizer
{
    using System;

    public class ImageClassificationResult
    {
        public ImageClassificationResult( string predictedTag, float probability)
        {
            PredictedTag = predictedTag;
            Probability = probability;
        }

        public string PredictedTag { get; }

        public float Probability { get; }

        public override string ToString() => $"{PredictedTag} ({Math.Round(Probability * 100, 2)}%)";
    }
}