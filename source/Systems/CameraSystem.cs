using Cameras.Components;
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
    public readonly partial struct CameraSystem : ISystem
    {
        private readonly Operation operation;
        private readonly Array<IsDestination> destinations;

        public CameraSystem()
        {
            operation = new();
            destinations = new();
        }

        public readonly void Dispose()
        {
            destinations.Dispose();
            operation.Dispose();
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            Schema schema = world.Schema;
            int settingsType = schema.GetComponentType<CameraSettings>();
            int matricesType = schema.GetComponentType<CameraMatrices>();
            int viewportType = schema.GetComponentType<IsViewport>();
            int ltwType = schema.GetComponentType<LocalToWorld>();
            int destinationType = schema.GetComponentType<IsDestination>();

            //find all destination components
            int capacity = (world.MaxEntityValue + 1).GetNextPowerOf2();
            if (destinations.Length < capacity)
            {
                destinations.Length = capacity;
            }

            destinations.Clear();

            ReadOnlySpan<Chunk> chunks = world.Chunks;
            foreach (Chunk chunk in chunks)
            {
                if (chunk.Definition.ContainsComponent(destinationType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsDestination> components = chunk.GetComponents<IsDestination>(destinationType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        destinations[entities[i]] = components[i];
                    }
                }
            }

            //ensure cameras have a matrices component
            foreach (Chunk chunk in chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(settingsType) && !definition.ContainsComponent(matricesType))
                {
                    operation.SelectEntities(chunk.Entities);
                }
            }

            if (operation.Count > 0)
            {
                operation.AddComponentType<CameraMatrices>();
                operation.Perform(world);
                operation.Reset();
            }

            chunks = world.Chunks;
            BitMask componentMask = new(settingsType, matricesType, viewportType);
            foreach (Chunk chunk in chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.componentTypes.ContainsAll(componentMask))
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
                        IsDestination destination = destinations[destinationEntity];
                        if (settings.orthographic)
                        {
                            CalculateOrthographic(world, entity, destination, ltwType, settings, ref matrices, ref viewport);
                        }
                        else
                        {
                            CalculatePerspective(world, entity, destination, ltwType, settings, ref matrices, ref viewport);
                        }
                    }
                }
            }
        }

        private static void CalculatePerspective(World world, uint entity, IsDestination destination, int ltwType, CameraSettings settings, ref CameraMatrices matrices, ref IsViewport viewport)
        {
            LocalToWorld ltw = world.GetComponentOrDefault(entity, ltwType, LocalToWorld.Default);
            Vector3 position = ltw.Position;
            Vector3 forward = ltw.Forward;
            Vector3 up = ltw.Up;
            Vector3 target = position + forward;
            Matrix4x4 view = Matrix4x4.CreateLookAt(position, target, up);
            float aspect = destination.AspectRatio;
            (float min, float max) = settings.Depth;
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(settings.size, aspect, min + 0.1f, max);
            projection.M43 += 0.1f;
            projection.M11 *= -1; //flip x axis
            matrices = new(projection, view);
        }

        private static void CalculateOrthographic(World world, uint entity, IsDestination destination, int ltwType, CameraSettings settings, ref CameraMatrices matrices, ref IsViewport viewport)
        {
            LocalToWorld ltw = world.GetComponentOrDefault(entity, ltwType, LocalToWorld.Default);
            Vector3 position = ltw.Position;
            Vector2 size = destination.GetSizeAsVector2();
            (float min, float max) = settings.Depth;
            Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(0, settings.size * size.X, 0, settings.size * size.Y, min + 0.1f, max);
            projection.M43 += 0.1f;
            Matrix4x4 view = Matrix4x4.CreateTranslation(-position);
            matrices = new(projection, view);
        }

        void ISystem.Finish(in SystemContext context, in World world)
        {
        }
    }
}