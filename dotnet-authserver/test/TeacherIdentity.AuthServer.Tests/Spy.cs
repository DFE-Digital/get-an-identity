using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Castle.DynamicProxy;

namespace TeacherIdentity.AuthServer.Tests;

public static class Spy
{
    // Hold onto all spies so we can get a Spy<T> from the proxied <T>;
    // key is the generated proxy object and the value is its owning Spy<T>
    private static readonly ConditionalWeakTable<object, object> _allSpies = new();

    public static T Of<T>(T wrappedInstance) where T : class =>
        new Spy<T>(wrappedInstance).Object;

    public static Spy<T> Get<T>(T proxy)
        where T : class
    {
        if (!_allSpies.TryGetValue(proxy, out var spy))
        {
            throw new ArgumentException("Instance is not a known spy.");
        }

        return (Spy<T>)spy;
    }

    internal static void TrackSpy<T>(Spy<T> spy) where T : class =>
        _allSpies.Add(spy.Object, spy);
}

public class Spy<T>
    where T : class
{
    private static readonly ConcurrentDictionary<Type, object> _mocksPerType = new();

    private readonly Mock<T> _mock;

    public Spy(T wrappedInstance)
    {
        _mock = (Mock<T>)_mocksPerType.GetOrAdd(typeof(T), _ => new Mock<T>());

        Object = new ProxyGenerator().CreateInterfaceProxyWithoutTarget<T>(
            new RecordInvocationsAndForwardInterceptor(wrappedInstance, _mock));

        Spy.TrackSpy(this);
    }

    public T Object { get; }

    public static implicit operator T(Spy<T> spy) => spy.Object;

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

        public RecordInvocationsAndForwardInterceptor(T inner, Mock<T> mock)
        {
            _inner = inner;
            _mock = mock;
        }

        public void Intercept(Castle.DynamicProxy.IInvocation invocation)
        {
            // Log the call on the mock, so it can be verified
            invocation.Method.Invoke(_mock.Object, invocation.Arguments);

            // Invoke the method on the inner instance and return its result
            invocation.ReturnValue = invocation.Method.Invoke(_inner, invocation.Arguments);
        }
    }
}
