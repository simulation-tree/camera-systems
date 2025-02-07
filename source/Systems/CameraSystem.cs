using Cameras.Components;
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

        public CameraSystem()
        {
            operation = new();
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                systemContainer.Write(new CameraSystem());
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            ComponentType settingsType = world.Schema.GetComponent<CameraSettings>();
            ComponentType matricesType = world.Schema.GetComponent<CameraMatrices>();
            ComponentType viewportType = world.Schema.GetComponent<IsViewport>();

            //ensure cameras have a matrices component
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.Contains(settingsType) && !definition.Contains(matricesType))
                {
                    USpan<uint> entities = chunk.Entities;
                    operation.SelectEntities(entities);
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
                if (definition.Contains(settingsType) && definition.Contains(matricesType) && definition.Contains(viewportType))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<CameraSettings> settingsComponents = chunk.GetComponents<CameraSettings>(settingsType);
                    USpan<CameraMatrices> matricesComponents = chunk.GetComponents<CameraMatrices>(matricesType);
                    USpan<IsViewport> viewportComponents = chunk.GetComponents<IsViewport>(viewportType);
                    for (uint i = 0; i < entities.Length; i++)
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

            LocalToWorld ltw = world.GetComponent(entity, LocalToWorld.Default);
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

            LocalToWorld ltw = world.GetComponent(entity, LocalToWorld.Default);
            Vector3 position = ltw.Position;
            Vector2 size = world.GetComponent<IsDestination>(destinationEntity).SizeAsVector2();
            (float min, float max) = settings.Depth;
            Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(0, settings.size * size.X, 0, settings.size * size.Y, min + 0.1f, max);
            projection.M43 += 0.1f;
            Matrix4x4 view = Matrix4x4.CreateTranslation(-position);
            matrices = new(projection, view);
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                operation.Dispose();
            }
        }
    }
}
