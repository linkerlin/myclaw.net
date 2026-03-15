using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;

namespace MyClaw.Core.VectorMemory;

/// <summary>
/// 持久化向量存储 - 支持自动保存和增量更新
/// </summary>
public class PersistentVectorStore : IVectorStore, IDisposable
{
    private readonly ConcurrentDictionary<string, VectorMemoryEntry> _entries = new();
    private readonly ConcurrentDictionary<string, bool> _dirtyEntries = new();
    private readonly int _dimension;
    private readonly string _storagePath;
    private readonly Timer? _autoSaveTimer;
    private readonly TimeSpan _autoSaveInterval;
    private readonly bool _compress;
    private readonly object _saveLock = new();
    private long _totalAccessCount = 0;
    private DateTime _lastSaveTime = DateTime.MinValue;

    public int Dimension => _dimension;
    public int Count => _entries.Count;

    /// <summary>
    /// 存储统计信息
    /// </summary>
    public PersistentStoreStats Stats
    {
        get
        {
            var dirtyCount = _dirtyEntries.Count;
            return new PersistentStoreStats
            {
                TotalEntries = _entries.Count,
                DirtyEntries = dirtyCount,
                PendingSaves = dirtyCount,
                LastSaveTime = _lastSaveTime,
                TotalAccessCount = _totalAccessCount,
                StoragePath = _storagePath,
                AutoSaveEnabled = _autoSaveTimer != null,
                StorageSizeBytes = GetStorageSize()
            };
        }
    }

    /// <summary>
    /// 创建持久化向量存储
    /// </summary>
    /// <param name="storagePath">存储文件路径</param>
    /// <param name="dimension">向量维度</param>
    /// <param name="autoSaveInterval">自动保存间隔（null则禁用自动保存）</param>
    /// <param name="compress">是否启用压缩</param>
    public PersistentVectorStore(
        string storagePath,
        int dimension = 384,
        TimeSpan? autoSaveInterval = null,
        bool compress = true)
    {
        _storagePath = storagePath;
        _dimension = dimension;
        _compress = compress;
        _autoSaveInterval = autoSaveInterval ?? TimeSpan.FromMinutes(5);

        // 创建存储目录
        var directory = Path.GetDirectoryName(storagePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 启动自动保存定时器
        if (autoSaveInterval.HasValue)
        {
            _autoSaveTimer = new Timer(
                _ => _ = AutoSaveAsync(),
                null,
                autoSaveInterval.Value,
                autoSaveInterval.Value);
        }
    }

    #region IVectorStore Implementation

    public Task<string> UpsertAsync(VectorMemoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrEmpty(entry.Id))
        {
            entry.Id = Guid.NewGuid().ToString();
        }

        entry.LastAccessedAt = DateTime.UtcNow;

        // 检查是否为新条目或已变更
        var isNewOrChanged = true;
        if (_entries.TryGetValue(entry.Id, out var existing))
        {
            // 简单比较：如果嵌入向量相同则认为未变更
            isNewOrChanged = !VectorsEqual(existing.Embedding, entry.Embedding) ||
                           existing.Content != entry.Content;
        }

        _entries[entry.Id] = entry;

        if (isNewOrChanged)
        {
            _dirtyEntries[entry.Id] = true;
        }

        return Task.FromResult(entry.Id);
    }

    public Task<int> UpsertBatchAsync(IEnumerable<VectorMemoryEntry> entries)
    {
        var count = 0;
        foreach (var entry in entries)
        {
            _ = UpsertAsync(entry);
            count++;
        }
        return Task.FromResult(count);
    }

    public Task<VectorMemoryEntry?> GetAsync(string id)
    {
        if (_entries.TryGetValue(id, out var entry))
        {
            entry.LastAccessedAt = DateTime.UtcNow;
            entry.AccessCount++;
            Interlocked.Increment(ref _totalAccessCount);
            return Task.FromResult<VectorMemoryEntry?>(entry);
        }
        return Task.FromResult<VectorMemoryEntry?>(null);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _entries.TryRemove(id, out _);
        if (removed)
        {
            _dirtyEntries[id] = true; // 标记为脏，保存时会处理删除
        }
        return Task.FromResult(removed);
    }

    public Task ClearAsync()
    {
        // 标记所有条目为脏（待删除）
        foreach (var id in _entries.Keys)
        {
            _dirtyEntries[id] = true;
        }
        _entries.Clear();
        return Task.CompletedTask;
    }

    public Task<List<VectorSearchResult>> SearchAsync(VectorSearchRequest request)
    {
        var results = new List<VectorSearchResult>();

        if (request.QueryVector == null || request.QueryVector.Length == 0)
        {
            return Task.FromResult(results);
        }

        foreach (var entry in _entries.Values)
        {
            // 过滤来源类型
            if (request.SourceTypes?.Count > 0 &&
                !request.SourceTypes.Contains(entry.SourceType))
            {
                continue;
            }

            // 过滤元数据
            if (request.MetadataFilter?.Count > 0)
            {
                var match = true;
                foreach (var filter in request.MetadataFilter)
                {
                    if (!entry.Metadata.TryGetValue(filter.Key, out var value) ||
                        value != filter.Value)
                    {
                        match = false;
                        break;
                    }
                }
                if (!match) continue;
            }

            // 计算相似度
            var score = CosineSimilarity(request.QueryVector, entry.Embedding);

            if (score >= request.MinScore)
            {
                results.Add(new VectorSearchResult
                {
                    Entry = entry,
                    Score = score
                });
            }
        }

        // 排序并取 TopK
        var sortedResults = results
            .OrderByDescending(r => r.Score)
            .Take(request.TopK)
            .ToList();

        return Task.FromResult(sortedResults);
    }

    public async Task SaveAsync(string path)
    {
        var savePath = string.IsNullOrEmpty(path) ? _storagePath : path;
        var tempPath = savePath + ".tmp";
        var backupPath = savePath + ".backup";

        lock (_saveLock)
        {
            try
            {
                // 序列化数据
                var data = new VectorStoreData
                {
                    Version = 1,
                    Dimension = _dimension,
                    SavedAt = DateTime.UtcNow,
                    Entries = _entries.Values.ToList()
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = false // 压缩格式
                };

                var json = JsonSerializer.Serialize(data, options);

                // 写入临时文件
                if (_compress)
                {
                    WriteCompressed(tempPath, json);
                }
                else
                {
                    File.WriteAllText(tempPath, json);
                }

                // 备份现有文件
                if (File.Exists(savePath))
                {
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                    File.Move(savePath, backupPath);
                }

                // 替换为临时文件
                File.Move(tempPath, savePath);

                // 清理备份（如果成功）
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                // 清除脏标记
                _dirtyEntries.Clear();
                _lastSaveTime = DateTime.UtcNow;

                // Log to stderr to avoid stdout pollution for MCP stdio mode
                Console.Error.WriteLine($"[vector-memory] Saved {data.Entries.Count} entries to {Path.GetFileName(savePath)} ({GetStorageSize() / 1024} KB)");
            }
            catch (Exception ex)
            {
                // 恢复备份
                if (File.Exists(backupPath) && !File.Exists(savePath))
                {
                    File.Move(backupPath, savePath);
                }
                throw new InvalidOperationException($"保存向量存储失败: {ex.Message}", ex);
            }
            finally
            {
                // 清理临时文件
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        await Task.CompletedTask;
    }

    public async Task LoadAsync(string path)
    {
        var loadPath = string.IsNullOrEmpty(path) ? _storagePath : path;

        if (!File.Exists(loadPath))
        {
            // File doesn't exist, this is normal for first run - use stderr
            Console.Error.WriteLine($"[vector-memory] Storage file not found: {loadPath}");
            return;
        }

        try
        {
            string json;
            var isCompressed = IsCompressedFile(loadPath);

            // 优先根据文件实际格式读取，而非配置
            if (isCompressed)
            {
                json = ReadCompressed(loadPath);
            }
            else
            {
                json = await File.ReadAllTextAsync(loadPath);
            }

            var data = JsonSerializer.Deserialize<VectorStoreData>(json);

            if (data == null)
            {
                throw new InvalidDataException("无法解析存储文件");
            }

            _entries.Clear();
            _dirtyEntries.Clear();

            foreach (var entry in data.Entries)
            {
                if (!string.IsNullOrEmpty(entry.Id))
                {
                    _entries[entry.Id] = entry;
                }
            }

            _lastSaveTime = DateTime.UtcNow;

            Console.Error.WriteLine($"[vector-memory] Loaded {data.Entries.Count} entries (dimension: {data.Dimension})");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[vector-memory] Load failed: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 强制立即保存
    /// </summary>
    public Task ForceSaveAsync() => SaveAsync(_storagePath);

    /// <summary>
    /// 获取所有条目（用于调试）
    /// </summary>
    public IEnumerable<VectorMemoryEntry> GetAllEntries()
    {
        return _entries.Values.ToList();
    }

    /// <summary>
    /// 压缩存储文件
    /// </summary>
    public void Compact()
    {
        // 强制完整保存以压缩和整理
        SaveAsync(_storagePath).Wait();
    }

    /// <summary>
    /// 获取存储文件大小（字节）
    /// </summary>
    public long GetStorageSize()
    {
        if (File.Exists(_storagePath))
        {
            return new FileInfo(_storagePath).Length;
        }
        return 0;
    }

    #endregion

    #region Private Methods

    private async Task AutoSaveAsync()
    {
        if (_dirtyEntries.Count == 0) return;

        try
        {
            await SaveAsync(_storagePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[vector-memory] Auto-save failed: {ex.Message}");
        }
    }

    private static bool VectorsEqual(float[]? a, float[]? b)
    {
        if (a == null || b == null) return a == b;
        if (a.Length != b.Length) return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (Math.Abs(a[i] - b[i]) > 1e-6) return false;
        }
        return true;
    }

    private static double CosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length || vector1.Length == 0)
            return 0;

        double dotProduct = 0;
        double magnitude1 = 0;
        double magnitude2 = 0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = Math.Sqrt(magnitude1);
        magnitude2 = Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }

    private static void WriteCompressed(string path, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        using var fs = File.Create(path);
        using var gzip = new GZipStream(fs, CompressionLevel.Optimal);
        gzip.Write(bytes, 0, bytes.Length);
    }

    private static string ReadCompressed(string path)
    {
        using var fs = File.OpenRead(path);
        using var gzip = new GZipStream(fs, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        return reader.ReadToEnd();
    }

    private static bool IsCompressedFile(string path)
    {
        // 检查文件魔数
        using var fs = File.OpenRead(path);
        if (fs.Length < 2) return false;

        Span<byte> magic = stackalloc byte[2];
        fs.ReadExactly(magic);
        // GZip 魔数: 0x1f 0x8b
        return magic[0] == 0x1f && magic[1] == 0x8b;
    }

    #endregion

    public void Dispose()
    {
        _autoSaveTimer?.Dispose();

        // 尝试最后一次保存
        if (_dirtyEntries.Count > 0)
        {
            try
            {
                SaveAsync(_storagePath).Wait(TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[vector-memory] Final save failed: {ex.Message}");
            }
        }
    }
}

/// <summary>
/// 向量存储数据格式
/// </summary>
public class VectorStoreData
{
    public int Version { get; set; }
    public int Dimension { get; set; }
    public DateTime SavedAt { get; set; }
    public List<VectorMemoryEntry> Entries { get; set; } = new();
}

/// <summary>
/// 持久化存储统计
/// </summary>
public class PersistentStoreStats
{
    public int TotalEntries { get; set; }
    public int DirtyEntries { get; set; }
    public int PendingSaves { get; set; }
    public DateTime LastSaveTime { get; set; }
    public long TotalAccessCount { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public bool AutoSaveEnabled { get; set; }
    public long StorageSizeBytes { get; set; }

    public override string ToString()
    {
        var sizeMb = StorageSizeBytes / (1024.0 * 1024.0);
        return $"Entries: {TotalEntries} | Dirty: {DirtyEntries} | Size: {sizeMb:F2} MB | AutoSave: {(AutoSaveEnabled ? "On" : "Off")}";
    }
}
