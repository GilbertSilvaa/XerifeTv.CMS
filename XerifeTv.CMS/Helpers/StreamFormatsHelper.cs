namespace XerifeTv.CMS.Helpers;

public class StreamFormatsHelper
{
  public static string[] Streaming => ["hls", "mpeg-dash", "rtsp"];
  public static string[] Vod => ["mp4", "hls", "webm", "mkv", "mov"];
}