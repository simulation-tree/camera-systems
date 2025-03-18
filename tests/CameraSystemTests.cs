using Cameras.Systems;
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
            TypeRegistry.Load<TransformsTypeBank>();
            TypeRegistry.Load<RenderingTypeBank>();
            TypeRegistry.Load<CamerasTypeBank>();
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
            simulator.AddSystem(new TransformSystem());
            simulator.AddSystem(new CameraSystem());
        }
    }
}