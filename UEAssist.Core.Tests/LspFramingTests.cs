using System.Text;
using Newtonsoft.Json.Linq;
using UEAssist.Semantics;
namespace UEAssist.Core.Tests;
public class LspFramingTests
{
    private sealed class ChunkedStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) => base.ReadAsync(buffer, offset, Math.Min(3, count), token);
    }
    [Fact]
    public async Task Utf8ByteLengthsAndFragmentedConsecutiveFramesRoundtrip()
    {
        var first = new JObject { ["id"] = 1, ["result"] = "한글 😀\n\"" };
        var second = new JObject { ["id"] = 2, ["result"] = new JArray("value") };
        using var stream = new ChunkedStream(LspFraming.Encode(first).Concat(LspFraming.Encode(second)).ToArray());
        Assert.True(JToken.DeepEquals(first, await LspFraming.ReadAsync(stream, CancellationToken.None)));
        Assert.True(JToken.DeepEquals(second, await LspFraming.ReadAsync(stream, CancellationToken.None)));
        Assert.Null(await LspFraming.ReadAsync(stream, CancellationToken.None));
    }
    [Theory]
    [InlineData("Content-Length: 5\r\n\r\n{}")]
    [InlineData("Content-Length: 5\r\n")]
    public async Task RejectsTruncatedFrames(string frame)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(frame));
        await Assert.ThrowsAsync<EndOfStreamException>(() => LspFraming.ReadAsync(stream, CancellationToken.None));
    }
    [Theory]
    [InlineData("nope")]
    [InlineData("-1")]
    [InlineData("999999999")]
    public async Task RejectsInvalidLengths(string length)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("Content-Length: " + length + "\r\n\r\n"));
        await Assert.ThrowsAsync<InvalidDataException>(() => LspFraming.ReadAsync(stream, CancellationToken.None));
    }
}
