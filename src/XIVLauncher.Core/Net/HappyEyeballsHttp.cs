using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Serilog;

namespace XIVLauncher.Core.Net;

// Source: https://slugcat.systems/post/24-06-16-ipv6-is-hard-happy-eyeballs-dotnet-httpclient/
//
// Copyright (c) 2019 Space Station 14 Contributors
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

public static class HappyEyeballsHttp
{
    private const int ConnectionAttemptDelay = 250;

    public static HttpClient CreateHttpClient(bool autoRedirect = true) =>
        new HttpClient(new SocketsHttpHandler
        {
            ConnectCallback = OnConnect,
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = autoRedirect,
            // PooledConnectionLifetime = TimeSpan.FromSeconds(1)     
        })
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

    private static async ValueTask<Stream> OnConnect(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        // Get IPs via DNS.
        var endPoint = context.DnsEndPoint;
        var resolvedAddresses = await GetIpsForHost(endPoint, cancellationToken).ConfigureAwait(false);
        if (resolvedAddresses.Length == 0)
            throw new HttpRequestException($"Host {context.DnsEndPoint.Host} resolved to no IPs");
        // Sort as specified in the RFC, interleaving.
        var ips = SortInterleaved(resolvedAddresses);
        Debug.Assert(ips.Length > 0);
        var (socket, index) = await ParallelTask(
            ips.Length,
            (i, cancel) => AttemptConnection(i, ips[i], endPoint.Port, cancel),
            TimeSpan.FromMilliseconds(ConnectionAttemptDelay),
            cancellationToken).ConfigureAwait(false);
        Log.Verbose("Successfully connected {EndPoint} to address: {Address}", endPoint, ips[index]);
        return new NetworkStream(socket, ownsSocket: true);
    }

    private static async Task<Socket> AttemptConnection(
        int index,
        IPAddress address,
        int port,
        CancellationToken cancel)
    {
        Log.Verbose("Trying IP {Address} for happy eyeballs [{Index}]", address, index);
        // The following socket constructor will create a dual-mode socket on systems where IPV6 is available.
        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            // Turn off Nagle's algorithm since it degrades performance in most HttpClient scenarios.
            NoDelay = true
        };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(address, port), cancel).ConfigureAwait(false);
            return socket;
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Happy Eyeballs to {Address} [{Index}] failed", address, index);
            socket.Dispose();
            throw;
        }
    }

    private static async Task<IPAddress[]> GetIpsForHost(DnsEndPoint endPoint, CancellationToken cancel)
    {
        if (IPAddress.TryParse(endPoint.Host, out var ip))
            return [ip];

        var entry = await Dns.GetHostEntryAsync(endPoint.Host, cancel).ConfigureAwait(false);
        return entry.AddressList;
    }

    private static IPAddress[] SortInterleaved(IPAddress[] addresses)
    {
        // Interleave returned addresses so that they are IPv6 -> IPv4 -> IPv6 -> IPv4.
        // Assuming we have multiple addresses of the same type that is.
        // As described in the RFC.

        var ipv6 = addresses.Where(x => x.AddressFamily == AddressFamily.InterNetworkV6).ToArray();
        var ipv4 = addresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToArray();

        var commonLength = Math.Min(ipv6.Length, ipv4.Length);

        var result = new IPAddress[addresses.Length];
        for (var i = 0; i < commonLength; i++)
        {
            result[i * 2] = ipv6[i];
            result[1 + i * 2] = ipv4[i];
        }

        if (ipv4.Length > ipv6.Length)
        {
            ipv4.AsSpan(commonLength).CopyTo(result.AsSpan(commonLength * 2));
        }
        else if (ipv6.Length > ipv4.Length)
        {
            ipv6.AsSpan(commonLength).CopyTo(result.AsSpan(commonLength * 2));
        }

        return result;
    }

    internal static async Task<(T, int)> ParallelTask<T>(
        int candidateCount,
        Func<int, CancellationToken, Task<T>> taskBuilder,
        TimeSpan delay,
        CancellationToken cancel) where T : IDisposable
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(candidateCount);

        using var successCts = CancellationTokenSource.CreateLinkedTokenSource(cancel);

        // All tasks we have ever tried.
        var allTasks = new List<Task<T>>();
        // Tasks we are still waiting on.
        var tasks = new List<Task<T>>();

        // The general loop here is as follows:
        // 1. Add a new task for the next IP to try.
        // 2. Wait until any task completes OR the delay happens.
        // If an error occurs, we stop checking that task and continue checking the next.
        // Every iteration we add another task, until we're full on them.
        // We keep looping until we have SUCCESS, or we run out of attempt tasks entirely.

        Task<T>? successTask = null;
        while (successTask == null && (allTasks.Count < candidateCount || tasks.Count > 0))
        {
            if (allTasks.Count < candidateCount)
            {
                // We have to queue another task this iteration.
                var newTask = taskBuilder(allTasks.Count, successCts.Token);
                tasks.Add(newTask);
                allTasks.Add(newTask);
            }

            var whenAnyDone = Task.WhenAny(tasks);
            Task<T> completedTask;

            if (allTasks.Count < candidateCount)
            {
                Log.Verbose("Waiting on ConnectionAttemptDelay");
                // If we have another one to queue, wait for a timeout instead of *just* waiting for a connection task.
                var timeoutTask = Task.Delay(delay, successCts.Token);
                var whenAnyOrTimeout = await Task.WhenAny(whenAnyDone, timeoutTask).ConfigureAwait(false);
                if (whenAnyOrTimeout != whenAnyDone)
                {
                    // Timeout finished. Go to next iteration so we queue another one.
                    continue;
                }

                completedTask = whenAnyDone.Result;
            }
            else
            {
                completedTask = await whenAnyDone.ConfigureAwait(false);
            }

            if (completedTask.IsCompletedSuccessfully)
            {
                // Success.
                successTask = completedTask;
                break;
            }
            else
            {
                // Faulted. Remove it.
                tasks.Remove(completedTask);
            }
        }

        Debug.Assert(allTasks.Count > 0);

        cancel.ThrowIfCancellationRequested();
        await successCts.CancelAsync().ConfigureAwait(false);

        if (successTask == null)
        {
            // We didn't get a single successful connection.
            throw new AggregateException(
                allTasks.Where(x => x.IsFaulted).SelectMany(x => x.Exception!.InnerExceptions));
        }

        // I don't know if this is possible but MAKE SURE that we don't get two sockets completing at once.
        // Just a safety measure.
        foreach (var task in allTasks)
        {
            if (task.IsCompletedSuccessfully && task != successTask)
                task.Result.Dispose();
        }

        return (successTask.Result, allTasks.IndexOf(successTask));
    }
}
