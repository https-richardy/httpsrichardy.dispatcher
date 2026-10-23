# HttpsRichardy.Dispatcher

MediatR, once a free library, now requires a commercial license. We needed a lightweight alternative without the overhead, so we built dispatcher. It's free, open-source, and gets the job done without unnecessary complexity.

## Features

- **Three distinct dispatching modes** for different use cases
- **Opt-in in-memory background queue** to offload work off the HTTP pipeline
- **Automatic handler discovery** via assembly scanning
- **Full async/await support**
- **Simple and intuitive API**
- **100% free and open-source**

## Installation

```bash
dotnet add package HttpsRichardy.Dispatcher
```

## Quick Start

### 1. Configure Dependency Injection

Add HttpsRichardy.Dispatcher to your service collection:

```csharp
using HttpsRichardy.Dispatcher.Extensions;

builder.Services.AddDispatcher(options =>
{
    options.ScanAssembly<Program>();
});
```

The `AddDispatcher` method automatically scans the provided assemblies and registers all message and event handlers.

## Usage Patterns

HttpsRichardy.Dispatcher supports two distinct patterns for different scenarios:

### Pattern 1: IDispatcher - One-to-One (1:1) Request-Response

Use `IDispatcher` when you have **exactly one handler** that processes a message and returns a result. This is ideal for commands and queries where a single response is expected.

#### Define Your Message

```csharp
public sealed record UserCreationScheme : IDispatchable<UserScheme>
{
    public string Name { get; init; }
    public string Email { get; init; }
}

public sealed record UserScheme
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
}
```

#### Create a Handler

```csharp
public sealed class UserCreationHandler(IUserRepository repository) :
    IDispatchableHandler<UserCreationScheme, UserScheme>
{
    public async Task<UserScheme> HandleAsync(
        UserCreationScheme message, CancellationToken cancellation = default)
    {
        var user = UserMapper.AsUser(message);
        var createdUser = await repository.InsertAsync(user, cancellation);

        return new UserScheme
        {
            Id = createdUser.Id,
            Name = createdUser.Name,
            Email = createdUser.Email
        };
    }
}
```

#### Dispatch the Message

```csharp
[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(IDispatcher dispatcher) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUserAsync(
        [FromBody] UserCreationScheme request, CancellationToken cancellation)
    {
        var result = await dispatcher.DispatchAsync(request, cancellation);

        return Ok(result);
    }
}
```

---

### Pattern 2: IEventDispatcher - One-to-Many (1:N) Pub/Sub

Use `IEventDispatcher` when you want to publish an **event that multiple handlers can react to**. Perfect for decoupling side effects and cross-cutting concerns.

#### Define Your Event

```csharp
public sealed record UserCreatedEvent : IEvent
{
    public string UserId { get; set; }
    public string Email { get; set; }
}
```

#### Create Multiple Event Handlers

```csharp
// Handler 1: Send welcome email
public sealed class SendWelcomeEmailEventHandler(IEmailService emailService) :
    IEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent message, CancellationToken cancellation = default)
    {
        await emailService.SendWelcomeEmailAsync(message.Email, cancellation);
    }
}

// Handler 2: Update analytics
public sealed class UpdateAnalyticsEventHandler(IAnalyticsService analyticsService) :
    IEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent message, CancellationToken cancellation = default)
    {
        await analyticsService.TrackUserCreationAsync(message.UserId);
    }
}
```

#### Publish Events

```csharp
[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(IDispatcher dispatcher, IEventDispatcher eventDispatcher) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUserAsync(
        [FromBody] UserCreationScheme request, CancellationToken cancellation)
    {
        var result = await dispatcher.DispatchAsync(request, cancellation);
        var @event = request.ToEvent();

        await eventDispatcher.DispatchAsync(@event, cancellation);

        return Ok(result);
    }
}
```

All three handlers will execute automatically when the event is published!

---

### Pattern 3: IEnqueuer - In-Memory Background Queue (fire-and-forget)

Use `IEnqueuer` when you want to **offload work off the HTTP request pipeline**. The producer drops a context onto an in-memory queue and returns immediately, while a hosted `BackgroundService` consumes it and runs your handler outside the request lifetime. Ideal for exports, imports, notifications, and any slow side effect that shouldn't block the caller.

The queue is backed by `System.Threading.Channels` (unbounded, single reader). It is a single-process, in-memory queue: enqueued items are **not persisted** and are lost on restart. For durable delivery, use a real broker.

#### Enable the Queue

The queue is opt-in. Register it with its own assembly scan — only applications that call `AddMemoryQueue` take on the hosting dependency:

```csharp
using HttpsRichardy.Dispatcher.Extensions;

builder.Services.AddMemoryQueue(options =>
{
    options.ScanAssembly<Program>();
});
```

This scans the assemblies for every `IQueueHandler<TContext>` and, for each context, wires up the in-memory queue, the `IEnqueuer<TContext>`, and a background consumer.

#### Define Your Context

```csharp
public sealed record ExportCustomersScheme
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}
```

#### Create a Queue Handler

A context must have **exactly one** handler. It runs in its own dependency-injection scope, so you can safely inject scoped services such as repositories:

```csharp
public sealed class ExportCustomersHandler(ICustomerRepository repository, IStorageClient storage) :
    IQueueHandler<ExportCustomersScheme>
{
    public async Task HandleAsync(ExportCustomersScheme context, CancellationToken cancellation = default)
    {
        var customers = await repository.FilterAsync(context.From, context.To, cancellation);
        await storage.ExportAsync(customers, cancellation);
    }
}
```

#### Enqueue Work

```csharp
[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController(IEnqueuer<ExportCustomersScheme> queue) : ControllerBase
{
    [HttpPost("export")]
    public async Task<IActionResult> ExportAsync(
        [FromBody] ExportCustomersScheme request, CancellationToken cancellation)
    {
        await queue.EnqueueAsync(request, cancellation);

        return Accepted();
    }
}
```

The request returns right away; the handler runs in the background. An exception thrown by a handler is logged and the consumer keeps processing the next item.

## Contracts

HttpsRichardy.Dispatcher provides these core interfaces:

| Interface | Purpose | Pattern |
|-----------|---------|---------|
| `IDispatchable<TResult>` | Base interface for messages | 1:1 Request-Response |
| `IDispatchableHandler<TMessage, TResult>` | Processes messages | 1:1 Request-Response |
| `IEvent` | Base interface for events | 1:N Pub/Sub |
| `IEventHandler<TEvent>` | Handles events | 1:N Pub/Sub |
| `IEnqueuer<TContext>` | Enqueues work onto the in-memory queue | Background fire-and-forget |
| `IQueueHandler<TContext>` | Consumes queued work in the background | Background fire-and-forget |

## Best Practices

1. **Use IDispatcher for Commands and Queries** - When you expect a single, synchronous response
2. **Use IEventDispatcher for Side Effects** - For sending emails, logging, analytics, etc.
3. **Keep Handlers Focused** - Each handler should have a single responsibility
4. **Handle CancellationToken** - Respect cancellation tokens for graceful shutdown
5. **Use Dependency Injection** - Let the container manage handler lifetimes
6. **Separate Concerns** - Don't mix business logic with side effects; use events instead
7. **Use IEnqueuer for Slow Side Effects** - Offload exports, imports, and notifications off the request pipeline; remember the queue is in-memory and not durable across restarts


## Contributing

Contributions are welcome! Feel free to submit issues and pull requests to help improve HttpsRichardy.Dispatcher.

## Support

If you find HttpsRichardy.Dispatcher helpful, consider giving it a ⭐ on GitHub!