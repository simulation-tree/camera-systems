using Cameras.Components;
using Rendering.Components;
using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using Worlds;

namespace Cameras.Systems
{
    public readonly partial struct CameraSystem : ISystem
    {
        private readonly Operation operation;

        public CameraSystem()
        {
            operation = new();
        }

        public readonly void Dispose()
        {
            operation.Dispose();
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            int settingsType = world.Schema.GetComponentType<CameraSettings>();
            int matricesType = world.Schema.GetComponentType<CameraMatrices>();
            int viewportType = world.Schema.GetComponentType<IsViewport>();

            //ensure cameras have a matrices component
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(settingsType) && !definition.ContainsComponent(matricesType))
                {
                    operation.SelectEntities(chunk.Entities);
                }
            }

            if (operation.Count > 0)
            {
                operation.AddComponent<CameraMatrices>();
                operation.Perform(world);
                operation.Clear();
            }

            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(settingsType) && definition.ContainsComponent(matricesType) && definition.ContainsComponent(viewportType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<CameraSettings> settingsComponents = chunk.GetComponents<CameraSettings>(settingsType);
                    ComponentEnumerator<CameraMatrices> matricesComponents = chunk.GetComponents<CameraMatrices>(matricesType);
                    ComponentEnumerator<IsViewport> viewportComponents = chunk.GetComponents<IsViewport>(viewportType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref CameraSettings settings = ref settingsComponents[i];
                        ref CameraMatrices matrices = ref matricesComponents[i];
                        ref IsViewport viewport = ref viewportComponents[i];
                        uint entity = entities[i];
                        if (settings.orthographic)
                        {
                            CalculateOrthographic(world, entity, ref settings, ref matrices, ref viewport);
                        }
                        else
                        {
                            CalculatePerspective(world, entity, ref settings, ref matrices, ref viewport);
                        }
                    }
                }
            }
        }

        private static void CalculatePerspective(World world, uint entity, ref CameraSettings settings, ref CameraMatrices matrices, ref IsViewport viewport)
        {
            uint destinationEntity = world.GetReference(entity, viewport.destinationReference);
            if (destinationEntity == default || !world.ContainsEntity(destinationEntity))
            {
                return;
            }

            LocalToWorld ltw = world.GetComponentOrDefault(entity, LocalToWorld.Default);
            Vector3 position = ltw.Position;
            Vector3 forward = ltw.Forward;
            Vector3 up = ltw.Up;
            Vector3 target = position + forward;
            Matrix4x4 view = Matrix4x4.CreateLookAt(position, target, up);
            float aspect = world.GetComponent<IsDestination>(destinationEntity).AspectRatio;
            (float min, float max) = settings.Depth;
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(settings.size, aspect, min + 0.1f, max);
            projection.M43 += 0.1f;
            projection.M11 *= -1; //flip x axis
            matrices = new(projection, view);
        }

        private static void CalculateOrthographic(World world, uint entity, ref CameraSettings settings, ref CameraMatrices matrices, ref IsViewport viewport)
        {
            uint destinationEntity = world.GetReference(entity, viewport.destinationReference);
            if (destinationEntity == default || !world.ContainsEntity(destinationEntity))
            {
                return;
            }

            LocalToWorld ltw = world.GetComponentOrDefault(entity, LocalToWorld.Default);
            Vector3 position = ltw.Position;
            Vector2 size = world.GetComponent<IsDestination>(destinationEntity).SizeAsVector2();
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