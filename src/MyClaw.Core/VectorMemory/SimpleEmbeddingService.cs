using System.Security.Cryptography;
using System.Text;

namespace MyClaw.Core.VectorMemory;

/// <summary>
/// 简单嵌入服务 - 基于特征哈希的文本嵌入
/// 注意：这是一个简化的实现，生产环境建议使用真正的嵌入模型
/// </summary>
public class SimpleEmbeddingService : IEmbeddingService
{
    private readonly int _dimension;
    private readonly HashSet<string> _vocabulary = new();
    private readonly Dictionary<string, float[]> _embeddingCache = new();

    public int Dimension => _dimension;

    public SimpleEmbeddingService(int dimension = 384)
    {
        _dimension = dimension;
    }

    public Task<float[]> EmbedAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new float[_dimension]);
        }

        // 检查缓存
        var cacheKey = GetHashKey(text);
        if (_embeddingCache.TryGetValue(cacheKey, out var cached))
        {
            return Task.FromResult(cached);
        }

        var embedding = ComputeEmbedding(text);

        // 缓存结果
        _embeddingCache[cacheKey] = embedding;

        // 限制缓存大小
        if (_embeddingCache.Count > 10000)
        {
            var keysToRemove = _embeddingCache.Keys.Take(1000).ToList();
            foreach (var key in keysToRemove)
            {
                _embeddingCache.Remove(key);
            }
        }

        return Task.FromResult(embedding);
    }

    public async Task<List<float[]>> EmbedBatchAsync(IEnumerable<string> texts)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
        {
            results.Add(await EmbedAsync(text));
        }
        return results;
    }

    /// <summary>
    /// 计算文本嵌入
    /// 使用多层特征哈希方法
    /// </summary>
    private float[] ComputeEmbedding(string text)
    {
        var embedding = new float[_dimension];

        // 1. 字符级 n-gram 特征
        AddNgramFeatures(embedding, text, 2); // bi-gram
        AddNgramFeatures(embedding, text, 3); // tri-gram

        // 2. 词级特征
        AddWordFeatures(embedding, text);

        // 3. 位置感知特征
        AddPositionalFeatures(embedding, text);

        // 4. 全局文本特征
        AddGlobalFeatures(embedding, text);

        // L2 归一化
        NormalizeL2(embedding);

        return embedding;
    }

    /// <summary>
    /// 添加 n-gram 特征
    /// </summary>
    private void AddNgramFeatures(float[] embedding, string text, int n)
    {
        var normalizedText = text.ToLowerInvariant();
        for (int i = 0; i <= normalizedText.Length - n; i++)
        {
            var ngram = normalizedText.Substring(i, n);
            var hash = ComputeHash(ngram);
            var index = (int)(hash % _dimension);
            embedding[Math.Abs(index)] += 1.0f / n; // 短 n-gram 权重更高
        }
    }

    /// <summary>
    /// 添加词级特征
    /// </summary>
    private void AddWordFeatures(float[] embedding, string text)
    {
        var words = text.ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']' },
                StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            if (word.Length < 2) continue;

            var hash = ComputeHash(word);
            var index = (int)(hash % _dimension);

            // 词频权重，但限制最大值
            embedding[Math.Abs(index)] += Math.Min(1.0f, 1.0f / words.Length * 10);

            // 词首字母特征
            if (word.Length > 0)
            {
                var firstCharHash = ComputeHash("first_" + word[0]);
                var firstCharIndex = (int)(firstCharHash % _dimension);
                embedding[Math.Abs(firstCharIndex)] += 0.3f;
            }
        }
    }

    /// <summary>
    /// 添加位置感知特征
    /// </summary>
    private void AddPositionalFeatures(float[] embedding, string text)
    {
        var words = text.Split(new[] { ' ', '\t', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < Math.Min(words.Length, 10); i++)
        {
            var positionFeature = $"pos_{i}_{words[i].ToLowerInvariant()}";
            var hash = ComputeHash(positionFeature);
            var index = (int)(hash % _dimension);
            embedding[Math.Abs(index)] += 0.5f / (i + 1); // 位置越靠前权重越高
        }
    }

    /// <summary>
    /// 添加全局特征
    /// </summary>
    private void AddGlobalFeatures(float[] embedding, string text)
    {
        // 文本长度特征
        var lengthBucket = Math.Min(text.Length / 50, 10);
        var lengthHash = ComputeHash($"len_{lengthBucket}");
        embedding[Math.Abs((int)(lengthHash % _dimension))] += 0.2f;

        // 问号/感叹号特征
        if (text.Contains('?'))
        {
            var qHash = ComputeHash("has_question");
            embedding[Math.Abs((int)(qHash % _dimension))] += 0.5f;
        }
        if (text.Contains('!'))
        {
            var eHash = ComputeHash("has_exclamation");
            embedding[Math.Abs((int)(eHash % _dimension))] += 0.5f;
        }

        // 数字特征
        if (text.Any(char.IsDigit))
        {
            var dHash = ComputeHash("has_digit");
            embedding[Math.Abs((int)(dHash % _dimension))] += 0.3f;
        }

        // 代码特征
        if (text.Contains("```") || text.Contains("function") || text.Contains("class "))
        {
            var codeHash = ComputeHash("has_code");
            embedding[Math.Abs((int)(codeHash % _dimension))] += 0.5f;
        }
    }

    /// <summary>
    /// 计算字符串哈希
    /// </summary>
    private long ComputeHash(string input)
    {
        // 使用 SHA256 的前 8 字节作为哈希值
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToInt64(bytes, 0);
    }

    /// <summary>
    /// L2 归一化
    /// </summary>
    private void NormalizeL2(float[] vector)
    {
        double sum = 0;
        foreach (var v in vector)
        {
            sum += v * v;
        }

        var magnitude = Math.Sqrt(sum);
        if (magnitude > 0)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] = (float)(vector[i] / magnitude);
            }
        }
    }

    /// <summary>
    /// 获取文本的缓存键
    /// </summary>
    private string GetHashKey(string text)
    {
        var hash = ComputeHash(text);
        return hash.ToString();
    }

    /// <summary>
    /// 清除嵌入缓存
    /// </summary>
    public void ClearCache()
    {
        _embeddingCache.Clear();
    }
}
