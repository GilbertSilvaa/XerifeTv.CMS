using Microsoft.Extensions.Options;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue;

public class BackgroundJobQueueRepository(IOptions<DBSettings> options) 
    : BaseRepository<BackgroundJobEntity>(ECollection.BACKGROUND_JOB_QUEUE, options), IBackgroundJobQueueRepository
{

}
