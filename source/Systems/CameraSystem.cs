using Cameras.Components;
using Cameras.Messages;
using Collections.Generic;
using Rendering.Components;
using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using Unmanaged;
using Worlds;

namespace Cameras.Systems
{
    public sealed partial class CameraSystem : SystemBase, IListener<CameraUpdate>
    {
        private readonly World world;
        private readonly Operation operation;
        private readonly Array<IsDestination> destinations;
        private readonly int settingsType;
        private readonly int matricesType;
        private readonly int viewportType;
        private readonly int ltwType;
        private readonly int destinationType;
        private readonly BitMask cameraComponents;

        public CameraSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            operation = new(world);
            destinations = new();

            Schema schema = world.Schema;
            settingsType = schema.GetComponentType<CameraSettings>();
            matricesType = schema.GetComponentType<CameraMatrices>();
            viewportType = schema.GetComponentType<IsViewport>();
            ltwType = schema.GetComponentType<LocalToWorld>();
            destinationType = schema.GetComponentType<IsDestination>();
            cameraComponents = new(settingsType, matricesType, viewportType);
        }

        public override void Dispose()
        {
            destinations.Dispose();
            operation.Dispose();
        }

        void IListener<CameraUpdate>.Receive(ref CameraUpdate message)
        {
            //find all destination components
            int capacity = (world.MaxEntityValue + 1).GetNextPowerOf2();
            if (destinations.Length < capacity)
            {
                destinations.Length = capacity;
            }

            destinations.Clear();
            Span<IsDestination> destinationsSpan = destinations.AsSpan();

            //collect destinations and make sure cameras have the matrix component
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                if (chunk.Count > 0)
                {
                    Definition definition = chunk.Definition;
                    if (definition.ContainsComponent(destinationType))
                    {
                        ReadOnlySpan<uint> entities = chunk.Entities;
                        ComponentEnumerator<IsDestination> components = chunk.GetComponents<IsDestination>(destinationType);
                        for (int i = 0; i < entities.Length; i++)
                        {
                            destinationsSpan[(int)entities[i]] = components[i];
                        }
                    }

                    if (definition.ContainsComponent(settingsType) && !definition.ContainsComponent(matricesType))
                    {
                        operation.AppendMultipleEntitiesToSelection(chunk.Entities);
                    }
                }
            }

            if (operation.Count > 0)
            {
                operation.AddComponentType(matricesType);
                operation.Perform();
                operation.Reset();
            }

            chunks = world.Chunks;
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                if (chunk.Definition.componentTypes.ContainsAll(cameraComponents))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<CameraSettings> settingsComponents = chunk.GetComponents<CameraSettings>(settingsType);
                    ComponentEnumerator<CameraMatrices> matricesComponents = chunk.GetComponents<CameraMatrices>(matricesType);
                    ComponentEnumerator<IsViewport> viewportComponents = chunk.GetComponents<IsViewport>(viewportType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        uint entity = entities[i];
                        ref IsViewport viewport = ref viewportComponents[i];
                        uint destinationEntity = world.GetReference(entity, viewport.destinationReference);
                        if (destinationEntity == default || !world.ContainsEntity(destinationEntity))
                        {
                            return;
                        }

                        CameraSettings settings = settingsComponents[i];
                        ref CameraMatrices matrices = ref matricesComponents[i];
                        IsDestination destination = destinationsSpan[(int)destinationEntity];
                        if (settings.orthographic)
                        {
                            CalculateOrthographic(entity, destination, settings, ref matrices);
                        }
                        else
                        {
                            CalculatePerspective(entity, destination, settings, ref matrices);
                        }
                    }
                }
            }
        }

        private void CalculatePerspective(uint entity, IsDestination destination, CameraSettings settings, ref CameraMatrices matrices)
        {
            LocalToWorld ltw = world.GetComponentOrDefault(entity, ltwType, LocalToWorld.Default);
            Vector3 position = ltw.Position;
            Vector3 forward = ltw.Forward;
            Vector3 up = ltw.Up;
            Vector3 target = position + forward;
            Matrix4x4 view = Matrix4x4.CreateLookAt(position, target, up);
            float aspect = destination.width / (float)destination.height;
            (float min, float max) = settings.Depth;
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(settings.size, aspect, min + 0.1f, max);
            projection.M43 += 0.1f;
            projection.M11 *= -1; //flip x axis
            matrices = new(projection, view);
        }

        private void CalculateOrthographic(uint entity, IsDestination destination, CameraSettings settings, ref CameraMatrices matrices)
        {
            LocalToWorld ltw = world.GetComponentOrDefault(entity, ltwType, LocalToWorld.Default);
            Vector3 position = ltw.Position;
            Vector2 size = new(destination.width, destination.height);
            (float min, float max) = settings.Depth;
            Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(0, settings.size * size.X, 0, settings.size * size.Y, min + 0.1f, max);
            projection.M43 += 0.1f;
            Matrix4x4 view = Matrix4x4.CreateTranslation(-position);
            matrices = new(projection, view);
        }
    }
}