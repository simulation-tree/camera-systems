using Cameras.Messages;
using Rendering;
using Simulation.Tests;
using Transforms;
using Transforms.Messages;
using Transforms.Systems;
using Types;
using Worlds;

namespace Cameras.Systems.Tests
{
    public abstract class CameraSystemTests : SimulationTests
    {
        public World world;

        static CameraSystemTests()
        {
            MetadataRegistry.Load<TransformsMetadataBank>();
            MetadataRegistry.Load<RenderingMetadataBank>();
            MetadataRegistry.Load<CamerasMetadataBank>();
        }

        protected override void SetUp()
        {
            base.SetUp();
            Schema schema = new();
            schema.Load<TransformsSchemaBank>();
            schema.Load<RenderingSchemaBank>();
            schema.Load<CamerasSchemaBank>();
            world = new(schema);
            Simulator.Add(new TransformSystem(Simulator, world));
            Simulator.Add(new CameraSystem(Simulator, world));
        }

        protected override void TearDown()
        {
            Simulator.Remove<CameraSystem>();
            Simulator.Remove<TransformSystem>();
            world.Dispose();
            base.TearDown();
        }

        protected override void Update(double deltaTime)
        {
            Simulator.Broadcast(new TransformUpdate());
            Simulator.Broadcast(new CameraUpdate());
        }
    }
}