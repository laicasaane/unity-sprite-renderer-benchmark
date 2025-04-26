using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

namespace SpriteBenchmark
{
    public class SpriteRendererTest : MonoBehaviour
    {
        private BenchmarkConfig _config;
        private Transform _parent;
        private SpriteRenderer[] _rendererArray;

        [SetUp]
        public void SetUpTest()
        {
            var config = _config = Resources.Load<BenchmarkConfig>(nameof(BenchmarkConfig));
            var count = config.count;
            var parent = _parent = new GameObject().transform;
            var rendererArray = _rendererArray = new SpriteRenderer[count];

            for (var i = 0; i < count; i++)
            {
                var go = new GameObject($"char-{i}");
                go.transform.SetParent(parent);
                rendererArray[i] = go.AddComponent<SpriteRenderer>();
            }
        }

        [TearDown]
        public void TearDownTest()
        {
            Object.DestroyImmediate(_parent.gameObject);
        }

        [Test, Performance]
        public void RunTest()
        {
            Measure.Method(() => {
                var rendererArray = _rendererArray;
                var renderersLength = rendererArray.Length;
                var sprites = _config.sprites;
                var spritesLength = sprites.Length;

                for (var i = 0; i < renderersLength; i++)
                {
                    var spriteIndex = Random.Range(0, spritesLength);
                    var sprite = sprites[spriteIndex];
                    var renderer = rendererArray[i];
                    renderer.sprite = sprite;
                }
            })
                .WarmupCount(5)
                .IterationsPerMeasurement(100)
                .MeasurementCount(10)
                .Run();
        }
    }
}
