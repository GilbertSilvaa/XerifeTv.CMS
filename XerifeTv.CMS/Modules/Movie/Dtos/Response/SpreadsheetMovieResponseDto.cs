﻿using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Movie.Dtos.Response;

public sealed class SpreadsheetMovieResponseDto
{
    public string ImdbId { get; private set; } = string.Empty;
    public int ParentalRating { get; private set; }
    public Video? Video { get; private set; }

    public static SpreadsheetMovieResponseDto FromCollunsStr(string[] cols)
    {
        string? imdbId = cols[0];
        string? parentalRating = cols[1];
        string? videoUrl = cols[2];
        string? videoStreamFormat = cols[3];
        string? videoSubtitleUrl = cols[4];

        List<string?> requiredValues = [
            imdbId,
            videoUrl,
            parentalRating,
            videoStreamFormat
        ];

        if (requiredValues.Any(v => string.IsNullOrEmpty(v)))
            throw new SpreadsheetInvalidException($"[{imdbId}] algum campo obrigatorio esta vazio");

        if (!int.TryParse(parentalRating, out var parentalRatingResult))
            throw new SpreadsheetInvalidException($"[{imdbId}] classificacao indicativa em formato invalido");

        if (!ParentalRatingHelper.ParentalRatingList.Contains(parentalRatingResult))
            throw new SpreadsheetInvalidException($"[{imdbId}] classificacao indicativa invalida");

        if (!StreamFormatsHelper.Streaming.Contains(videoStreamFormat)
            && !StreamFormatsHelper.Vod.Contains(videoStreamFormat))
            throw new SpreadsheetInvalidException($"[{imdbId}] stream format invalido");

        return new SpreadsheetMovieResponseDto
        {
            ImdbId = imdbId,
            ParentalRating = parentalRatingResult,
            Video = new Video(videoUrl, 0, videoStreamFormat, videoSubtitleUrl)
        };
    }
}
