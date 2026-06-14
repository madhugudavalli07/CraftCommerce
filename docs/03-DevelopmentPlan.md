# CraftCommerce Development Plan

## 1. Main Development Principle

CraftCommerce should be developed in two environments:

1. Local development environment
2. Azure learning/deployment environment

The local environment should be the primary development setup. Azure should be used for deployment practice, integration testing, and learning cloud concepts.

The reason is simple: Redis and Kafka are easiest and cheapest to run locally with Docker. Azure SQL Database can be used from the beginning if the free offer is applied correctly, but the safest development flow is to support both local SQL Server and Azure SQL.

## 2. Recommended Free Development Strategy

| Component | Local Development | Azure Development/Deployment |
|---|---|---|
| .NET services | Run directly or Docker Compose | Azure Container Apps |
| Database | SQL Server Docker container | Azure SQL Database free offer |
| DB tool | SSMS / Azure Data Studio | SSMS / Azure Portal Query Editor |
| Redis | Redis Docker container | Avoid managed Redis at first; use local Redis or container Redis only for experiments |
| Kafka | Kafka Docker container | Avoid always-on Kafka in Azure free setup |
| Cloud messaging | Not needed locally | Azure Service Bus for first cloud version, free for 12 months |
| Frontend | Angular local dev server later | Azure Static Web Apps later |
| Images | Docker local build | Docker Hub or Azure Container Registry free period |

## 3. Can We Directly Use Azure SQL DB from Development?

Yes, you can directly use Azure SQL Database from development through SSMS and from your .NET services.

However, the recommended plan is:

| Stage | DB Choice | Why |
|---|---|---|
| Daily coding | Local SQL Server in Docker | Fast, no internet dependency, no free-tier risk |
| Cloud integration testing | Azure SQL Database | Validates real Azure connectivity and deployment |
| Final deployed backend | Azure SQL Database | Same cloud DB used by Azure Container Apps |

Do not depend only on Azure SQL during daily coding. If your internet is down, Azure pauses, firewall rules change, or free vCore seconds are consumed, development gets blocked.

## 4. Azure SQL Free Offer Notes

Azure SQL Database has a free offer with:

- Up to 10 General Purpose databases per subscription.
- 100,000 vCore seconds per database per month.
- 32 GB data storage per database.
- 32 GB backup storage per database.
- Option to auto-pause when the free monthly limit is reached.

Important rule: when creating the DB, confirm that the Azure portal shows the free offer applied and estimated cost as zero.

Reference: https://learn.microsoft.com/en-us/azure/azure-sql/database/free-offer?view=azuresql

## 5. How to Use SSMS

SSMS can be used for both local SQL Server and Azure SQL Database.

### Local SQL Server with SSMS

Use this during normal development.

Connection details:

| Field | Value |
|---|---|
| Server type | Database Engine |
| Server name | `localhost,1433` |
| Authentication | SQL Server Authentication |
| Login | `sa` |
| Password | Local Docker SQL password |

Example local connection string:

```text
Server=localhost,1433;Database=CraftCommerceAuthDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;
```

### Azure SQL with SSMS

Use this for cloud testing and deployment validation.

Connection details:

| Field | Value |
|---|---|
| Server type | Database Engine |
| Server name | `<your-server-name>.database.windows.net` |
| Authentication | Microsoft Entra MFA or SQL Server Authentication |
| Login | Azure SQL server admin or Entra login |
| Password | Azure SQL password if using SQL auth |
| Encryption | Strict / required by newer SSMS versions |

You must also allow your client IP in the Azure SQL server firewall.

Reference: https://learn.microsoft.com/en-us/azure/azure-sql/database/connect-query-ssms?view=azuresql

## 6. Database Development Plan

Each service owns its own database.

| Service | Local DB | Azure DB |
|---|---|---|
| Auth Service | `CraftCommerceAuthDb` | `craft-auth-db` |
| Product Service | `CraftCommerceProductDb` | `craft-product-db` |
| Order Service | `CraftCommerceOrderDb` | `craft-order-db` |
| Payment Service | `CraftCommercePaymentDb` | `craft-payment-db` |
| Notification Service | `CraftCommerceNotificationDb` optional | `craft-notification-db` optional |

Use EF Core migrations from each service.

Example:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Recommended configuration:

```text
appsettings.Development.json -> local SQL Server
appsettings.Azure.json       -> Azure SQL Database
```

Example local config:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=CraftCommerceAuthDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;"
  }
}
```

Example Azure config:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=craft-auth-db;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

## 7. Redis Development Plan

Redis should be used locally first.

Use Redis for:

- Cart storage
- Product cache
- Rate limiting later
- Idempotency keys for order/payment
- Token blacklist only if needed

### Local Redis

Run Redis using Docker Compose.

Connection string:

```text
localhost:6379
```

Example usage:

| Use Case | Redis Key |
|---|---|
| User cart | `cart:user:{userId}` |
| Anonymous cart | `cart:anon:{anonymousId}` |
| Product cache | `product:{productId}` |
| Product list cache | `products:category:{categoryId}` |
| Idempotency | `idem:{requestKey}` |

### Azure Redis

For a strict free plan, do not use Azure Managed Redis initially.

Recommended options:

| Option | Cost | Recommendation |
|---|---|---|
| Azure Managed Redis | Usually paid | Avoid for now |
| Redis in Azure Container Apps | Can consume free Container Apps allowance but not ideal for persistence | Use only for experiments |
| Redis on free/12-month VM | Possible, but operationally heavier | Optional later |
| Local Redis only | Free | Best for learning phase |

For the first Azure deployment, services should work even if Redis is disabled or optional. Product cache can be skipped in Azure. Cart can be added later.

## 8. Kafka Development Plan

Kafka should be used locally first.

Kafka is excellent for learning event-driven architecture, but it is not ideal to run permanently in Azure free-tier infrastructure because Kafka is stateful and usually needs always-running brokers.

### Local Kafka

Run Kafka using Docker Compose.

Use it for:

- `user.registered`
- `order.created`
- `payment.completed`
- `payment.failed`
- `order.status_changed`

### Azure Kafka Plan

For a strict free plan, do not deploy Kafka to Azure in the first version.

Recommended approach:

| Environment | Messaging |
|---|---|
| Local development | Kafka |
| Azure first deployment | Azure Service Bus |
| Azure always-free experiment | Azure Event Grid for simple events |
| Advanced learning | Azure Event Hubs Kafka-compatible endpoint or self-hosted Kafka |

Azure Service Bus Standard is listed as free for 12 months with 750 hours and 13 million operations. Event Grid is listed as always free for 100,000 operations per month.

Reference: https://azure.microsoft.com/en-us/pricing/free-services

## 9. Event Bus Abstraction

To support both Kafka locally and Azure messaging in cloud, use an abstraction.

```csharp
public interface IEventBus
{
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
}
```

Implementations:

```text
KafkaEventBus          -> Local development
AzureServiceBusEventBus -> Azure deployment
InMemoryEventBus       -> Unit tests
```

Configuration:

```json
{
  "EventBus": {
    "Provider": "Kafka"
  }
}
```

Azure configuration:

```json
{
  "EventBus": {
    "Provider": "AzureServiceBus"
  }
}
```

This lets you learn Kafka locally while still deploying cheaply to Azure.

## 10. Local Docker Compose Plan

Local Docker Compose should include:

```text
sqlserver
redis
kafka
zookeeper or kraft kafka setup
auth-service
product-service
order-service
payment-service
notification-service
gateway
```

Development flow:

1. Start infrastructure containers.
2. Run EF migrations.
3. Start .NET services.
4. Test APIs through the gateway.
5. Inspect DB using SSMS.
6. Inspect Redis using Redis CLI or RedisInsight.
7. Inspect Kafka using Kafka UI or console consumers.

## 11. Azure Deployment Plan With Free-First Thinking

### Azure Resources

| Resource | Free Plan? | Usage |
|---|---|---|
| Azure SQL Database | Always-free offer available | Main DB per service |
| Azure Container Apps | Always-free monthly grants | Host .NET services |
| Azure Static Web Apps | Free tier | Angular frontend later |
| Event Grid | Always-free monthly operations | Simple cloud events |
| Service Bus | 12 months free | Better queue/topic messaging |
| Azure Managed Redis | Not recommended for free plan | Avoid first |
| Event Hubs | Not first choice for free plan | Use later for Kafka compatibility |

Azure Container Apps free monthly grant includes:

- 180,000 vCPU seconds
- 360,000 GiB seconds
- 2 million HTTP requests

Reference: https://learn.microsoft.com/en-us/azure/container-apps/billing

### Azure Services to Deploy First

Deploy only the minimum:

1. Gateway
2. Auth Service
3. Product Service
4. Order Service
5. Payment Service
6. Notification Worker
7. Azure SQL DBs
8. Service Bus or Event Grid

Do not deploy Redis or Kafka to Azure in the first free version.

## 12. Environment Strategy

Use three environment profiles.

| Environment | Purpose | DB | Redis | Messaging |
|---|---|---|---|---|
| `Local` | Daily coding | Local SQL Server | Local Redis | Local Kafka |
| `CloudDev` | Azure testing | Azure SQL | Optional/disabled | Service Bus/Event Grid |
| `ProductionLearning` | Final learning deployment | Azure SQL | Optional/disabled | Service Bus |

## 13. Service Configuration Rules

Each service should read configuration from:

1. `appsettings.json`
2. `appsettings.Development.json`
3. Environment variables
4. Azure Container App secrets

Never hardcode:

- DB passwords
- JWT signing keys
- Service Bus connection strings
- Redis passwords
- Kafka credentials

## 14. Recommended First Development Milestones

### Milestone 1: Solution Setup

- Create .NET solution.
- Create service projects.
- Create shared libraries.
- Add Docker Compose infrastructure.

### Milestone 2: Local SQL + SSMS

- Run SQL Server in Docker.
- Connect using SSMS.
- Create Auth DB using EF migrations.
- Verify tables in SSMS.

### Milestone 3: Auth Service

- Signup
- Login
- Password hashing
- JWT generation
- Refresh token table

### Milestone 4: Product Service

- Product CRUD
- Categories
- Inventory table
- Product listing API

### Milestone 5: Gateway

- Add YARP.
- Route `/api/auth/*`.
- Route `/api/products/*`.
- Validate JWT for protected routes.

### Milestone 6: Kafka Local

- Add Kafka to Docker Compose.
- Publish `user.registered`.
- Publish `order.created`.
- Consume events in Notification Service.

### Milestone 7: Redis Local

- Add Redis to Docker Compose.
- Add product cache.
- Add cart storage.

### Milestone 8: Azure SQL

- Create free Azure SQL DBs.
- Connect from SSMS.
- Run migrations against Azure SQL.
- Test services locally using Azure SQL connection strings.

### Milestone 9: Azure Container Apps

- Create Dockerfiles.
- Push images.
- Deploy services.
- Use Azure SQL.
- Use Service Bus or Event Grid.

### Milestone 10: Angular Frontend

- Build login.
- Product listing.
- Cart.
- Checkout.
- Order history.

## 15. Cost-Control Rules

- Use local Redis and Kafka during development.
- Use Azure SQL only with the free offer applied.
- Keep Azure SQL configured to auto-pause when free limits are reached.
- Disconnect SSMS Object Explorer when not using it because open connections can prevent auto-pause.
- Keep Azure Container Apps min replicas at 0 where possible.
- Do not run Kafka 24/7 in Azure.
- Do not use Azure Managed Redis in the first version.
- Set Azure budget alerts.
- Check cost analysis every few days.
- Avoid AKS for now.

## 16. Final Recommendation

Use this plan:

```text
Daily development:
  .NET services on local machine
  SQL Server in Docker
  Redis in Docker
  Kafka in Docker
  SSMS for database inspection

Cloud testing:
  .NET services in Azure Container Apps
  Azure SQL Database free offer
  Azure Service Bus for 12-month free messaging
  No Azure Redis initially
  No Azure Kafka initially

Later advanced learning:
  Event Hubs Kafka endpoint
  Managed Redis or Redis container
  AKS
```

This gives the best balance between learning real architecture and keeping the project free.
