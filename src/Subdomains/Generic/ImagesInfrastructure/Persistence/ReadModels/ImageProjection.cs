using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Events.Shared.Images;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using ImagesApplication.Persistence.ReadModels;
using ImagesDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace ImagesInfrastructure.Persistence.ReadModels;

public class ImageProjection : IReadModelProjection
{
    private readonly IReadModelStore<Image> _images;

    public ImageProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _images = new ReadModelStore<Image>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _images.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.ContentType = e.ContentType;
                        dto.CreatedById = e.CreatedById;
                    },
                    cancellationToken);

            case DetailsChanged e:
                return await _images.HandleUpdateAsync(e.RootId,
                    dto =>
                    {
                        dto.Description = e.Description;
                        dto.Filename = e.Filename;
                    }, cancellationToken);

            case AttributesChanged e:
                return await _images.HandleUpdateAsync(e.RootId, dto => { dto.Size = e.Size; },
                    cancellationToken);

            case Deleted e:
                return await _images.HandleDeleteAsync(e.RootId, cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(ImageRoot);
}