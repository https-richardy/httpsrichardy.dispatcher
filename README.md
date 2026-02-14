# HttpsRichardy.Dispatcher

MediatR, once a free library, now requires a commercial license. We needed a lightweight alternative without the overhead, so we built dispatcher. It's free, open-source, and gets the job done without unnecessary complexity.

## Features

- **Two distinct dispatching modes** for different use cases
- **Zero external dependencies** - lightweight and fast
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

## Contracts

HttpsRichardy.Dispatcher provides four core interfaces:

| Interface | Purpose | Pattern |
|-----------|---------|---------|
| `IDispatchable<TResult>` | Base interface for messages | 1:1 Request-Response |
| `IDispatchableHandler<TMessage, TResult>` | Processes messages | 1:1 Request-Response |
| `IEvent` | Base interface for events | 1:N Pub/Sub |
| `IEventHandler<TEvent>` | Handles events | 1:N Pub/Sub |

## Best Practices

1. **Use IDispatcher for Commands and Queries** - When you expect a single, synchronous response
2. **Use IEventDispatcher for Side Effects** - For sending emails, logging, analytics, etc.
3. **Keep Handlers Focused** - Each handler should have a single responsibility
4. **Handle CancellationToken** - Respect cancellation tokens for graceful shutdown
5. **Use Dependency Injection** - Let the container manage handler lifetimes
6. **Separate Concerns** - Don't mix business logic with side effects; use events instead


## Contributing

Contributions are welcome! Feel free to submit issues and pull requests to help improve HttpsRichardy.Dispatcher.

## Support

If you find HttpsRichardy.Dispatcher helpful, consider giving it a ⭐ on GitHub!