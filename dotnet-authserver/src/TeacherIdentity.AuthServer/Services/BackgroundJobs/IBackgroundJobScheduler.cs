using System.Linq.Expressions;

namespace TeacherIdentity.AuthServer.Services.BackgroundJobs;

public interface IBackgroundJobScheduler
{
    Task Enqueue<T>(Expression<Func<T, Task>> expression) where T : notnull;
}
