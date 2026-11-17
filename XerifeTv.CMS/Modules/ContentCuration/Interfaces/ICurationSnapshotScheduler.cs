namespace XerifeTv.CMS.Modules.ContentCuration.Interfaces;

public interface ICurationSnapshotScheduler
{
    Task ScheduleRebuildAsync(string collectionId, string collectionName);
    Task RemoveSnapshotAsync(string collectionId);
    Task ScheduleCatalogRebuildAsync();
}
