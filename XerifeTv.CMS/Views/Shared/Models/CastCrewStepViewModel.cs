using XerifeTv.CMS.Modules.People.Dtos.Response;

namespace XerifeTv.CMS.Views.Shared.Models;

public sealed record CastCrewStepViewModel(
    string Controller,
    IReadOnlyCollection<GetCreditResponseDto> Credits);
