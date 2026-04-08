using AncillaryApplication.Persistence;
using Application.Persistence.Shared;
using Common;
using Domain.Common.Identity;

namespace AncillaryApplication;

public partial class AncillaryApplication : IAncillaryApplication
{
    private readonly IAuditRepository _auditRepository;
    private readonly IEmailDeliveryRepository _emailDeliveryRepository;
    private readonly ISmsDeliveryRepository _smsDeliveryRepository;
    private readonly IEmailDeliveryService _emailDeliveryService;
    private readonly ISmsDeliveryService _smsDeliveryService;
    private readonly IIdentifierFactory _idFactory;
    private readonly IRecorder _recorder;
    private readonly IUsageDeliveryService _usageDeliveryService;
    private readonly IProvisioningNotificationService _provisioningNotificationService;
#if TESTINGONLY
    private readonly IAuditMessageQueueRepository _auditMessageRepository;
    private readonly IEmailMessageQueueRepository _emailMessageRepository;
    private readonly ISmsMessageQueueRepository _smsMessageRepository;
    private readonly IUsageMessageQueueRepository _usageMessageRepository;
    private readonly IProvisioningMessageQueueRepository _provisioningMessageRepository;

    public AncillaryApplication(IRecorder recorder, IIdentifierFactory idFactory,
        IUsageMessageQueueRepository usageMessageRepository, IUsageDeliveryService usageDeliveryService,
        IAuditMessageQueueRepository auditMessageRepository, IAuditRepository auditRepository,
        IEmailMessageQueueRepository emailMessageRepository, IEmailDeliveryService emailDeliveryService,
        IEmailDeliveryRepository emailDeliveryRepository,
        ISmsMessageQueueRepository smsMessageRepository, ISmsDeliveryService smsDeliveryService,
        ISmsDeliveryRepository smsDeliveryRepository,
        IProvisioningMessageQueueRepository provisioningMessageRepository,
        IProvisioningNotificationService provisioningNotificationService)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _usageMessageRepository = usageMessageRepository;
        _usageDeliveryService = usageDeliveryService;
        _auditMessageRepository = auditMessageRepository;
        _auditRepository = auditRepository;
        _emailMessageRepository = emailMessageRepository;
        _emailDeliveryService = emailDeliveryService;
        _emailDeliveryRepository = emailDeliveryRepository;
        _smsMessageRepository = smsMessageRepository;
        _smsDeliveryService = smsDeliveryService;
        _smsDeliveryRepository = smsDeliveryRepository;
        _provisioningMessageRepository = provisioningMessageRepository;
        _provisioningNotificationService = provisioningNotificationService;
    }
#else
    public AncillaryApplication(IRecorder recorder, IIdentifierFactory idFactory,
        // ReSharper disable once UnusedParameter.Local
        IUsageMessageQueueRepository usageMessageQueue, IUsageDeliveryService usageDeliveryService,
        // ReSharper disable once UnusedParameter.Local
        IAuditMessageQueueRepository auditMessageQueueRepository, IAuditRepository auditRepository,
        // ReSharper disable once UnusedParameter.Local
        IEmailMessageQueueRepository emailMessageQueue, IEmailDeliveryService emailDeliveryService,
        IEmailDeliveryRepository emailDeliveryRepository,
        // ReSharper disable once UnusedParameter.Local
        ISmsMessageQueueRepository smsMessageQueue, ISmsDeliveryService smsDeliveryService,
        ISmsDeliveryRepository smsDeliveryRepository,
        // ReSharper disable once UnusedParameter.Local
        IProvisioningMessageQueueRepository provisioningMessageQueue,
        IProvisioningNotificationService provisioningNotificationService)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _usageDeliveryService = usageDeliveryService;
        _auditRepository = auditRepository;
        _emailDeliveryService = emailDeliveryService;
        _emailDeliveryRepository = emailDeliveryRepository;
        _smsDeliveryService = smsDeliveryService;
        _smsDeliveryRepository = smsDeliveryRepository;
        _provisioningNotificationService = provisioningNotificationService;
    }
#endif
}