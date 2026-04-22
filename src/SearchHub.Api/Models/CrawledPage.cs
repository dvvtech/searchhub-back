using ProtoBuf;

namespace SearchHub.Api.Models;

[ProtoContract]
public class CrawledPage
{
    [ProtoMember(1)]
    public string Url { get; init; } = string.Empty;

    [ProtoMember(2)]
    public string Title { get; init; } = string.Empty;

    [ProtoMember(3)]
    public string Content { get; init; } = string.Empty;

    [ProtoMember(4)]
    public int SiteId { get; init; }
}
