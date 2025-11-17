using System.Collections;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines a request that returns an empty response
/// </summary>
public abstract class WebRequestEmpty<TRequest> : WebRequest<TRequest, EmptyResponse>
    where TRequest : IWebRequest
{
}

/// <summary>
///     Defines an incoming REST request that returns a <see cref="TResponse" /> response
/// </summary>
public abstract class WebRequest<TRequest, TResponse> : WebRequest<TRequest>, IWebRequest<TResponse>
    where TResponse : IWebResponse
    where TRequest : IWebRequest
{
}

/// <summary>
///     Defines an incoming REST request that returns a <see cref="TResponse" /> response
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
// ReSharper disable once UnusedTypeParameter
public abstract partial class WebRequest<TRequest> : IWebRequest
    where TRequest : IWebRequest
{
}

/// <summary>
///     Defines an incoming REST request that returns no response
/// </summary>
public abstract class WebRequestVoid<TRequest> : WebRequest<TRequest>, IWebRequestVoid
    where TRequest : IWebRequest
{
}

/// <summary>
///     Defines an incoming REST request that returns a <see cref="Stream" />
/// </summary>
public abstract class WebRequestStream<TRequest> : WebRequest<TRequest>, IWebRequestStream
    where TRequest : IWebRequest
{
}

/// <summary>
///     Defines an incoming REST request that is an array of <see cref="TItem" />
/// </summary>
public abstract class WebRequestArray<TRequest, TResponse, TItem> : WebRequest<TRequest>, IWebRequest<TResponse>,
    IList<TItem>
    where TResponse : IWebResponse
    where TRequest : IWebRequest
{
    protected readonly List<TItem> Items = new();

    public void Add(TItem item)
    {
        Items.Add(item);
    }

    public void Clear()
    {
        Items.Clear();
    }

    public bool Contains(TItem item)
    {
        return Items.Contains(item);
    }

    public void CopyTo(TItem[] array, int arrayIndex)
    {
        Items.CopyTo(array, arrayIndex);
    }

    public int Count => Items.Count;

    public IEnumerator<TItem> GetEnumerator()
    {
        return Items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Items).GetEnumerator();
    }

    public int IndexOf(TItem item)
    {
        return Items.IndexOf(item);
    }

    public void Insert(int index, TItem item)
    {
        Items.Insert(index, item);
    }

    public bool IsReadOnly => false;

    public TItem this[int index]
    {
        get => Items[index];
        set => Items[index] = value;
    }

    public bool Remove(TItem item)
    {
        return Items.Remove(item);
    }

    public void RemoveAt(int index)
    {
        Items.RemoveAt(index);
    }
}