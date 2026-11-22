namespace XerifeTv.CMS.Modules.Common;

public enum EMessageViewType
{
    SUCCESS,
    ALERT,
    ERROR
}

public record MessageView(EMessageViewType Type, string Message);
