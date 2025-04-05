using XerifeTv.CMS.Modules.User.Enums;

namespace XerifeTv.CMS.Modules.User.Dtos.Request;

public record UpdateUserRequestDto(
	string Id, 
	string UserName, 
	string Email, 
	EUserRole? Role);