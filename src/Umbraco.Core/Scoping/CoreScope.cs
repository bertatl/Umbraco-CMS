﻿using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.DistributedLocking;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.IO;

namespace Umbraco.Cms.Core.Scoping;

public class CoreScope : ICoreScope
{
    protected bool? _completed;
    private ICompletable? _scopedFileSystem;
    private IScopedNotificationPublisher? _notificationPublisher;
    private IsolatedCaches? _isolatedCaches;

    private readonly RepositoryCacheMode _repositoryCacheMode;
    private readonly bool? _shouldScopeFileSystems;
    private readonly IEventAggregator _eventAggregator;

    private bool _disposed;

    public CoreScope(
        IDistributedLockingMechanismFactory distributedLockingMechanismFactory,
        ILoggerFactory loggerFactory,
        FileSystems scopedFileSystem,
        IEventAggregator eventAggregator,
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified,
        bool? shouldScopeFileSystems = null,
        IScopedNotificationPublisher? notificationPublisher = null)
    {
        _eventAggregator = eventAggregator;
        InstanceId = Guid.NewGuid();
        CreatedThreadId = Environment.CurrentManagedThreadId;
        Locks = ParentScope is null
            ? new LockingMechanism(distributedLockingMechanismFactory, loggerFactory.CreateLogger<LockingMechanism>())
            : ResolveLockingMechanism();
        _repositoryCacheMode = repositoryCacheMode;
        _shouldScopeFileSystems = shouldScopeFileSystems;
        _notificationPublisher = notificationPublisher;

        if (_shouldScopeFileSystems is true)
        {
            _scopedFileSystem = scopedFileSystem.Shadow();
        }
    }

    public CoreScope(
        CoreScope? parentScope,
        IDistributedLockingMechanismFactory distributedLockingMechanismFactory,
        ILoggerFactory loggerFactory,
        FileSystems scopedFileSystem,
        IEventAggregator eventAggregator,
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified,
        bool? shouldScopeFileSystems = null,
        IScopedNotificationPublisher? notificationPublisher = null)
    : this(distributedLockingMechanismFactory, loggerFactory, scopedFileSystem, eventAggregator, repositoryCacheMode, shouldScopeFileSystems, notificationPublisher)
    {
        if (parentScope is null)
        {
            return;
        }

        Locks = parentScope.Locks;

        // cannot specify a different fs scope!
        // can be 'true' only on outer scope (and false does not make much sense)
        if (_shouldScopeFileSystems != null && ParentScope?._shouldScopeFileSystems != _shouldScopeFileSystems)
        {
            throw new ArgumentException(
                $"Value '{_shouldScopeFileSystems.Value}' be different from parent value '{ParentScope?._shouldScopeFileSystems}'.",
                nameof(_shouldScopeFileSystems));
        }

        // cannot specify a different mode!
        // TODO: means that it's OK to go from L2 to None for reading purposes, but writing would be BAD!
        // this is for XmlStore that wants to bypass caches when rebuilding XML (same for NuCache)
        if (repositoryCacheMode != RepositoryCacheMode.Unspecified &&
            parentScope.RepositoryCacheMode > repositoryCacheMode)
        {
            throw new ArgumentException(
                $"Value '{repositoryCacheMode}' cannot be lower than parent value '{parentScope.RepositoryCacheMode}'.", nameof(repositoryCacheMode));
        }

        // Only the outermost scope can specify the notification publisher
        if (_notificationPublisher != null)
        {
            throw new ArgumentException("Value cannot be specified on nested scope.", nameof(_notificationPublisher));
        }


        ParentScope = parentScope;
    }

    private CoreScope? ParentScope { get; }

    public int Depth
    {
        get
        {
            if (ParentScope == null)
            {
                return 0;
            }

            return ParentScope.Depth + 1;
        }
    }

    public Guid InstanceId { get; }

    public int CreatedThreadId { get; }

    public ILockingMechanism Locks { get; }

    public IScopedNotificationPublisher Notifications
    {
        get
        {
            EnsureNotDisposed();
            if (ParentScope != null)
            {
                return ParentScope.Notifications;
            }

            return _notificationPublisher ??= new ScopedNotificationPublisher(_eventAggregator);
        }
    }

    public RepositoryCacheMode RepositoryCacheMode
    {
        get
        {
            if (_repositoryCacheMode != RepositoryCacheMode.Unspecified)
            {
                return _repositoryCacheMode;
            }

            return ParentScope?.RepositoryCacheMode ?? RepositoryCacheMode.Default;
        }
    }

    public IsolatedCaches IsolatedCaches
    {
        get
        {
            if (ParentScope != null)
            {
                return ParentScope.IsolatedCaches;
            }

            return _isolatedCaches ??= new IsolatedCaches(_ => new DeepCloneAppCache(new ObjectCacheAppCache()));
        }
    }

    public bool ScopedFileSystems
    {
        get
        {
            if (ParentScope != null)
            {
                return ParentScope.ScopedFileSystems;
            }

            return _scopedFileSystem != null;
        }
    }

    /// <summary>
    /// Completes a scope
    /// </summary>
    /// <returns>A value indicating whether the scope is completed or not.</returns>
    public bool Complete()
    {
        if (_completed.HasValue == false)
        {
            _completed = true;
        }

        return _completed.Value;
    }

    public void ReadLock(params int[] lockIds) => Locks.ReadLock(InstanceId, TimeSpan.Zero, lockIds);

    public void WriteLock(params int[] lockIds) => Locks.WriteLock(InstanceId, TimeSpan.Zero, lockIds);

    public void WriteLock(TimeSpan timeout, int lockId) => Locks.ReadLock(InstanceId, timeout, lockId);

    public void ReadLock(TimeSpan timeout, int lockId) => Locks.WriteLock(InstanceId, timeout, lockId);

    public void EagerWriteLock(params int[] lockIds) => Locks.EagerWriteLock(InstanceId, TimeSpan.Zero, lockIds);

    public void EagerWriteLock(TimeSpan timeout, int lockId) => Locks.EagerWriteLock(InstanceId, timeout, lockId);

    public void EagerReadLock(TimeSpan timeout, int lockId) => Locks.EagerReadLock(InstanceId, timeout, lockId);

    public void EagerReadLock(params int[] lockIds) => Locks.EagerReadLock(InstanceId, TimeSpan.Zero, lockIds);

    public virtual void Dispose()
    {
        if (ParentScope is null)
        {
            HandleScopedFileSystems();
            HandleScopedNotifications();
        }
        else
        {
            ParentScope.ChildCompleted(_completed);
        }

        // Decrement the lock counters on the parent if any.
        Locks.ClearLocks(InstanceId);

        if (ParentScope is null)
        {
            Locks.EnsureLocksCleared(InstanceId);
        }

        _disposed = true;
    }

    protected void ChildCompleted(bool? completed)
    {
        // if child did not complete we cannot complete
        if (completed.HasValue == false || completed.Value == false)
        {
            _completed = false;
        }
    }

    private void HandleScopedFileSystems()
    {
        if (_shouldScopeFileSystems == true)
        {
            if (_completed.HasValue && _completed.Value)
            {
                _scopedFileSystem?.Complete();
            }

            _scopedFileSystem?.Dispose();
            _scopedFileSystem = null;
        }
    }

    private void HandleScopedNotifications() => _notificationPublisher?.ScopeExit(_completed.HasValue && _completed.Value);

    private void EnsureNotDisposed()
    {
        // We can't be disposed
        if (_disposed)
        {
            throw new ObjectDisposedException($"The {nameof(CoreScope)} with ID ({InstanceId}) is already disposed");
        }

        // And neither can our ancestors if we're trying to be disposed since
        // a child must always be disposed before it's parent.
        // This is a safety check, it's actually not entirely possible that a parent can be
        // disposed before the child since that will end up with a "not the Ambient" exception.
        ParentScope?.EnsureNotDisposed();
    }

    private ILockingMechanism ResolveLockingMechanism() =>
        ParentScope is not null ? ParentScope.ResolveLockingMechanism() : Locks;
}
