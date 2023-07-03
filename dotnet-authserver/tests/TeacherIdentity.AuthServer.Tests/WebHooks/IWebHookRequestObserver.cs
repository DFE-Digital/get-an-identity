namespace TeacherIdentity.AuthServer.Tests.WebHooks;

public interface IWebHookRequestObserver
{
    void Initialize();

    void OnWebHookRequestReceived(WebHookRequest webHookRequest);

    void AssertWebHookRequestsReceived(params Action<WebHookRequest>[] requestInspectors);
}
