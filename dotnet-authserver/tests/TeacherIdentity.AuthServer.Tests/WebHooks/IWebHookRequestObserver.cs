namespace TeacherIdentity.AuthServer.Tests.WebHooks;

public interface IWebHookRequestObserver
{
    void Initialize();

    Task OnWebHookRequestReceived(WebHookRequest webHookRequest);

    void AssertWebHookRequestsReceived(params Action<WebHookRequest>[] requestInspectors);
}
