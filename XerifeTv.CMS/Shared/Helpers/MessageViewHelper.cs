
using System.Text.Json;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Shared.Helpers;

public class MessageViewHelper
{
	public static string ErrorJson(string message)
		=> JsonSerializer.Serialize(new MessageView(EMessageViewType.ERROR, message));
	
	public static string AlertJson(string message)
		=> JsonSerializer.Serialize(new MessageView(EMessageViewType.ALERT, message));
	
	public static string SuccessJson(string message)
		=> JsonSerializer.Serialize(new MessageView(EMessageViewType.SUCCESS, message));
	
	public static MessageView JsonStrToMessageView(string jsonStr)
		=> JsonSerializer.Deserialize<MessageView>(jsonStr);
}