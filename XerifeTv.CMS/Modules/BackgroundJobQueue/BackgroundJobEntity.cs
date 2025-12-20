using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue;

public class BackgroundJobEntity : BaseEntity
{
	public string JobName { get; private set; } = string.Empty;
	public EBackgroundJobType Type { get; private set; }
	public EBackgroundJobStatus Status { get; private set; }
	public string RequestedByUserId { get; private set; } = string.Empty;
	public int TotalRecordsToProcess { get; private set; }
	public int TotalFailedRecords { get; private set; }
	public int TotalSuccessfulRecords { get; private set; }
	public int TotalProcessedRecords { get; private set; }
	public DateTime? ProcessedAt { get; private set; }
	public DateTime? FinishedAt { get; private set; }
	public ICollection<string> ErrorList { get; private set; } = [];
	public string? SpreadsheetFileUrl { get; private set; } = null;
	public string? SeriesIdImportEpisodes { get; private set; } = null;
	public bool UserWasNotified { get; private set; } = false;

	public static BackgroundJobEntity Create(
		string id,
		EBackgroundJobType type,
		string spreadsheetFileName,
		string spreadsheetFileUrl,
		string userId)
	{
		return new BackgroundJobEntity
		{
			Id = id,
			Type = type,
			JobName = type switch
			{
				EBackgroundJobType.REGISTER_SPREADSHEET_MOVIES => $"Cadastro/Atualizacao de Filmes ({spreadsheetFileName})",
				EBackgroundJobType.REGISTER_SPREADSHEET_SERIES => $"Cadastro/Atualizacao de Series ({spreadsheetFileName})",
				EBackgroundJobType.REGISTER_SPREADSHEET_CHANNELS => $"Cadastro de Canais ({spreadsheetFileName})",
				_ => string.Empty
			},
			Status = EBackgroundJobStatus.PENDING,
			RequestedByUserId = userId,
			SpreadsheetFileUrl = spreadsheetFileUrl
		};
	}

	public static BackgroundJobEntity Create(string seriesId, string seriesImdbId, string userId)
	{
		return new BackgroundJobEntity
		{
			Type = EBackgroundJobType.IMPORT_EPISODES_FROM_SERIES_IMDB,
			JobName = $"Importacao de Episodios via IMDB [{seriesImdbId}]",
			Status = EBackgroundJobStatus.PENDING,
			RequestedByUserId = userId,
			SeriesIdImportEpisodes = seriesId
		};
	}

	public BackgroundJobEntity Update(UpdateBackgroundJobRequestDto dto)
	{
		if (Status == EBackgroundJobStatus.COMPLETED || Status == EBackgroundJobStatus.FAILED)
			throw new Exception("Background Job ja concluido");

		if (TotalProcessedRecords > dto.TotalProcessedRecords)
			throw new Exception("Nao foi possível reduzir o progresso. O valor atual ja esta maior ao informado");

		if (dto.Status == EBackgroundJobStatus.PROCESSING && Status != EBackgroundJobStatus.PROCESSING)
			ProcessedAt = DateTime.UtcNow;

		if (dto.Status is EBackgroundJobStatus.COMPLETED or EBackgroundJobStatus.FAILED or EBackgroundJobStatus.CANCELED)
			FinishedAt = DateTime.UtcNow;

		Status = dto.Status;
		TotalRecordsToProcess = dto.TotalRecordsToProcess;
		TotalFailedRecords = dto.TotalFailedRecords;
		TotalSuccessfulRecords = dto.TotalSuccessfulRecords;
		TotalProcessedRecords = dto.TotalProcessedRecords;
		ErrorList = dto.ErrorList;
		UpdateAt = DateTime.UtcNow;

		return this;
	}

	public void UserNotify() => UserWasNotified = true;
}