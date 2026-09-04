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
        private readonly HashSet<ChunkCoordinate> activeChunks;

        private float tickAccumulator;

        public int PendingCount =>
            pendingUpdates.Count;

        public int ActiveChunkCount =>
            activeChunks.Count;

        public bool HasPendingUpdates =>
            pendingUpdates.Count > 0;

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

            activeChunks =
                new HashSet<ChunkCoordinate>();

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

        public void Clear()
        {
            pendingUpdates.Clear();
            pendingKeys.Clear();
            activeChunks.Clear();

            tickAccumulator = 0f;
        }

        public void ClearChunk(
            ChunkCoordinate coordinate)
        {
            if (pendingUpdates.Count == 0)
            {
                activeChunks.Remove(
                    coordinate
                );

                return;
            }

            Queue<FluidPendingUpdate> remaining =
                new Queue<FluidPendingUpdate>(
                    pendingUpdates.Count
                );

            pendingKeys.Clear();

            while (
                pendingUpdates.Count > 0)
            {
                FluidPendingUpdate update =
                    pendingUpdates.Dequeue();

                if (update.Chunk == coordinate)
                    continue;

                remaining.Enqueue(update);

                pendingKeys.Add(
                    new FluidUpdateKey(update)
                );
            }

            while (
                remaining.Count > 0)
            {
                pendingUpdates.Enqueue(
                    remaining.Dequeue()
                );
            }

            activeChunks.Remove(
                coordinate
            );
        }

        private void ProcessUpdate(
            FluidPendingUpdate update)
        {
            updateSystem.TryApply(
                update.ToChange(),
                out FluidChangeResult result
            );

            if (!result.Applied)
                return;

            ActivateChunk(
                update.Chunk
            );

            ActivateChunk(
                result.Change.TargetChunk
            );
        }

        private void CleanupInactiveChunks()
        {
            if (activeChunks.Count == 0)
                return;

            HashSet<ChunkCoordinate> stillActive =
                new HashSet<ChunkCoordinate>();

            foreach (
                FluidPendingUpdate update
                in pendingUpdates)
            {
                stillActive.Add(
                    update.Chunk
                );
            }

            activeChunks.Clear();

            foreach (
                ChunkCoordinate coordinate
                in stillActive)
            {
                activeChunks.Add(
                    coordinate
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

            public FluidUpdateKey(
                FluidPendingUpdate update)
            {
                chunk = update.Chunk;

                x = update.X;
                y = update.Y;
                z = update.Z;
            }

            public bool Equals(
                FluidUpdateKey other)
            {
                return chunk == other.chunk &&
                       x == other.x &&
                       y == other.y &&
                       z == other.z;
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
                    z
                );
            }
        }
    }
}