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

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                systemContainer.Write(new CameraSystem());
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            ComponentQuery<CameraSettings> camerasWithoutMatricesQuery = new(world);
            camerasWithoutMatricesQuery.ExcludeComponent<CameraMatrices>();
            foreach (var r in camerasWithoutMatricesQuery)
            {
                operation.SelectEntity(r.entity);
            }

            if (operation.Count > 0)
            {
                operation.AddComponent<CameraMatrices>(world.Schema);
                world.Perform(operation);
                operation.Clear();
            }

            ComponentQuery<CameraSettings, CameraMatrices, IsViewport, CameraSettings> query = new(world);
            foreach (var r in query)
            {
                ref CameraSettings settings = ref r.component4;
                if (settings.orthographic)
                {
                    CalculateOrthographic(world, r);
                }
                else
                {
                    CalculatePerspective(world, r);
                }
            }
        }

        private static void CalculatePerspective(World world, Chunk.Entity<CameraSettings, CameraMatrices, IsViewport, CameraSettings> r)
        {
            uint destinationEntity = world.GetReference(r.entity, r.component3.destinationReference);
            if (destinationEntity == default || !world.ContainsEntity(destinationEntity))
            {
                return;
            }

            LocalToWorld ltw = world.GetComponent(r.entity, LocalToWorld.Default);
            (Vector3 position, Quaternion rotation, Vector3 scale) = ltw.Decomposed;
            Vector3 forward = ltw.Forward;
            Vector3 up = ltw.Up;
            Vector3 target = position + forward;
            Matrix4x4 view = Matrix4x4.CreateLookAt(position, target, up);
            Matrix4x4 projection = Matrix4x4.Identity;
            ref CameraSettings fov = ref r.component4;
            float aspect = world.GetComponent<IsDestination>(destinationEntity).AspectRatio;
            (float min, float max) = r.component1.Depth;
            projection = Matrix4x4.CreatePerspectiveFieldOfView(fov.size, aspect, min + 0.1f, max);
            projection.M43 += 0.1f;
            projection.M11 *= -1; //flip x axis
            r.component2 = new(projection, view);
        }

        private static void CalculateOrthographic(World world, Chunk.Entity<CameraSettings, CameraMatrices, IsViewport, CameraSettings> r)
        {
            uint destinationEntity = world.GetReference(r.entity, r.component3.destinationReference);
            if (destinationEntity == default || !world.ContainsEntity(destinationEntity))
            {
                return;
            }

            LocalToWorld ltw = world.GetComponent(r.entity, LocalToWorld.Default);
            (Vector3 position, Quaternion rotation, Vector3 scale) = ltw.Decomposed;
            Vector3 forward = ltw.Forward;
            Vector3 up = ltw.Up;
            Vector3 target = position + forward;
            Matrix4x4 view = Matrix4x4.CreateLookAt(position, target, up);
            Matrix4x4 projection = Matrix4x4.Identity;
            ref CameraSettings orthographicSize = ref r.component4;
            Vector2 size = world.GetComponent<IsDestination>(destinationEntity).SizeAsVector2();
            (float min, float max) = r.component1.Depth;
            projection = Matrix4x4.CreateOrthographicOffCenter(0, orthographicSize.size * size.X, 0, orthographicSize.size * size.Y, min + 0.1f, max);
            projection.M43 += 0.1f;
            view = Matrix4x4.CreateTranslation(-position);
            r.component2 = new(projection, view);
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
