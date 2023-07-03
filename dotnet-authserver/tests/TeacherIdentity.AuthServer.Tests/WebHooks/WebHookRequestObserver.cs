namespace TeacherIdentity.AuthServer.Tests.WebHooks;

public class WebHookRequestObserver : IWebHookRequestObserver
{
    private readonly AsyncLocal<List<WebHookRequest>> _requests = new();

    public void Initialize() => _requests.Value ??= new List<WebHookRequest>();

    public Task OnWebHookRequestReceived(WebHookRequest webHookRequest)
    {
        if (_requests.Value is null)
        {
            throw new InvalidOperationException("Not initialized.");
        }

        _requests.Value.Add(webHookRequest);

        return Task.CompletedTask;
    }

    public void AssertWebHookRequestsReceived(params Action<WebHookRequest>[] requestInspectors)
    {
        var requests = (_requests.Value ?? new()).AsReadOnly();
        Assert.Collection(requests, requestInspectors);
    }
}
