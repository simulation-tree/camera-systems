using Rendering;
using Simulation.Tests;
using Transforms;
using Transforms.Systems;
using Types;
using Worlds;

namespace Cameras.Systems.Tests
{
    public abstract class CameraSystemTests : SimulationTests
    {
        static CameraSystemTests()
        {
            MetadataRegistry.Load<TransformsMetadataBank>();
            MetadataRegistry.Load<RenderingMetadataBank>();
            MetadataRegistry.Load<CamerasMetadataBank>();
        }

        protected override Schema CreateSchema()
        {
            Schema schema = base.CreateSchema();
            schema.Load<TransformsSchemaBank>();
            schema.Load<RenderingSchemaBank>();
            schema.Load<CamerasSchemaBank>();
            return schema;
        }

        protected override void SetUp()
        {
            base.SetUp();
            Simulator.Add(new TransformSystem());
            Simulator.Add(new CameraSystem());
        }

        protected override void TearDown()
        {
            Simulator.Remove<CameraSystem>();
            Simulator.Remove<TransformSystem>();
            base.TearDown();
        }
    }
}