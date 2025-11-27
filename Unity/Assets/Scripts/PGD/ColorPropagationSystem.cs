using UnityEngine;
using UnityEngine.EventSystems;

namespace PGD.Drones
{
    [DisableAutoRegister]
    public class ColorPropagationSystem : PGDSystem<PGDTransform, PGDPosition, Start, Target>
    {
        // 颜色列表
        private Color[] targetColors = 
        {
            new (0.2f, 0.8f, 0.9f, 1f),
            new (1.0f, 0.4f, 0.4f, 1f),
            new (0.2f, 0.6f, 1.0f, 1f),
            new (0.3f, 0.9f, 0.4f, 1f),
            new (1.0f, 0.85f, 0.2f, 1f)
        };
        
        // 当前颜色索引
        private int currentColorIndex;
        
        // 获取当前颜色的属性
        private Color CurrentTargetColor => targetColors[currentColorIndex];
        
        // 正在传播的颜色（当传播进行中时，保持不变）
        private Color? propagatingColor;
    
        private CommandQueue cq;
        private IQuery queryColorToBeUpdated;
        private IQuery<NeighborOf> queryRelation;
        
        protected override void OnAddWorld(IECSWorld world)
        {
            QueryFilter.WithoutAnyTags(ITags.Get<Disabled>());
            cq = world.GetCommandQueue();
            cq.EnableReuse = true;
            queryColorToBeUpdated = world.Query().WithAllTags(ITags.Get<ColorToBeUpdated>());
            queryRelation = world.QueryRelation<NeighborOf>().WithAllTags(ITags.Get<ColorToBeUpdated>());
        }
        
        protected override void OnUpdate()
        {
            // Debug.Log("PGD ColorUpdateSystem running");
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
                
                Debug.Log("点击了鼠标");
                
                // 不使用 Physics.Raycast（因为 Instanced 渲染没有 Collider）
                // 使用数学方法计算射线与所有实体的最近距离
                Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
                IEntity clickedEntity = FindNearestEntityOnRay(ray);
                
                if (!clickedEntity.IsDeleted())
                {
                    Debug.Log($"选中了实体 ID: {clickedEntity.Id}，当前颜色: {CurrentTargetColor}");
                    
                    // 设置正在传播的颜色
                    propagatingColor = CurrentTargetColor;
                    
                    SelectEntity(clickedEntity);
                    
                    // 切换到下一个颜色（轮询）
                    currentColorIndex = (currentColorIndex + 1) % targetColors.Length;
                }
                else
                {
                    Debug.Log("没有找到实体");
                }
            }
            
            // 当有颜色正在传播时，继续更新邻居颜色
            if (propagatingColor.HasValue && !queryColorToBeUpdated.IsEmpty())
            {
                updateNeighborColors(propagatingColor.Value);
                Debug.Log($"剩余{queryColorToBeUpdated.EntityCount}个方块待染色，传播颜色: {propagatingColor.Value}");
            }
            else if (propagatingColor.HasValue && queryColorToBeUpdated.IsEmpty())
            {
                // 传播完成，清除传播颜色
                Debug.Log($"颜色传播完成: {propagatingColor.Value}");
                propagatingColor = null;
            }
        }
        
        private void SelectEntity(IEntity entity)
        {
            if (!propagatingColor.HasValue)
            {
                Debug.LogError("propagatingColor 未设置！");
                return;
            }
            
            Color colorToUse = propagatingColor.Value;

            if (entity.TryGetComponent<CubeColor>(out var cubeColor) && cubeColor.Value.Equals(colorToUse))
            {
                return;
            }
            
            // 添加/修改新的颜色组件
            entity.AddComponent(new CubeColor(colorToUse));
            
            // 给所有cube加上待更新颜色的Tag
            foreach (var cubeEntity in GetQuery().Entities)
            {
                cq.AddTag<ColorToBeUpdated>(cubeEntity.Id);
            }
            cq.Apply();
            entity.RemoveTag<ColorToBeUpdated>();
            Debug.Log($"给{GetQuery().EntityCount - 1}个实体添加了ColorToBeUpdated");

            // 该区域cube更新命中次数
            RecordHitCounts(entity);
            var directNeighborRelations = entity.GetRelations<NeighborOf>();
            foreach (var directNeighborRelation in directNeighborRelations)
            {
                RecordHitCounts(directNeighborRelation.Target);
            }
        }

        private void updateNeighborColors(Color color)
        {
            queryRelation.ForEachEntity((ref NeighborOf neighborOf, IEntity entity) =>
            {
                // 对于每个待更新颜色的方块，如果其邻居有目标颜色，则更新当前方块颜色
                ref var targetEntity = ref neighborOf.Target;
                if (targetEntity.TryGetComponent<CubeColor>(out var cubeColor) && cubeColor.Value.Equals(color))
                {
                    cq.AddComponent(entity.Id, new CubeColor(color));
                    cq.RemoveTag<ColorToBeUpdated>(entity.Id);
                }
            });
            cq.Apply();
        }
        
        private IEntity FindNearestEntityOnRay(Ray ray)
        {
            var query = GetQuery();
            IEntity nearest = default;

            Vector3 origin = ray.origin;
            Vector3 direction = ray.direction.normalized;

            const float cubeRadius = 0.6f;              // 方块半径（米），根据阵列大小可调
            const float depthBias = 1f;             // 深度权重，越靠近摄像机权重越高

            float bestScore = float.MaxValue;

            foreach (var entity in query.Entities)
            {
                ref var trans = ref entity.GetComponent<PGDTransform>();
                Vector3 entityPos = new Vector3(
                    trans.mtr.Translation.X,
                    trans.mtr.Translation.Y,
                    trans.mtr.Translation.Z
                );

                Vector3 toPoint = entityPos - origin; // 实体中心相对射线起点的向量
                float alongRay = Vector3.Dot(toPoint, direction); // 实体中心在射线方向上的投影距离
                if (alongRay < 0f) continue; // 在相机后方

                Vector3 closestPoint = origin + direction * alongRay; // 射线上距离实体最近的点
                float perpDistance = Vector3.Distance(closestPoint, entityPos); // 实体中心到射线上最近点的距离

                // 根据深度扩展容差，越远的实体允许稍大的偏差
                float dynamicRadius = cubeRadius + alongRay * 0.01f;
                if (perpDistance > dynamicRadius) continue;

                float score = perpDistance + alongRay * depthBias; // 优先挑选离镜头近的实体
                if (score < bestScore)
                {
                    bestScore = score;
                    nearest = entity;
                }
            }

            return nearest;
        }
        
        private void RecordHitCounts(IEntity entity)
        {
            if (!entity.HasComponent<HitCounter>())
            {
                entity.AddComponent(new HitCounter{ counts = 1 });
            }
            else
            {
                entity.AddComponent(new HitCounter { counts = entity.GetComponent<HitCounter>().counts + 1 });
            }
        }
    }
}