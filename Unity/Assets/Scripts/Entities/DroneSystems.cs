using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Entities.Drones
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class DroneUpdateSystem : SystemBase
    {
        private static float s_GlobalTime = 0f;
        // public static float WaveIntensity = 1.0f;
        
        protected override void OnUpdate()
        {
            s_GlobalTime += World.Time.DeltaTime;
            float deltaTime = World.Time.DeltaTime * 1000f;
            var query = SystemAPI.QueryBuilder()
                .WithAll<DroneTag, LocalTransform, DroneStart, DroneTarget, DroneAnimation>()
                .WithNone<DroneDisabled>()
                .Build();

            var startType = GetComponentTypeHandle<DroneStart>(true);
            var targetType = GetComponentTypeHandle<DroneTarget>(true);
            var animationType = GetComponentTypeHandle<DroneAnimation>(false);
            var transfromType = GetComponentTypeHandle<LocalTransform>(false);
            
            NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            foreach (var chunk in chunks)
            {
                var starts = chunk.GetNativeArray(ref startType);
                var targets = chunk.GetNativeArray(ref targetType);
                var animations = chunk.GetNativeArray(ref animationType);
                var transfroms = chunk.GetNativeArray(ref transfromType);
                
                var count = chunk.Count;
                for (int i = 0; i < count; i++)
                {
                    var animation = animations[i];
                    var target = targets[i];
                    var start = starts[i];
                    var transfrom = transfroms[i];
                    
                    animation.Elapsed += deltaTime;
                    
                    float t = math.saturate(animation.Elapsed / animation.Duration); 
                    float3 basePos = math.lerp(start.Value, target.Value, t);
                    
                    transfrom.Position = basePos;
                    animations[i] = animation;
                    transfroms[i] = transfrom;
                }
                
            }
            chunks.Dispose();
        }
    }

    public static class DroneShapeSystem
    {
        public static void SetTargetPlane(EntityManager entityManager, int entityCount, float duration)
        {
            int rowCount = (int)math.sqrt(entityCount);
            float distance = 1.2f;
            float offset = distance * rowCount / 2;

            var activeQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<DroneTag>(),
                ComponentType.Exclude<DroneDisabled>());
            
            var entities = activeQuery.ToEntityArray(Allocator.Temp);
            int activeCount = math.min(entities.Length, entityCount);

            for (int i = 0; i < activeCount; i++)
            {
                Entity entity = entities[i];
                
                float3 currentPos = float3.zero;
                if (entityManager.HasComponent<LocalTransform>(entity))
                {
                    var transform = entityManager.GetComponentData<LocalTransform>(entity);
                    currentPos = transform.Position;
                }

                int x = i % rowCount;
                int z = i / rowCount;
                float3 targetPos = new float3(
                    distance * x - offset,
                    -distance,
                    distance * z - offset);

                entityManager.SetComponentData(entity, new DroneStart { Value = currentPos });
                entityManager.SetComponentData(entity, new DroneTarget { Value = targetPos });
                entityManager.SetComponentData(entity, new DroneAnimation
                {
                    Duration = duration,
                    Elapsed = 0
                });
            }

            entities.Dispose();
        }

        public static void SetTargetCube(EntityManager entityManager, int entityCount, float duration)
        {
            
        }

        public static void SetTargetRing(EntityManager entityManager, int entityCount, float duration)
        {
            
        }
        
        public static void SetTargetRings(EntityManager entityManager, int entityCount, float duration, int ringLayers)
        {
            
        }
    }
}