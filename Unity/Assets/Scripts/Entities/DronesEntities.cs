using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Entities.Drones
{
    public class DronesEntities
    {
        private readonly World world;
        private readonly EntityManager entityManager;
        public readonly int maxDroneCount = 255 * 1024;

        private readonly EntityQuery allDronesQuery;
        private readonly EntityQuery activeDronesQuery;

        private float duration = 500;

        private ComponentTypeHandle<DroneAnimation> animationTypeHandle;
        private ComponentTypeHandle<DroneStart> startTypeHandle;
        private ComponentTypeHandle<DroneTarget> targetTypeHandle;
        private ComponentTypeHandle<LocalTransform> transformTypeHandle;

        public DronesEntities()
        {
            world = World.DefaultGameObjectInjectionWorld;
            entityManager = world.EntityManager;
            
            allDronesQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<DroneTag>());
            activeDronesQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<DroneTag>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.Exclude<DroneDisabled>());
            
            UpdateTypeHandles();

            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<DroneUpdateSystem>();
            if (system != null)
            {
                system.Enabled = true;
            }
        }

        private void UpdateTypeHandles()
        {
            animationTypeHandle = entityManager.GetComponentTypeHandle<DroneAnimation>(false);
            startTypeHandle = entityManager.GetComponentTypeHandle<DroneStart>(true);
            targetTypeHandle = entityManager.GetComponentTypeHandle<DroneTarget>(true);
            transformTypeHandle = entityManager.GetComponentTypeHandle<LocalTransform>(false);
        }

        public void Initialize()
        {
            EntityArchetype droneArchetype = entityManager.CreateArchetype(
                typeof(DroneTag),
                typeof(DroneStart),
                typeof(DroneTarget),
                typeof(LocalTransform),
                typeof(DroneAnimation),
                typeof(DronePosition),
                typeof(CubeColor),
                typeof(DroneDisabled),
                typeof(NeighborOf));
            
            NativeArray<Entity> entities = new NativeArray<Entity>(maxDroneCount, Allocator.Temp);
            entityManager.CreateEntity(droneArchetype, entities);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                float3 zeroPos = float3.zero;
                
                entityManager.SetComponentData(entity, new DronePosition { Value = zeroPos });
                entityManager.SetComponentData(entity, new DroneStart { Value = zeroPos });
                entityManager.SetComponentData(entity, new DroneTarget { Value = zeroPos });
                entityManager.SetComponentData(entity, new DroneAnimation { Duration = 500, Elapsed = 0 });
                entityManager.SetComponentData(entity, LocalTransform.FromPosition(zeroPos));
                entityManager.SetComponentData(entity, new CubeColor(Color.gray));
                entityManager.AddBuffer<NeighborOf>(entity);
            }
            
            entities.Dispose();
        }

        public void SetEntityCount(int count)
        {
            CleanupWithinEntities();
            NativeArray<Entity> entities = allDronesQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                bool shouldBeEnabled = i < count;
                bool isDisabled  = entityManager.HasComponent<DroneDisabled>(entity);

                if (shouldBeEnabled && isDisabled )
                {
                    entityManager.RemoveComponent<DroneDisabled>(entity);
                }
                else if (!shouldBeEnabled && !isDisabled )
                {
                    entityManager.AddComponent<DroneDisabled>(entity);
                }
            }
            
            entities.Dispose();
        }

        public int GetActiveCount()
        {
            return activeDronesQuery.CalculateEntityCount();
        }

        public NativeArray<Entity> GetActiveEntities(Allocator allocator)
        {
            return activeDronesQuery.ToEntityArray(allocator);
        }

        public void SetStart(float duration)
        {
            this.duration = duration;
            CleanupWithinEntities();
        }

        public void SetTargetPlane(float duration, float distance)
        { 
            SetStart(duration);
            
            NativeArray<Entity> entities = activeDronesQuery.ToEntityArray(Allocator.Temp);
            int entityCount = entities.Length;
            
            DroneShapeSystem.SetTargetPlane(entityManager, entityCount, duration);
            
            entities.Dispose();
        }

        public void SetTargetCube(float duration, float distance)
        {
            SetStart(duration);
            
            NativeArray<Entity> entities = activeDronesQuery.ToEntityArray(Allocator.Temp);
            int entityCount = entities.Length;
            
            DroneShapeSystem.SetTargetCube(entityManager, entityCount, duration);
            
            entities.Dispose();
        }
        
        internal void SetTargetRings(float duration, int ringCnt, float distance, int ringLayers)
        {
            SetStart(duration);

            if (ringLayers <= 1)
            {
                DroneShapeSystem.SetTargetRing(entityManager, GetActiveCount(), duration);
            }
            else
            {
                DroneShapeSystem.SetTargetRings(entityManager, GetActiveCount(), duration, ringLayers);
            }
        }

        public void UpdateTransforms(float deltaTime)
        {
            UpdateTypeHandles();
        }

        public void BuildNeighborRelations()
        {
            if (NeighborManager.relationBuilt) return;

            ClearNeighborRelations();

            using var entities = activeDronesQuery.ToEntityArray(Allocator.Temp);
            using var transforms = activeDronesQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            float neighborDistanceSq = NeighborManager.neighborDistance * NeighborManager.neighborDistance;

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                DynamicBuffer<NeighborOf> buffer = entityManager.HasBuffer<NeighborOf>(entity)
                    ? entityManager.GetBuffer<NeighborOf>(entity)
                    : entityManager.AddBuffer<NeighborOf>(entity);
                buffer.Clear();
                Vector3 posA = transforms[i].Position;
                
                for (int j = 0; j < entities.Length; j++)
                {
                    if (i == j) continue;
                    Vector3 posB = transforms[j].Position;
                    float distance = Vector3.Distance(posA, posB);
                    
                    if (distance <= NeighborManager.neighborDistance)
                    {
                        buffer.Add(new NeighborOf { Target = entities[j], Distance = distance });
                    }
                }
            }
            Debug.Log($"共 {entities.Length} 个实体建立了邻居关系");
            
            if (world != null)
            {
                var colorPropagationSystem = world.GetOrCreateSystemManaged<ColorPropagationSystem>();
                if (colorPropagationSystem != null)
                {
                    colorPropagationSystem.Enabled = true; // 激活颜色传播系统
                }
            }
            
            NeighborManager.relationBuilt = true;
            NeighborManager.UpdateHotSpotButtonState();
        }

        public void ClearNeighborRelations()
        {
            using var entities = allDronesQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (entityManager.HasBuffer<NeighborOf>(entity))
                {
                    entityManager.GetBuffer<NeighborOf>(entity).Clear();
                }
            }

            NeighborManager.relationBuilt = false;
            NeighborManager.UpdateHotSpotButtonState();
        }

        public void GenerateHotspotGraph(Color defaultColor)
        {
            using var drones = activeDronesQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in drones)
            {
                entityManager.SetComponentData(entity, new CubeColor(defaultColor));
                if (entityManager.HasComponent<ColorToBeUpdated>(entity))
                {
                    entityManager.RemoveComponent<ColorToBeUpdated>(entity);
                }
            }

            var hitQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<HitCounter>());
            using var hitEntities = hitQuery.ToEntityArray(Allocator.Temp);
            using var hitData = hitQuery.ToComponentDataArray<HitCounter>(Allocator.Temp);

            for (int i = 0; i < hitEntities.Length; i++)
            {
                var entity = hitEntities[i];
                int hits = hitData[i].counts;
                Color color = hits switch
                {
                    1 => new Color(0.95f, 0.78f, 0.15f, 1f),
                    >= 2 and <= 3 => new Color(1.0f, 0.42f, 0.1f, 1f),
                    >= 4 => new Color(0.95f, 0.08f, 0.0f, 1f),
                    _ => new Color(0.25f, 0.25f, 0.25f, 1f)
                };
                entityManager.SetComponentData(entity, new CubeColor(color));
            }
        }
        
        public void ClearAllColors()
        {
            using var entities = allDronesQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (entityManager.HasComponent<CubeColor>(entity))
                {
                    entityManager.RemoveComponent<CubeColor>(entity);
                }

                if (entityManager.HasComponent<ColorToBeUpdated>(entity))
                {
                    entityManager.RemoveComponent<ColorToBeUpdated>(entity);
                }
            }
        }

        public void ClearHitCounters()
        {
            var hitQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<HitCounter>());
            using var entities = hitQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                entityManager.RemoveComponent<HitCounter>(entity);
            }
        }

        public void CleanupWithinEntities()
        {
            ClearNeighborRelations(); // 清除邻居关系
            ClearAllColors(); // 清理颜色和颜色待更新标签
            ClearHitCounters(); // 清除命中计数器lookup
            
            if (world != null)
            {
                var colorPropagationSystem = world.GetOrCreateSystemManaged<ColorPropagationSystem>();
                if (colorPropagationSystem != null)
                {
                    colorPropagationSystem.Enabled = false; // 禁用颜色传播系统
                }
            }
        }

        public void CleanupOnSwitchImpl()
        {
            CleanupWithinEntities();
            entityManager.DestroyEntity(allDronesQuery);

            if (world != null)
            {
                if (activeDronesQuery != null) activeDronesQuery.Dispose();
                if (allDronesQuery != null) allDronesQuery.Dispose();
                
                var droneUpdateSystem = world.GetOrCreateSystemManaged<DroneUpdateSystem>();
                if (droneUpdateSystem != null)
                {
                    droneUpdateSystem.Enabled = false;
                }
            }
        }
    }
}