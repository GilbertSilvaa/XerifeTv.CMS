using XerifeTv.CMS.Modules.People.Dtos.Response;

namespace XerifeTv.CMS.Shared.Views.Models;

public sealed record CastCrewStepViewModel(
    string Controller,
    IReadOnlyCollection<GetCreditResponseDto> Credits);
