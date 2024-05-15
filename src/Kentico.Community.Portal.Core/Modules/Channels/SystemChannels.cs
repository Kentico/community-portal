using System.Collections.Immutable;

namespace Kentico.Community.Portal.Core.Modules;

public static class SystemChannels
{
    public static WebsiteChannel Website { get; } = new();
    public static EmailsChannel Emails { get; } = new();

    public static readonly ImmutableList<ISystemChannel> ProtectedChannels =
    [
        Website,
        Emails
    ];

    public static bool Includes(ChannelInfo channel) =>
        ProtectedChannels.Select(c => c.ChannelGUID).Contains(channel.ChannelGUID);

    public record WebsiteChannel : ISystemChannel
    {
        public static Guid GUID { get; } = new Guid("4f636110-fd4a-4905-83e6-998752c2b2c2");
        public const string CodeName = "devnet";

        public Guid ChannelGUID => GUID;

        public string ChannelName => CodeName;
    }

    public record EmailsChannel : ISystemChannel
    {
        public static Guid GUID { get; } = new Guid("1b41b848-ddd2-4a8d-9bc7-850a93e147c4");
        public const string CodeName = "KenticoCommunityEmails";

        public Guid ChannelGUID => GUID;

        public string ChannelName => CodeName;
    }
}

public interface ISystemChannel
{
    Guid ChannelGUID { get; }
    string ChannelName { get; }
}
