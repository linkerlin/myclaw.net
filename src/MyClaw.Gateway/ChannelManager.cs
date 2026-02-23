using MyClaw.Channels;
using MyClaw.Channels.Uno;
using MyClaw.Channels.WebUI;
using MyClaw.Core.Configuration;
using MyClaw.Core.Messaging;

namespace MyClaw.Gateway;

#pragma warning disable CS0618 // MessageBus 兼容性别名

/// <summary>
/// 渠道管理器 - 管理所有消息渠道
/// </summary>
public class ChannelManager
{
    private readonly Dictionary<string, IChannel> _channels = new();
    private readonly MessageBus _messageBus;

    public ChannelManager(MessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    /// <summary>
    /// 注册渠道
    /// </summary>
    public void RegisterChannel(IChannel channel)
    {
        _channels[channel.Name] = channel;
        
        // 订阅出站消息
        _messageBus.SubscribeOutbound(channel.Name, async msg =>
        {
            try
            {
                await channel.SendAsync(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[channel-mgr] 发送到 {channel.Name} 失败: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 初始化渠道（根据配置）
    /// </summary>
    public void InitializeChannels(ChannelsConfig config)
    {
        // WebUI 渠道
        if (config.WebUI.Enabled)
        {
            RegisterChannel(new WebUIChannel(config.WebUI, _messageBus));
        }

        // Uno Platform 渠道
        if (config.Uno.Enabled)
        {
            RegisterChannel(new UnoPlatformChannel(config.Uno, _messageBus));
        }

        // TODO: 其他渠道 (Telegram, Feishu, WeCom, WhatsApp)
    }

    /// <summary>
    /// 启动所有渠道
    /// </summary>
    public async Task StartAllAsync(CancellationToken ct = default)
    {
        var tasks = _channels.Values
            .Where(c => c.IsEnabled)
            .Select(c => StartChannelAsync(c, ct));
        
        await Task.WhenAll(tasks);
    }

    private async Task StartChannelAsync(IChannel channel, CancellationToken ct)
    {
        Console.WriteLine($"[channel-mgr] 正在启动 {channel.Name}");
        try
        {
            await channel.StartAsync(ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[channel-mgr] 启动 {channel.Name} 失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 停止所有渠道
    /// </summary>
    public async Task StopAllAsync()
    {
        foreach (var channel in _channels.Values)
        {
            Console.WriteLine($"[channel-mgr] 正在停止 {channel.Name}");
            try
            {
                await channel.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[channel-mgr] 停止 {channel.Name} 错误: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 获取已启用的渠道列表
    /// </summary>
    public List<string> GetEnabledChannels()
    {
        return _channels.Values
            .Where(c => c.IsEnabled)
            .Select(c => c.Name)
            .ToList();
    }

    /// <summary>
    /// 获取指定渠道
    /// </summary>
    public IChannel? GetChannel(string name)
    {
        return _channels.TryGetValue(name, out var channel) ? channel : null;
    }
}

#pragma warning restore CS0618
