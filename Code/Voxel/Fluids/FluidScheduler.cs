using System;
using System.Collections.Generic;

namespace WildEarth.Voxel
{
    public sealed class FluidScheduler
    {
        private readonly FluidUpdateSystem updateSystem;
        private readonly FluidSimulationSettings settings;

        private readonly Queue<FluidPendingUpdate> pendingUpdates;
        private readonly HashSet<FluidUpdateKey> pendingKeys;

        private readonly Queue<FluidPendingUpdate> deferredUpdates;
        private readonly Dictionary<
            ChunkCoordinate,
            HashSet<FluidUpdateKey>
        > deferredKeys;

        private readonly HashSet<ChunkCoordinate> activeChunks;

        private readonly Queue<ChunkCoordinate> changedChunks;
        private readonly HashSet<ChunkCoordinate> changedChunkKeys;

        // Reutilizados para evitar allocations temporales.
        private readonly Queue<FluidPendingUpdate> reusableUpdateQueue;

        private float tickAccumulator;

        public int PendingCount =>
            pendingUpdates.Count;

        public int DeferredCount =>
            deferredUpdates.Count;

        public int ActiveChunkCount =>
            activeChunks.Count;

        public bool HasPendingUpdates =>
            pendingUpdates.Count > 0;

        public bool HasDeferredUpdates =>
            deferredUpdates.Count > 0;

        public bool HasPendingOrDeferredUpdates =>
            pendingUpdates.Count > 0 ||
            deferredUpdates.Count > 0;

        public FluidScheduler(
            FluidUpdateSystem updateSystem,
            FluidSimulationSettings settings)
        {
            this.updateSystem =
                updateSystem ??
                throw new ArgumentNullException(
                    nameof(updateSystem)
                );

            this.settings = settings;

            pendingUpdates =
                new Queue<FluidPendingUpdate>();

            pendingKeys =
                new HashSet<FluidUpdateKey>();

            deferredUpdates =
                new Queue<FluidPendingUpdate>();

            deferredKeys =
                new Dictionary<
                    ChunkCoordinate,
                    HashSet<FluidUpdateKey>
                >();

            activeChunks =
                new HashSet<ChunkCoordinate>();

            changedChunks =
                new Queue<ChunkCoordinate>();

            changedChunkKeys =
                new HashSet<ChunkCoordinate>();

            reusableUpdateQueue =
                new Queue<FluidPendingUpdate>();

            tickAccumulator = 0f;
        }

        public bool Enqueue(
            FluidPendingUpdate update)
        {
            if (!update.IsValid)
                return false;

            if (update.Distance >
                settings.MaxPropagationDistance)
            {
                return false;
            }

            FluidUpdateKey key =
                new FluidUpdateKey(update);

            if (!pendingKeys.Add(key))
                return false;

            pendingUpdates.Enqueue(update);

            activeChunks.Add(
                update.Chunk
            );

            return true;
        }

        public int EnqueueRange(
            IEnumerable<FluidPendingUpdate> updates)
        {
            if (updates == null)
            {
                throw new ArgumentNullException(
                    nameof(updates)
                );
            }

            int added = 0;

            foreach (
                FluidPendingUpdate update
                in updates)
            {
                if (Enqueue(update))
                    added++;
            }

            return added;
        }

        public bool RemoveNext(
            out FluidPendingUpdate update)
        {
            if (pendingUpdates.Count == 0)
            {
                update = default;
                return false;
            }

            update =
                pendingUpdates.Dequeue();

            pendingKeys.Remove(
                new FluidUpdateKey(update)
            );

            return true;
        }

        public int ProcessTick()
        {
            int budget =
                Math.Max(
                    settings.MaxUpdatesPerTick,
                    1
                );

            int processed = 0;

            while (
                processed < budget &&
                RemoveNext(
                    out FluidPendingUpdate update
                ))
            {
                ProcessUpdate(update);
                processed++;
            }

            updateSystem.FlushDirtyChunks();

            CleanupInactiveChunks();

            return processed;
        }

        public int Advance(
            float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaTime)
                );
            }

            int processed = 0;

            float tickInterval =
                1f /
                Math.Max(
                    settings.TicksPerSecond,
                    1
                );

            tickAccumulator += deltaTime;

            while (
                tickAccumulator >= tickInterval)
            {
                tickAccumulator -= tickInterval;

                processed += ProcessTick();

                if (pendingUpdates.Count == 0)
                    break;
            }

            return processed;
        }

        public void ActivateChunk(
            ChunkCoordinate coordinate)
        {
            activeChunks.Add(
                coordinate
            );
        }

        public bool IsChunkActive(
            ChunkCoordinate coordinate)
        {
            return activeChunks.Contains(
                coordinate
            );
        }

        public bool HasPendingUpdate(
            FluidPendingUpdate update)
        {
            return pendingKeys.Contains(
                new FluidUpdateKey(update)
            );
        }

        public bool HasDeferredUpdate(
            FluidPendingUpdate update)
        {
            if (!deferredKeys.TryGetValue(
                    update.Chunk,
                    out HashSet<FluidUpdateKey> keys))
            {
                return false;
            }

            return keys.Contains(
                new FluidUpdateKey(update)
            );
        }

        public void NotifyChunkLoaded(
            ChunkCoordinate coordinate)
        {
            if (deferredUpdates.Count == 0)
                return;

            reusableUpdateQueue.Clear();

            while (
                deferredUpdates.Count > 0)
            {
                FluidPendingUpdate update =
                    deferredUpdates.Dequeue();

                if (update.Chunk == coordinate)
                {
                    RemoveDeferredKey(update);

                    Enqueue(update);
                    continue;
                }

                reusableUpdateQueue.Enqueue(update);
            }

            while (
                reusableUpdateQueue.Count > 0)
            {
                deferredUpdates.Enqueue(
                    reusableUpdateQueue.Dequeue()
                );
            }
        }

        public bool TryConsumeChangedChunk(
            out ChunkCoordinate coordinate)
        {
            if (changedChunks.Count == 0)
            {
                coordinate = default;
                return false;
            }

            coordinate =
                changedChunks.Dequeue();

            changedChunkKeys.Remove(
                coordinate
            );

            return true;
        }

        public void Clear()
        {
            pendingUpdates.Clear();
            pendingKeys.Clear();

            deferredUpdates.Clear();
            deferredKeys.Clear();

            activeChunks.Clear();

            changedChunks.Clear();
            changedChunkKeys.Clear();

            reusableUpdateQueue.Clear();

            tickAccumulator = 0f;
        }

        public void ClearChunk(
            ChunkCoordinate coordinate)
        {
            ClearPendingChunk(coordinate);
            ClearDeferredChunk(coordinate);

            activeChunks.Remove(
                coordinate
            );
        }

        private void ProcessUpdate(
            FluidPendingUpdate update)
        {
            bool applied =
                updateSystem.TryApply(
                    update.ToChange(),
                    out FluidChangeResult result
                );

            if (!result.TargetChunkLoaded)
            {
                DeferUpdate(update);
                return;
            }

            if (!result.Applied)
                return;

            ActivateChunk(
                update.Chunk
            );

            ActivateChunk(
                result.Change.TargetChunk
            );

            if (changedChunkKeys.Add(update.Chunk))
            {
                changedChunks.Enqueue(
                    update.Chunk
                );
            }

            if (changedChunkKeys.Add(
                    result.Change.TargetChunk))
            {
                changedChunks.Enqueue(
                    result.Change.TargetChunk
                );
            }
        }

        private void DeferUpdate(
            FluidPendingUpdate update)
        {
            FluidUpdateKey key =
                new FluidUpdateKey(update);

            if (!TryAddDeferredKey(
                    update.Chunk,
                    key))
            {
                return;
            }

            deferredUpdates.Enqueue(
                update
            );
        }

        private bool TryAddDeferredKey(
            ChunkCoordinate coordinate,
            FluidUpdateKey key)
        {
            if (!deferredKeys.TryGetValue(
                    coordinate,
                    out HashSet<FluidUpdateKey> keys))
            {
                keys =
                    new HashSet<FluidUpdateKey>();

                deferredKeys.Add(
                    coordinate,
                    keys
                );
            }

            return keys.Add(key);
        }

        private void RemoveDeferredKey(
            FluidPendingUpdate update)
        {
            ChunkCoordinate coordinate =
                update.Chunk;

            if (!deferredKeys.TryGetValue(
                    coordinate,
                    out HashSet<FluidUpdateKey> keys))
            {
                return;
            }

            keys.Remove(
                new FluidUpdateKey(update)
            );

            if (keys.Count == 0)
            {
                deferredKeys.Remove(
                    coordinate
                );
            }
        }

        private void ClearPendingChunk(
            ChunkCoordinate coordinate)
        {
            if (pendingUpdates.Count == 0)
                return;

            reusableUpdateQueue.Clear();

            pendingKeys.Clear();

            while (
                pendingUpdates.Count > 0)
            {
                FluidPendingUpdate update =
                    pendingUpdates.Dequeue();

                if (update.Chunk == coordinate)
                    continue;

                reusableUpdateQueue.Enqueue(update);

                pendingKeys.Add(
                    new FluidUpdateKey(update)
                );
            }

            while (
                reusableUpdateQueue.Count > 0)
            {
                pendingUpdates.Enqueue(
                    reusableUpdateQueue.Dequeue()
                );
            }
        }

        private void ClearDeferredChunk(
            ChunkCoordinate coordinate)
        {
            if (deferredUpdates.Count == 0)
            {
                deferredKeys.Remove(
                    coordinate
                );

                return;
            }

            reusableUpdateQueue.Clear();

            while (
                deferredUpdates.Count > 0)
            {
                FluidPendingUpdate update =
                    deferredUpdates.Dequeue();

                if (update.Chunk == coordinate)
                {
                    RemoveDeferredKey(update);
                    continue;
                }

                reusableUpdateQueue.Enqueue(update);
            }

            while (
                reusableUpdateQueue.Count > 0)
            {
                deferredUpdates.Enqueue(
                    reusableUpdateQueue.Dequeue()
                );
            }

            deferredKeys.Remove(
                coordinate
            );
        }

        private void CleanupInactiveChunks()
        {
            if (activeChunks.Count == 0)
                return;

            // Equivalente al comportamiento anterior,
            // pero sin crear otro HashSet temporal.
            activeChunks.Clear();

            foreach (
                FluidPendingUpdate update
                in pendingUpdates)
            {
                activeChunks.Add(
                    update.Chunk
                );
            }
        }

        private readonly struct FluidUpdateKey :
            IEquatable<FluidUpdateKey>
        {
            private readonly ChunkCoordinate chunk;
            private readonly int x;
            private readonly int y;
            private readonly int z;
            private readonly byte level;

            public FluidUpdateKey(
                FluidPendingUpdate update)
            {
                chunk = update.Chunk;
                x = update.X;
                y = update.Y;
                z = update.Z;
                level = update.State.Level;
            }

            public bool Equals(
                FluidUpdateKey other)
            {
                return chunk == other.chunk &&
                    x == other.x &&
                    y == other.y &&
                    z == other.z &&
                    level == other.level;
            }

            public override bool Equals(
                object obj)
            {
                return obj is FluidUpdateKey other &&
                       Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    chunk,
                    x,
                    y,
                    z,
                    level
                );
            }
        }
    }
}