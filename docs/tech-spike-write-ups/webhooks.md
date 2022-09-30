# Web hooks spike

## Context

We need a way for Get an identity clients to retrieve user data updates outside of a sign in journey, for example when a user's preferred name has changed.

We would prefer this to be a push-based mechanism rather than pull-based so that clients are notified in a timely manner without having to poll our APIs, incurring load on our database.

In addition the message delivery mechanism should be platform and technology-agnostic so that we're not overly-coupled to a particular cloud provider and we can deliver updates to apps written in both Ruby and .NET.

HTTP web hooks fit the bill.

Two approaches were evaluated. The first was Azure Event Grid with custom events and WebHook event delivery. The second used Azure Service Bus queues and some .NET code to deliver the messages over HTTP.


## Azure Event Grid

Azure Event Grid is a highly scalable, serverless event broker. It can receive events from Azure services but it can also receive custom events from anywhere.
Events can be delivered to a variety of Azure services or to WebHooks.

### Pros

- Event Grid manages web hook delivery and retries; there is zero load on our app and database after sending the initial message to Event Grid.
- Highly scalable; Event Grid scales to levels far beyond what we're likely to ever need.

### Cons

- WebHook configuration is tricky; each WebHook has to complete a handshake process before it can be added. Having to support this handshake mechanism is extra burden on clients and it leaks the underlying technology into clients.
- Adding a WebHook is an infrastructure concern, not an application concern. Adding a new endpoint would likely need Terraform configuration work; ideally we would be able to do this from the admin section of the Identity app itself.
- We don't have total control over message schema.
- Local development is tricky; web hooks need to be reachable by Event Grid.
- The [retry policy](https://learn.microsoft.com/en-us/azure/event-grid/delivery-and-retry#retry-schedule) is not configurable.


## .NET with an Azure Service Bus queue

In this approach the Get an identity app itself maintains the list of web hook endpoints to deliver messages to. When a new notification is published, the app sends a message to a Service Bus queue for each configured web hook endpoint.
The app then consumes the messages from the queue and attempts to send the message to the web hook endpoint. If it fails, the message will be retried later. If it keeps failing then the message will be dead-lettered.

### Pros

- The Get an identity app itself maintains the list of web hook endpoints. An admin UI can be built to enable adding and removing these endpoints without requiring any infrastructure changes or deployments.
- We have total control over the message schema.
- Local development is simple since messages are being sent from the app itself (so sending to `localhost` is possible).
- We have total control over the retry policy. With the dead letter queue we would also be able to re-send messages manually using the Azure tooling should a web hook endpoint be down for a prolonged period of time.

### Cons

- The Get an identity app itself is responsible for delivering the HTTP messages to the web hook endpoints. This is additional load on the app. This could be mitigated by moving this processing into a seperate worker process.
- More development effort.