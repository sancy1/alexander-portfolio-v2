polyglot 

Monorepo

Polorepo

Monolite



74.220.48.0/24

74.220.48.196/32

74.220.56.0/24



https://alexander-portfolio-apigateway.onrender.com/health

https://alexander-portfolio-apigateway.onrender.com/api/v1/health



https://alexander-portfolio-authservice.onrender.com/api/v1/Health

**https://alexander-portfolio-authservice.onrender.com/swagger/index.html**

https://alexander-portfolio-apigateway.onrender.com/api/v1/health/test/rabbitmq



https://alexander-portfolio-apigateway.onrender.com/api/v1/auth/google/login

https://alexander-portfolio-apigateway.onrender.com/api/v1/auth/github/login





https://pingrobot-seven.vercel.app/dashboard

https://elexousia-weatherforecast-lovat.vercel.app/auth/callback?success=true





Outbox Message

{

&#x20; "eventId": "1eb6dd93-5fe4-4742-943d-70648612bdc1",

&#x20; "eventType": "admin.loggedin",

&#x20; "sourceService": "auth-service",

&#x20; "timestamp": "2026-06-06T12:14:52.276782Z",

&#x20; "version": "1.0",

&#x20; "userId": "11adabcd-664f-4c08-94f2-400b603d4bef",

&#x20; "userType": "admin",

&#x20; "correlationId": null,

&#x20; "causationId": null,

&#x20; "payload": {

&#x20;   "userId": "11adabcd-664f-4c08-94f2-400b603d4bef",

&#x20;   "email": "alexander.s.cyril@gmail.com",

&#x20;   "displayName": "alexander\_new",

&#x20;   "loginMethod": "password",

&#x20;   "loginTime": "2026-06-06T12:14:52Z",

&#x20;   "clientIp": "::1",

&#x20;   "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36 Avast/147.0.0.0"

&#x20; }

}





\-----------------------

Auth Service Structure

\-----------------------

auth-service/

│   ├── AuthService.API/

│   │   ├── Controllers/

│   │   │   ├── AdminAuthController.cs

│   │   │   ├── HealthController.cs

│   │   │   ├── OutboxController.cs

│   │   │   └── SocialAuthController.cs

│   │   ├── Extensions/

│   │   │   └── ServiceCollectionExtensions.cs

│   │   ├── Middleware/

│   │   │   ├── GatewaySecretMiddleware.cs

│   │   │   ├── JwtBlacklistMiddleware.cs

│   │   │   ├── JwtMiddleware.cs

│   │   │   ├── RateLimitingMiddleware.cs

│   │   │   └── RequestLoggingMiddleware.cs

│   │   ├── .env

│   │   ├── .env.example

│   │   ├── AuthService.API.csproj

│   │   ├── Program.cs

│   │   ├── appsettings.Development.json

│   │   └── appsettings.json

│   ├── AuthService.Application/

│   │   ├── Common/

│   │   │   ├── AdminKeySettings.cs

│   │   │   ├── CloudinarySettings.cs

│   │   │   ├── DatabaseSettings.cs

│   │   │   ├── EventEnvelope.cs

│   │   │   ├── JwtSettings.cs

│   │   │   ├── OAuthSettings.cs

│   │   │   └── OutboxHelper.cs

│   │   ├── DTOs/

│   │   │   ├── Events/

│   │   │   │   ├── UserLoggedInEvent.cs

│   │   │   │   ├── UserModifiedEvent.cs

│   │   │   │   └── UserRegisteredEvent.cs

│   │   │   ├── Requests/

│   │   │   │   ├── AdminBlockSocialUserRequest.cs

│   │   │   │   ├── AdminDeleteSocialUserRequest.cs

│   │   │   │   ├── AdminLoginRequest.cs

│   │   │   │   ├── AdminRegisterRequest.cs

│   │   │   │   ├── AdminUpdateSocialUserRequest.cs

│   │   │   │   ├── AvatarUploadRequest.cs

│   │   │   │   ├── ChangePasswordRequest.cs

│   │   │   │   ├── CompleteProfileRequest.cs

│   │   │   │   ├── DeleteAccountRequest.cs

│   │   │   │   ├── DeleteSocialUserRequest.cs

│   │   │   │   ├── ForgotPasswordRequest.cs

│   │   │   │   ├── PasswordChangeRequest.cs

│   │   │   │   ├── PasswordResetRequest.cs

│   │   │   │   ├── ResetPasswordRequest.cs

│   │   │   │   ├── RestoreAccountRequest.cs

│   │   │   │   ├── RestoreSocialAccountRequest.cs

│   │   │   │   ├── SocialSignupRequest.cs

│   │   │   │   ├── UpdateAdminProfileRequest.cs

│   │   │   │   ├── UpdatePublicProfileRequest.cs

│   │   │   │   └── UpdateSocialProfileRequest.cs

│   │   │   └── Responses/

│   │   │       ├── AdminLoginResponse.cs

│   │   │       ├── AdminProfileResponse.cs

│   │   │       ├── AuthResponse.cs

│   │   │       ├── DeleteAccountResponse.cs

│   │   │       ├── ErrorResponse.cs

│   │   │       ├── UpdateAdminProfileResponse.cs

│   │   │       └── UserResponse.cs

│   │   ├── Features/

│   │   │   ├── Admin/

│   │   │   │   ├── Commands/

│   │   │   │   │   ├── AdminBlockSocialUserCommand.cs

│   │   │   │   │   ├── AdminBlockSocialUserHandler.cs

│   │   │   │   │   ├── AdminDeleteSocialUserCommand.cs

│   │   │   │   │   ├── AdminDeleteSocialUserHandler.cs

│   │   │   │   │   ├── AdminUnblockSocialUserCommand.cs

│   │   │   │   │   ├── AdminUnblockSocialUserHandler.cs

│   │   │   │   │   ├── ChangePasswordCommand.cs

│   │   │   │   │   ├── ChangePasswordHandler.cs

│   │   │   │   │   ├── DeleteAccountCommand.cs

│   │   │   │   │   ├── DeleteAccountHandler.cs

│   │   │   │   │   ├── LoginAdminCommand.cs

│   │   │   │   │   ├── LoginAdminHandler.cs

│   │   │   │   │   ├── LogoutAdminCommand.cs

│   │   │   │   │   ├── LogoutAdminHandler.cs

│   │   │   │   │   ├── RegisterAdminCommand.cs

│   │   │   │   │   ├── RegisterAdminHandler.cs

│   │   │   │   │   ├── RequestPasswordResetCommand.cs

│   │   │   │   │   ├── RequestPasswordResetHandler.cs

│   │   │   │   │   ├── ResetPasswordCommand.cs

│   │   │   │   │   ├── ResetPasswordHandler.cs

│   │   │   │   │   ├── RestoreAccountCommand.cs

│   │   │   │   │   ├── RestoreAccountHandler.cs

│   │   │   │   │   ├── UpdateAdminProfileCommand.cs

│   │   │   │   │   ├── UpdateAdminProfileHandler.cs

│   │   │   │   │   ├── UpdateAdminPublicProfileCommand.cs

│   │   │   │   │   ├── UpdateAdminPublicProfileHandler.cs

│   │   │   │   │   ├── UploadAvatarCommand.cs

│   │   │   │   │   └── UploadAvatarHandler.cs

│   │   │   │   └── Queries/

│   │   │   │       ├── GetAdminByUsernameHandler.cs

│   │   │   │       ├── GetAdminByUsernameQuery.cs

│   │   │   │       ├── GetAdminProfileHandler.cs

│   │   │   │       └── GetAdminProfileQuery.cs

│   │   │   └── Social/

│   │   │       ├── Commands/

│   │   │       │   ├── CompleteUserProfileCommand.cs

│   │   │       │   ├── CompleteUserProfileHandler.cs

│   │   │       │   ├── CreateSocialUserCommand.cs

│   │   │       │   ├── CreateSocialUserHandler.cs

│   │   │       │   ├── DeleteSocialUserCommand.cs

│   │   │       │   ├── DeleteSocialUserHandler.cs

│   │   │       │   ├── HandleOAuthLoginCommand.cs

│   │   │       │   ├── HandleOAuthLoginHandler.cs

│   │   │       │   ├── UpdateSocialPublicProfileCommand.cs

│   │   │       │   ├── UpdateSocialPublicProfileHandler.cs

│   │   │       │   ├── UpdateSocialUserProfileCommand.cs

│   │   │       │   ├── UpdateSocialUserProfileHandler.cs

│   │   │       │   ├── UploadSocialUserAvatarCommand.cs

│   │   │       │   └── UploadSocialUserAvatarHandler.cs

│   │   │       └── Queries/

│   │   │           ├── GetUserByEmailQuery.cs

│   │   │           ├── GetUserByIdQuery.cs

│   │   │           └── GetUserByProviderQuery.cs

│   │   ├── Interfaces/

│   │   │   ├── Messaging/

│   │   │   │   ├── IKafkaProducer.cs

│   │   │   │   ├── IMessagePublisher.cs

│   │   │   │   ├── IMessageSubscriber.cs

│   │   │   │   └── NullKafkaProducer.cs

│   │   │   ├── Persistence/

│   │   │   │   ├── IAdminRepository.cs

│   │   │   │   ├── IOutboxRepository.cs

│   │   │   │   ├── ISocialUserRepository.cs

│   │   │   │   └── IUnitOfWork.cs

│   │   │   ├── Security/

│   │   │   │   ├── IAdminKeyValidator.cs

│   │   │   │   └── ITokenBlacklistService.cs

│   │   │   └── Services/

│   │   │       └── ICloudinaryService.cs

│   │   ├── Services/

│   │   ├── Validators/

│   │   │   ├── AdminLoginValidator.cs

│   │   │   ├── AdminRegisterValidator.cs

│   │   │   ├── ChangePasswordValidator.cs

│   │   │   ├── PasswordResetValidator.cs

│   │   │   ├── ResetPasswordValidator.cs

│   │   │   └── SocialSignupValidator.cs

│   │   └── AuthService.Application.csproj

│   ├── AuthService.Domain/

│   │   ├── Entities/

│   │   │   ├── Admin.cs

│   │   │   ├── OutboxMessage.cs

│   │   │   └── SocialUser.cs

│   │   ├── Enums/

│   │   │   ├── AccountStatus.cs

│   │   │   ├── SocialProvider.cs

│   │   │   └── UserRole.cs

│   │   ├── Exceptions/

│   │   │   ├── AdminKeyInvalidException.cs

│   │   │   ├── DomainException.cs

│   │   │   └── InvalidCredentialsException.cs

│   │   ├── Interfaces/

│   │   │   ├── IJwtGenerator.cs

│   │   │   ├── IOAuthValidator.cs

│   │   │   └── IPasswordHasher.cs

│   │   ├── ValueObjects/

│   │   │   ├── AdminKey.cs

│   │   │   ├── Email.cs

│   │   │   ├── PasswordHash.cs

│   │   │   └── ProviderId.cs

│   │   └── AuthService.Domain.csproj

│   ├── AuthService.Infrastructure/

│   │   ├── Caching/

│   │   │   ├── IRedisCacheService.cs

│   │   │   ├── RedisCacheService.cs

│   │   │   └── TokenBlacklistService.cs

│   │   ├── ExternalServices/

│   │   │   ├── GitHub/

│   │   │   ├── Google/

│   │   │   ├── GitHubAuthClient.cs

│   │   │   └── GoogleAuthClient.cs

│   │   ├── Messaging/

│   │   │   ├── Kafka/

│   │   │   │   ├── KafkaConsumer.cs

│   │   │   │   ├── KafkaProducer.cs

│   │   │   │   └── KafkaSettings.cs

│   │   │   └── RabbitMQ/

│   │   │       ├── RabbitMQConnectionManager

│   │   │       ├── RabbitMQPublisher.cs

│   │   │       ├── RabbitMQSettings.cs

│   │   │       └── RabbitMQSubscriber.cs

│   │   ├── Migrations/

│   │   │   ├── 20260521123000\_InitialCreate.Designer.cs

│   │   │   ├── 20260521123000\_InitialCreate.cs

│   │   │   ├── 20260521183908\_AddAvatarUrlToAdmin.Designer.cs

│   │   │   ├── 20260521183908\_AddAvatarUrlToAdmin.cs

│   │   │   ├── 20260522151414\_AddDeleteColumnsToAdmin.Designer.cs

│   │   │   ├── 20260522151414\_AddDeleteColumnsToAdmin.cs

│   │   │   ├── 20260522182224\_AddDeleteColumnsToSocialUsers.Designer.cs

│   │   │   ├── 20260522182224\_AddDeleteColumnsToSocialUsers.cs

│   │   │   ├── 20260523102411\_AddAdminBlockColumnsToSocialUsers.Designer.cs

│   │   │   ├── 20260523102411\_AddAdminBlockColumnsToSocialUsers.cs

│   │   │   ├── 20260531234855\_AddPublicProfileFields.Designer.cs

│   │   │   ├── 20260531234855\_AddPublicProfileFields.cs

│   │   │   └── AppDbContextModelSnapshot.cs

│   │   ├── Persistence/

│   │   │   ├── Configurations/

│   │   │   │   ├── AdminConfiguration.cs

│   │   │   │   └── SocialUserConfiguration.cs

│   │   │   ├── Migrations/

│   │   │   ├── Repositories/

│   │   │   │   ├── AdminRepository.cs

│   │   │   │   ├── OutboxRepository.cs

│   │   │   │   ├── SocialUserRepository.cs

│   │   │   │   └── UnitOfWork.cs

│   │   │   ├── AppDbContext.cs

│   │   │   ├── DatabaseConnectionManager.cs

│   │   │   ├── DatabaseStartupVerifier.cs

│   │   │   └── DesignTimeDbContextFactory.cs

│   │   ├── Security/

│   │   │   ├── AdminKeyValidator.cs

│   │   │   ├── JwtGenerator.cs

│   │   │   ├── OAuthValidator.cs

│   │   │   └── PasswordHasher.cs

│   │   ├── Services/

│   │   │   ├── CloudinaryService.cs

│   │   │   ├── IOutboxProcessorService.cs

│   │   │   └── OutboxProcessorService.cs

│   │   └── AuthService.Infrastructure.csproj

│   ├── AuthService.Tests/

│   │   ├── IntegrationTests/

│   │   │   ├── Controllers/

│   │   │   │   ├── AdminAuthControllerTests.cs

│   │   │   │   └── SocialAuthControllerTests.cs

│   │   │   ├── Messaging/

│   │   │   │   └── BrokerTests.cs

│   │   │   └── Persistence/

│   │   │       └── RepositoryTests.cs

│   │   ├── TestHelpers/

│   │   │   └── MockDataFactory.cs

│   │   ├── UnitTests/

│   │   │   ├── Application/

│   │   │   │   ├── AdminLoginValidatorTests.cs

│   │   │   │   └── AdminRegisterValidatorTests.cs

│   │   │   └── Domain/

│   │   │       ├── AdminKeyTests.cs

│   │   │       ├── AdminTests.cs

│   │   │       ├── EmailTests.cs

│   │   │       ├── PasswordHashTests.cs

│   │   │       └── SocialUserTests.cs

│   │   └── AuthService.Tests.csproj

│   ├── .dockerignore

│   ├── .env.production

│   ├── AuthService.sln

│   ├── Dockerfile

│   ├── README.md

│   ├── ca.pem

│   ├── dotnet

│   └── project.json

































OLD:

https://alexander-portfolio-apigateway.onrender.com

NEW:

https://alexander-portfolio-authservice.onrender.com



SO DO YOU REMEMBER THE INSTRUCTIONS APPLIED WHEN IMPLEMENTING RabbitMQ such as  such as 1. Avioding memroy leak 2. Sending through refrence to aviod too much load on databse so we dont hit our limit too fast and any other important thing we implemnted? now we want to implemnt kafka side by side wit RabbitMQ i need you to give the guidelines for professional implemntation in terms of similar to the RabbitMQ we did. this is not a lot of text just a few ethicks i must follow to meet profesional implementation just like RabbitMQ 

&#x20;

RESEARCH FINDINGS:

To implement Apache Kafka professionally side-by-side with RabbitMQ, you must account for Kafka's unique architecture. While RabbitMQ acts as a traditional message broker, Kafka operates as a distributed commit log.Here are the enterprise guidelines to prevent memory leaks, optimize database load, and ensure production stability.🧠 1. Eliminating Memory Leaks in KafkaKafka clients maintain internal buffers and persistent network connections. Improper lifecycle management will quickly exhaust your application's heap memory.Singleton Client Instances: Reuse your Kafka Producer instance across the entire application lifecycle. Creating a new producer per request creates a severe memory leak.Explicit Consumer Closure: Always call consumer.close() inside a finally block when a worker stops. This releases network sockets and internal fetch buffers immediately.Tune Fetch Sizes: Restrict memory spikes by limiting max.partition.fetch.bytes and max.poll.records. This prevents your consumers from pulling more data into memory than they can process at once.Deregister Event Listeners: Unsubscribe or remove event listeners (like on('message') or error handlers) if a consumer connection drops and reconnects.💾 2. Sending by Reference (Database Load Management)Just like your RabbitMQ design, Kafka messages should be lightweight indicators, not heavy data payloads.The Claim Check Pattern: Pass only the data record's unique ID (e.g., userId or transactionId) and the database state change type in the Kafka message payload.Cache-Backed Consumers: When consumers receive the reference ID, check a distributed cache (like Redis) before querying the primary database.Batch Queries: Configure consumers to batch-read Kafka records and execute a single WHERE ID IN (...) SQL query instead of hitting the database for every single message.Backpressure with Pause/Resume: If the database becomes slow or reaches connection limits, programmatically call consumer.pause() to stop fetching new messages, then consumer.resume() once the database recovers.🏗️ 3. Architecture \& Operational EthicsRunning Kafka alongside RabbitMQ requires clear boundaries regarding which broker handles which workload.Segregate the Responsibilities: Use RabbitMQ for complex routing and transient, short-lived tasks (e.g., triggering email alerts). Use Kafka for high-throughput stream processing, activity tracking, and event sourcing.Define Retention Policies: Unlike RabbitMQ (where messages disappear after consumption), Kafka stores messages on disk. Set strict time-based (log.retention.hours) or size-based retention limits to save disk space.Commit Offsets Wisely: Disable auto-commits (enable.auto.commit = false) in production. Manually commit offsets only after your application successfully processes the message and updates the database to prevent data loss.Implement Dead Letter Queues (DLQ): Route malformed payloads to a designated Kafka "error" topic immediately. Never let a poisonous message block the consumer loop, which stalls the entire partition.



HOW TO START. PLEASE RECALL THE PROJECT STRUCTURE THEN REQUEST FOR FILES NEEDED TO IMPLEMNT KAFKA CAREFULLY WITHOUT ERRORS ALONG SIDE ALREADY WORKING RABBITMQ. NOTE THAT THEY MUST BE WELL SEPERATED TO BE USE EITHER INDEPENDLY OR TOGETHER BASED ON THE SERVICE THAT WILL CONSUM THEM. ALREADY, REDIS IS WORKING AND SO IS RABITMQ NOW WE MUCG IMPLEMNT KAFKA WITH BEST INDUSTRIAL PRACTICES AS LAYEDOUT ABOVE. 



REQUEST FILES WITH FILEPATH AND NAME FOR ME TO UPLAOD THE WRITE THE APP







&#x20;



