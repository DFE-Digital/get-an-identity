using System.Linq.Expressions;
using Castle.DynamicProxy;

namespace TeacherIdentity.AuthServer.Tests;

public class SpyRegistry
{
    private readonly Dictionary<Type, object> _allSpies = new();

    public Spy<T> Get<T>()
        where T : class
    {
        lock (_allSpies)
        {
            if (!_allSpies.ContainsKey(typeof(T)))
            {
                _allSpies[typeof(T)] = new Spy<T>();
            }

            return (Spy<T>)_allSpies[typeof(T)];
        }
    }
}

public class Spy<T>
    where T : class
{
    private readonly Mock<T> _mock;
    private readonly List<T> _wrappedInstances = new();

    public Spy()
    {
        _mock = new Mock<T>();
    }

    public T Wrap(T innerInstance)
    {
        lock (_wrappedInstances)
        {
            _wrappedInstances.Add(innerInstance);
        }

        return (T)new ProxyGenerator().CreateInterfaceProxyWithoutTarget(
            typeof(T),
            additionalInterfacesToProxy: new[] { typeof(IDisposable) },
            new RecordInvocationsAndForwardInterceptor(innerInstance, _mock, OnDispose));

        void OnDispose()
        {
            lock (_wrappedInstances)
            {
                _wrappedInstances.Remove(innerInstance);
            }
        }
    }

    public void Reset() => _mock.Reset();

    public void Verify(Expression<Action<T>> expression) =>
        _mock.Verify(expression);

    public void Verify(Expression<Action<T>> expression, Times times) =>
        _mock.Verify(expression, times);

    public void Verify(Expression<Action<T>> expression, Func<Times> times) =>
        _mock.Verify(expression, times);

    public void Verify(Expression<Action<T>> expression, string failMessage) =>
        _mock.Verify(expression, failMessage);

    public void Verify(Expression<Action<T>> expression, Times times, string failMessage) =>
        _mock.Verify(expression, times, failMessage);

    public void Verify(Expression<Action<T>> expression, Func<Times> times, string failMessage) =>
        _mock.Verify(expression, times, failMessage);

    public void Verify<TResult>(Expression<Func<T, TResult>> expression) =>
        _mock.Verify(expression);

    public void Verify<TResult>(Expression<Func<T, TResult>> expression, Times times) =>
        _mock.Verify(expression, times);

    public void Verify<TResult>(Expression<Func<T, TResult>> expression, Func<Times> times) =>
        _mock.Verify(expression, times);

    public void Verify<TResult>(Expression<Func<T, TResult>> expression, string failMessage) =>
        _mock.Verify(expression, failMessage);

    public void Verify<TResult>(Expression<Func<T, TResult>> expression, Times times, string failMessage) =>
        _mock.Verify(expression, times, failMessage);

    private class RecordInvocationsAndForwardInterceptor : IInterceptor
    {
        private readonly T _inner;
        private readonly Mock<T> _mock;
        private readonly Action _onDispose;

        public RecordInvocationsAndForwardInterceptor(T inner, Mock<T> mock, Action onDispose)
        {
            _inner = inner;
            _mock = mock;
            _onDispose = onDispose;
        }

        public void Intercept(Castle.DynamicProxy.IInvocation invocation)
        {
            if (invocation.Method == typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose)))
            {
                _onDispose();
                return;
            }

            // Log the call on the mock, so it can be verified
            invocation.Method.Invoke(_mock.Object, invocation.Arguments);

            // Invoke the method on the inner instance and return its result
            invocation.ReturnValue = invocation.Method.Invoke(_inner, invocation.Arguments);
        }
    }
}
