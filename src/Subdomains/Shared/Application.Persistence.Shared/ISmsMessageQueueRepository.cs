using Application.Persistence.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Common;

namespace Application.Persistence.Shared;

public interface ISmsMessageQueueRepository : IMessageQueueStore<SmsMessage>, IApplicationRepository
{
#if TESTINGONLY
    new Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken);
#endif
}