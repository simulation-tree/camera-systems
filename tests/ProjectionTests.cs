using Cameras.Components;
using Cameras.Systems.Tests;
using Rendering;
using System;
using System.Numerics;
using Transforms;
using Transforms.Components;
using Worlds;

namespace Cameras.Tests
{
    public class ProjectionTests : CameraSystemTests
    {
        [Test]
        public void CheckValuesOfViewMatrix()
        {
            Destination destination = new(world, new(1980, 1080), "dummy");
            Camera camera = new(world, destination, CameraSettings.CreatePerspectiveDegrees(90), 0f, 1000f);
            Transform cameraTransform = camera.Become<Transform>();

            simulator.Update(TimeSpan.FromSeconds(0.01f));

            CameraMatrices matrices = camera.Matrices;
            Assert.That(matrices.Position.X, Is.EqualTo(0).Within(0.1f));
            Assert.That(matrices.Position.Y, Is.EqualTo(0).Within(0.1f));
            Assert.That(matrices.Position.Z, Is.EqualTo(0).Within(0.1f));

            cameraTransform.LocalPosition = new(2, 1, 0);
            cameraTransform.LocalRotation = Rotation.FromDirection(Vector3.UnitZ).value;

            simulator.Update(TimeSpan.FromSeconds(0.01f));

            matrices = camera.Matrices;
            Assert.That(matrices.Position.X, Is.EqualTo(2).Within(0.1f));
            Assert.That(matrices.Position.Y, Is.EqualTo(1).Within(0.1f));
            Assert.That(matrices.Position.Z, Is.EqualTo(0).Within(0.1f));
            Assert.That(matrices.Forward.X, Is.EqualTo(0).Within(0.1f));
            Assert.That(matrices.Forward.Y, Is.EqualTo(0).Within(0.1f));
            Assert.That(matrices.Forward.Z, Is.EqualTo(1).Within(0.1f));

            cameraTransform.LocalRotation = Rotation.FromDirection(Vector3.UnitX).value;

            simulator.Update(TimeSpan.FromSeconds(0.01f));

            matrices = camera.Matrices;
            Assert.That(matrices.Position.X, Is.EqualTo(2).Within(0.1f));
            Assert.That(matrices.Position.Y, Is.EqualTo(1).Within(0.1f));
            Assert.That(matrices.Position.Z, Is.EqualTo(0).Within(0.1f));
            Assert.That(matrices.Forward.X, Is.EqualTo(1).Within(0.1f));
            Assert.That(matrices.Forward.Y, Is.EqualTo(0).Within(0.1f));
            Assert.That(matrices.Forward.Z, Is.EqualTo(0).Within(0.1f));
        }

        [Test]
        public void CheckPerspectiveRay()
        {
            Destination destination = new(world, new(1980, 1080), "dummy");
            Camera camera = new(world, destination, CameraSettings.CreatePerspectiveDegrees(90), 0f, 1000f);
            Transform cameraTransform = camera.Become<Transform>();

            simulator.Update(TimeSpan.FromSeconds(0.01f));

            CameraMatrices projection = camera.Matrices;
            (Vector3 origin, Vector3 direction) ray = projection.GetRayFromScreenPoint(new(0.5f, 0.5f));

            Assert.That(ray.origin.X, Is.EqualTo(0).Within(0.1));
            Assert.That(ray.origin.Y, Is.EqualTo(0).Within(0.1));
            Assert.That(ray.origin.Z, Is.EqualTo(0).Within(0.1));
            Assert.That(ray.direction.X, Is.EqualTo(0).Within(0.1));
            Assert.That(ray.direction.Y, Is.EqualTo(0).Within(0.1));
            Assert.That(ray.direction.Z, Is.EqualTo(1).Within(0.1));

            cameraTransform.LocalPosition = new(2, 1, 0);
            cameraTransform.LocalRotation = Rotation.FromDirection(Vector3.UnitX).value;

            simulator.Update(TimeSpan.FromSeconds(0.01f));

            projection = camera.Matrices;
            ray = projection.GetRayFromScreenPoint(new(0.5f, 0.5f));

            Assert.That(ray.origin.X, Is.EqualTo(2).Within(0.1));
            Assert.That(ray.origin.Y, Is.EqualTo(1).Within(0.1));
            Assert.That(ray.origin.Z, Is.EqualTo(0).Within(0.1));

            Assert.That(ray.direction.X, Is.EqualTo(1).Within(0.1));
            Assert.That(ray.direction.Y, Is.EqualTo(0).Within(0.1));
            Assert.That(ray.direction.Z, Is.EqualTo(0).Within(0.1));
        }
    }
}