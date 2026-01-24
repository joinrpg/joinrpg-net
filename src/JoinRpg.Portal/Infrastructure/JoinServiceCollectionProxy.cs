using System.Collections;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.DailyJobs;

namespace JoinRpg.Portal.Infrastructure;

internal class JoinServiceCollectionProxy(IServiceCollection inner) : IJoinServiceCollection
{
    public IJoinServiceCollection AddDailyJob<TJob>() where TJob : class, IDailyJob
    {
        _ = this
            .AddScoped<TJob>()
            .AddScoped<IDailyJob, TJob>()
            .AddScoped<JobRunner<TJob>>()
            .AddScoped<IJobRunner, JobRunner<TJob>>()
            .AddHostedService<MidnightJobBackgroundService<TJob>>();
        return this;
    }

    ServiceDescriptor IList<ServiceDescriptor>.this[int index] { get => inner[index]; set => inner[index] = value; }

    int ICollection<ServiceDescriptor>.Count => inner.Count;

    bool ICollection<ServiceDescriptor>.IsReadOnly => inner.IsReadOnly;

    void ICollection<ServiceDescriptor>.Add(ServiceDescriptor item) => inner.Add(item);

    void ICollection<ServiceDescriptor>.Clear() => inner.Clear();
    bool ICollection<ServiceDescriptor>.Contains(ServiceDescriptor item) => inner.Contains(item);
    void ICollection<ServiceDescriptor>.CopyTo(ServiceDescriptor[] array, int arrayIndex) => inner.CopyTo(array, arrayIndex);
    IEnumerator<ServiceDescriptor> IEnumerable<ServiceDescriptor>.GetEnumerator() => inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => (inner as IEnumerable).GetEnumerator();
    int IList<ServiceDescriptor>.IndexOf(ServiceDescriptor item) => inner.IndexOf(item);
    void IList<ServiceDescriptor>.Insert(int index, ServiceDescriptor item) => inner.Insert(index, item);
    bool ICollection<ServiceDescriptor>.Remove(ServiceDescriptor item) => inner.Remove(item);
    void IList<ServiceDescriptor>.RemoveAt(int index) => inner.RemoveAt(index);
}
