using Amazon.Runtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Controllers;

[Route("MediaProxy")]
[ApiController]
public class MediaProxyController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : ControllerBase
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient();

    [HttpGet]
    [Route("mp4")]
    public async Task<IActionResult> ProxyMp4([FromQuery] string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("URL não informada.");

        if (!TryDecryptUrl(url, out var decryptedUrl))
            return BadRequest("URL inválida.");

        if (!Uri.TryCreate(decryptedUrl, UriKind.Absolute, out var uri))
            return BadRequest("URL inválida.");

        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; XerifeTvProxy/1.0)");

        if (Request.Headers.TryGetValue("Range", out var range))
            request.Headers.TryAddWithoutValidation("Range", range.ToString());

        var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        HttpContext.Response.OnCompleted(() =>
        {
            response.Dispose();
            return Task.CompletedTask;
        });

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, $"Erro no servidor upstream: {response.StatusCode}");

        Response.StatusCode = (int)response.StatusCode;

        if (response.Content.Headers.ContentRange is not null)
            Response.Headers["Content-Range"] = response.Content.Headers.ContentRange.ToString();

        if (response.Content.Headers.ContentLength is not null)
            Response.ContentLength = response.Content.Headers.ContentLength;

        Response.Headers["Accept-Ranges"] = "bytes";

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "video/mp4";

        return File(stream, contentType);
    }

    [HttpGet]
    [Route("hls")]
    public async Task<IActionResult> ProxyHls([FromQuery] string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("URL não informada.");

        if (!TryDecryptUrl(url, out var decryptedUrl))
            return BadRequest("URL inválida.");

        if (!Uri.TryCreate(decryptedUrl, UriKind.Absolute, out var uri))
            return BadRequest("URL inválida.");

        using var response = await httpClient.GetAsync(uri, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var rewrittenPlaylist = RewritePlaylist(content, uri);

        return Content(rewrittenPlaylist, "application/vnd.apple.mpegurl");
    }

    private bool TryDecryptUrl(string encryptedUrl, out string decryptedUrl)
    {
        try
        {
            decryptedUrl = CryptographyHelper.Decrypt(encryptedUrl, configuration["SecuritySettings:ContentEncryptionKey"]!);
            return !string.IsNullOrWhiteSpace(decryptedUrl);
        }
        catch
        {
            decryptedUrl = string.Empty;
            return false;
        }
    }

    private static readonly Regex UriAttributeRegex = new("URI\\s*=\\s*\"([^\"]+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private string RewritePlaylist(string playlist, Uri baseUri)
    {
        var lines = playlist.Split('\n', StringSplitOptions.None);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            {
                if (UriAttributeRegex.IsMatch(line))
                {
                    lines[i] = UriAttributeRegex.Replace(line, match =>
                    {
                        if (!Uri.TryCreate(baseUri, match.Groups[1].Value, out var resourceUri))
                            return match.Value;

                        return $"URI=\"{BuildProxyUrl(resourceUri)}\"";
                    });
                }

                continue;
            }

            if (!Uri.TryCreate(baseUri, line, out var lineUri))
                continue;

            lines[i] = BuildProxyUrl(lineUri);
        }

        return string.Join('\n', lines);
    }

    private string BuildProxyUrl(Uri uri)
    {
        var encryptedUrl = CryptographyHelper.Encrypt(
            uri.ToString(),
            configuration["SecuritySettings:ContentEncryptionKey"]!);

        var encodedUrl = Uri.EscapeDataString(encryptedUrl);

        var isPlaylist = uri.AbsolutePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);
        var route = isPlaylist ? "hls" : "mp4";

        return $"{Request.Scheme}://{Request.Host}/MediaProxy/{route}?url={encodedUrl}";
    }
}