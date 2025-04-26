using UnityEngine;

namespace SpriteBenchmark
{
    [CreateAssetMenu(fileName = "BenchmarkConfig", menuName = "SpriteBenchmark/Benchmark Config")]
    public class BenchmarkConfig : ScriptableObject
    {
        public int count = 1000;
        public Sprite[] sprites;
    }
}
