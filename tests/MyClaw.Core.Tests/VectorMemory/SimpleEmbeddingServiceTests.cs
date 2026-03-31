using MyClaw.Core.VectorMemory;

namespace MyClaw.Core.Tests.VectorMemory;

public class SimpleEmbeddingServiceTests
{
    [Fact]
    public void Constructor_ShouldSetCorrectDimension()
    {
        var service = new SimpleEmbeddingService(384);

        Assert.Equal(384, service.Dimension);
    }

    [Fact]
    public async Task EmbedAsync_ShouldReturnCorrectDimension()
    {
        var service = new SimpleEmbeddingService(128);

        var embedding = await service.EmbedAsync("Test text");

        Assert.Equal(128, embedding.Length);
    }

    [Fact]
    public async Task EmbedAsync_ShouldReturnNonZeroForNonEmptyText()
    {
        var service = new SimpleEmbeddingService(128);

        var embedding = await service.EmbedAsync("This is a test sentence.");

        // 至少有一些非零值
        Assert.Contains(embedding, value => value != 0);
    }

    [Fact]
    public async Task EmbedAsync_ShouldReturnZeroVectorForEmptyText()
    {
        var service = new SimpleEmbeddingService(128);

        var embedding = await service.EmbedAsync("");

        Assert.All(embedding, v => Assert.Equal(0, v));
    }

    [Fact]
    public async Task EmbedAsync_ShouldReturnZeroVectorForWhitespace()
    {
        var service = new SimpleEmbeddingService(128);

        var embedding = await service.EmbedAsync("   ");

        Assert.All(embedding, v => Assert.Equal(0, v));
    }

    [Fact]
    public async Task EmbedAsync_ShouldReturnNormalizedVector()
    {
        var service = new SimpleEmbeddingService(128);

        var embedding = await service.EmbedAsync("Test normalization");

        // L2 范数应该接近 1
        double sumSquares = embedding.Sum(v => (double)v * v);
        var norm = Math.Sqrt(sumSquares);

        Assert.True(norm is > 0.99 and < 1.01, $"Norm was {norm}");
    }

    [Fact]
    public async Task EmbedAsync_SimilarTextsShouldHaveSimilarEmbeddings()
    {
        var service = new SimpleEmbeddingService(128);

        var embedding1 = await service.EmbedAsync("The quick brown fox jumps");
        var embedding2 = await service.EmbedAsync("The quick brown fox leaps");

        var similarity = CosineSimilarity(embedding1, embedding2);

        // 相似文本应该有较高相似度 (>0.8)
        Assert.True(similarity > 0.8, $"Similarity was {similarity}");
    }

    [Fact]
    public async Task EmbedAsync_DifferentTextsShouldHaveDifferentEmbeddings()
    {
        var service = new SimpleEmbeddingService(128);

        var embedding1 = await service.EmbedAsync("Hello world");
        var embedding2 = await service.EmbedAsync("Database query optimization");

        var similarity = CosineSimilarity(embedding1, embedding2);

        // 不同文本的相似度应该较低
        Assert.True(similarity < 0.95, $"Similarity was {similarity}");
    }

    [Fact]
    public async Task EmbedAsync_ShouldCacheResults()
    {
        var service = new SimpleEmbeddingService(128);

        var embedding1 = await service.EmbedAsync("Cache test");
        var embedding2 = await service.EmbedAsync("Cache test");

        // 应该返回相同的引用或相等的值
        Assert.Equal(embedding1, embedding2);
    }

    [Fact]
    public async Task EmbedBatchAsync_ShouldReturnMultipleEmbeddings()
    {
        var service = new SimpleEmbeddingService(128);
        var texts = new[] { "Text one", "Text two", "Text three" };

        var embeddings = await service.EmbedBatchAsync(texts);

        Assert.Equal(3, embeddings.Count);
        Assert.All(embeddings, e => Assert.Equal(128, e.Length));
    }

    [Fact]
    public async Task EmbedAsync_ShouldHandleChineseText()
    {
        var service = new SimpleEmbeddingService(128);

        var embedding = await service.EmbedAsync("这是一个中文测试");

        Assert.Equal(128, embedding.Length);
        Assert.Contains(embedding, value => value != 0);
    }

    [Fact]
    public async Task EmbedAsync_ShouldDetectCodeFeatures()
    {
        var service = new SimpleEmbeddingService(128);

        var codeEmbedding = await service.EmbedAsync("function test() { return true; }");
        var textEmbedding = await service.EmbedAsync("This is just regular text.");

        // 代码应该有不同的特征
        Assert.NotEqual(codeEmbedding, textEmbedding);
    }

    [Fact]
    public async Task EmbedAsync_ShouldDetectQuestionFeatures()
    {
        var service = new SimpleEmbeddingService(128);

        var questionEmbedding = await service.EmbedAsync("What is this?");
        var statementEmbedding = await service.EmbedAsync("This is a statement.");

        // 问句应该有不同的特征
        Assert.NotEqual(questionEmbedding, statementEmbedding);
    }

    [Fact]
    public async Task ClearCache_ShouldClearCachedEmbeddings()
    {
        var service = new SimpleEmbeddingService(128);

        await service.EmbedAsync("Test");
        service.ClearCache();

        // 清除后再次获取应该是新计算
        // 这个测试主要是确保方法不会抛出异常
        await service.EmbedAsync("Test");
    }

    private static double CosineSimilarity(float[] v1, float[] v2)
    {
        double dotProduct = 0;
        double magnitude1 = 0;
        double magnitude2 = 0;

        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
            magnitude1 += v1[i] * v1[i];
            magnitude2 += v2[i] * v2[i];
        }

        magnitude1 = Math.Sqrt(magnitude1);
        magnitude2 = Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }
}
