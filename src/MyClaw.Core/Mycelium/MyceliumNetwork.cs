namespace MyClaw.Core.Mycelium;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// 菌丝共生网络 - 跨实例知识共享
/// 
/// 设计哲学：
/// - 同物理机上多个实例共享 ~/{project}/mycelium/ 目录
/// - 当一个实例踩坑（记录 NOCICEPTION）时，自动分泌孢子
/// - 其他实例在心跳时吸收孢子，实现群体免疫
/// 
/// 生物隐喻：
/// - 菌丝网络 (Mycelium): 真菌的地下网络，用于营养和信息传递
/// - 孢子 (Spore): 繁殖单位，携带遗传信息
/// - 群体免疫 (Herd Immunity): 一个个体获得的免疫传递给群体
/// </summary>
public class MyceliumNetwork
{
    private readonly string _myceliumDir;
    private readonly string _instanceId;
    private readonly ILogger<MyceliumNetwork>? _logger;

    /// <summary>
    /// 支持的孢子类型
    /// </summary>
    public static readonly HashSet<string> SporeTypes = new()
    {
        "NOCICEPTION",  // 痛觉记忆
        "TOOLS",        // 技能池抗体
    };

    public MyceliumNetwork(string myclawDir, string? instanceId = null, ILogger<MyceliumNetwork>? logger = null)
    {
        _myceliumDir = Path.Combine(myclawDir, "mycelium");
        _instanceId = instanceId ?? Environment.MachineName + "_" + Environment.UserName + "_" + Process.GetCurrentProcess().Id;
        _logger = logger;
        
        Directory.CreateDirectory(_myceliumDir);
    }

    /// <summary>
    /// 分泌孢子 - 当 TOOLS.md 或 NOCICEPTION.md 更新时
    /// </summary>
    /// <param name="type">孢子类型</param>
    /// <param name="content">孢子内容</param>
    /// <returns>孢子文件路径</returns>
    public async Task<string> SecreteSporeAsync(string type, string content)
    {
        if (!SporeTypes.Contains(type))
            throw new ArgumentException($"Invalid spore type: {type}. Must be one of: {string.Join(", ", SporeTypes)}");

        var hash = HashString(DateTime.UtcNow.ToString("o") + content).Substring(0, 8);
        var sporePath = Path.Combine(_myceliumDir, $"{_instanceId}_{hash}.json");

        var spore = new Spore
        {
            SenderId = _instanceId,
            Type = type,
            Content = content,
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        var json = JsonSerializer.Serialize(spore, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(sporePath, json);

        _logger?.LogInformation("Secrete spore {Type} to {Path}", type, sporePath);
        return sporePath;
    }

    /// <summary>
    /// 吸收孢子 - 在心跳时调用
    /// </summary>
    /// <param name="myclawDir">目标 myclaw 目录</param>
    /// <returns>吸收结果</returns>
    public async Task<AbsorptionResult> AbsorbSporesAsync(string myclawDir)
    {
        var sporeFiles = Directory.GetFiles(_myceliumDir, "*.json")
            .Where(f => !f.EndsWith(".consumed", StringComparison.OrdinalIgnoreCase));

        var absorbed = new List<Spore>();
        var nociceptionCount = 0;
        var toolsCount = 0;

        foreach (var sporePath in sporeFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(sporePath);
                var spore = JsonSerializer.Deserialize<Spore>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (spore == null)
                {
                    _logger?.LogWarning("Failed to deserialize spore: {Path}", sporePath);
                    continue;
                }

                // 跳过自己分泌的孢子
                if (spore.SenderId == _instanceId)
                    continue;

                // 吸收异体知识
                if (spore.Type == "NOCICEPTION")
                {
                    await AppendToNociceptionAsync(myclawDir, spore);
                    nociceptionCount++;
                }
                else if (spore.Type == "TOOLS")
                {
                    await AppendToToolsAsync(myclawDir, spore);
                    toolsCount++;
                }

                // 消耗孢子（标记为已消费）
                var consumedPath = sporePath + ".consumed";
                File.Move(sporePath, consumedPath, overwrite: true);
                absorbed.Add(spore);

                _logger?.LogInformation("Absorbed spore from {SenderId}: {Type}", spore.SenderId, spore.Type);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to absorb spore: {Path}", sporePath);
            }
        }

        return new AbsorptionResult
        {
            AbsorbedSpores = absorbed,
            NociceptionCount = nociceptionCount,
            ToolsCount = toolsCount
        };
    }

    /// <summary>
    /// 添加痛觉记忆到 NOCICEPTION.md
    /// </summary>
    private async Task AppendToNociceptionAsync(string myclawDir, Spore spore)
    {
        var path = Path.Combine(myclawDir, "NOCICEPTION.md");
        var content = $"\n> 🍄 [菌丝共生] 接收到异体意识 ({spore.SenderId}) 传来的疼痛记忆:\n{spore.Content}";
        await File.AppendAllTextAsync(path, content);
    }

    /// <summary>
    /// 添加技能到 TOOLS.md
    /// </summary>
    private async Task AppendToToolsAsync(string myclawDir, Spore spore)
    {
        var path = Path.Combine(myclawDir, "TOOLS.md");
        var content = $"\n> 🍄 [菌丝共生] 吸收了异体意识 ({spore.SenderId}) 进化出的技能池抗体:\n{spore.Content}";
        await File.AppendAllTextAsync(path, content);
    }

    /// <summary>
    /// SHA256 哈希
    /// </summary>
    private static string HashString(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// 获取菌丝网络统计信息
    /// </summary>
    public async Task<MyceliumStats> GetStatsAsync()
    {
        var sporeFiles = Directory.GetFiles(_myceliumDir, "*.json")
            .Where(f => !f.EndsWith(".consumed", StringComparison.OrdinalIgnoreCase));

        var consumedFiles = Directory.GetFiles(_myceliumDir, "*.consumed");

        var selfSpores = 0;
        var foreignSpores = 0;

        foreach (var file in sporeFiles.Concat(consumedFiles))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var spore = JsonSerializer.Deserialize<Spore>(json);
                if (spore?.SenderId == _instanceId)
                    selfSpores++;
                else
                    foreignSpores++;
            }
            catch
            {
                // Ignore parse errors
            }
        }

        return new MyceliumStats
        {
            TotalSpores = sporeFiles.Count() + consumedFiles.Count(),
            AvailableSpores = sporeFiles.Count(),
            ConsumedSpores = consumedFiles.Count(),
            SelfSpores = selfSpores,
            ForeignSpores = foreignSpores
        };
    }
}

/// <summary>
/// 孢子 - 知识共享单位
/// </summary>
public class Spore
{
    /// <summary>
    /// 发送者实例 ID
    /// </summary>
    public string SenderId { get; init; } = "";

    /// <summary>
    /// 孢子类型 (NOCICEPTION / TOOLS)
    /// </summary>
    public string Type { get; init; } = "";

    /// <summary>
    /// 孢子内容
    /// </summary>
    public string Content { get; init; } = "";

    /// <summary>
    /// 分泌时间戳
    /// </summary>
    public string Timestamp { get; init; } = "";
}

/// <summary>
/// 吸收结果
/// </summary>
public class AbsorptionResult
{
    /// <summary>
    /// 吸收的孢子列表
    /// </summary>
    public List<Spore> AbsorbedSpores { get; init; } = new();

    /// <summary>
    /// 吸收的痛觉记忆数量
    /// </summary>
    public int NociceptionCount { get; init; }

    /// <summary>
    /// 吸收的技能数量
    /// </summary>
    public int ToolsCount { get; init; }

    /// <summary>
    /// 是否吸收了任何孢子
    /// </summary>
    public bool HasAbsorbed => AbsorbedSpores.Any();
}

/// <summary>
/// 菌丝网络统计
/// </summary>
public class MyceliumStats
{
    /// <summary>
    /// 总孢子数
    /// </summary>
    public int TotalSpores { get; init; }

    /// <summary>
    /// 可用孢子数
    /// </summary>
    public int AvailableSpores { get; init; }

    /// <summary>
    /// 已消费孢子数
    /// </summary>
    public int ConsumedSpores { get; init; }

    /// <summary>
    /// 自身分泌的孢子数
    /// </summary>
    public int SelfSpores { get; init; }

    /// <summary>
    /// 异体孢子数
    /// </summary>
    public int ForeignSpores { get; init; }
}
