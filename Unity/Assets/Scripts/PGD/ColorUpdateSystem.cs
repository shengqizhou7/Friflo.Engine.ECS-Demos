using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PGD.Drones
{
    [DisableAutoRegister]
    public class ColorUpdateSystem : PGDSystem<PGDTransform, PGDPosition, Start, Target>
    {
        // public float neighborDistance = 0.012f;
        
        // 颜色列表
        public Color[] targetColors = new Color[]
        {
            new Color(0.2f, 0.8f, 0.9f, 1f),     // 青蓝色 - 清新明亮
            new Color(1.0f, 0.4f, 0.4f, 1f),    // 鲜红色 - 高对比
            new Color(0.2f, 0.6f, 1.0f, 1f),    // 亮蓝色 - 鲜明清晰
            new Color(0.3f, 0.9f, 0.4f, 1f),    // 翠绿色 - 自然鲜明
            new Color(1.0f, 0.85f, 0.2f, 1f),   // 金黄色 - 温暖明亮
        };
        
        // 当前颜色索引
        private int currentColorIndex = 0;
        
        // 获取当前颜色的属性
        private Color CurrentTargetColor => targetColors[currentColorIndex];
        
        // 正在传播的颜色（当传播进行中时，保持不变）
        private Color? propagatingColor = null;
    
        private IECSWorld world = PGDGameContext.GetWorld();
        private CommandQueue cq;
        private IQuery queryColorToBeUpdated;
        
        protected override void OnAddWorld(IECSWorld world)
        {
            QueryFilter.WithoutAnyTags(ITags.Get<Disabled>());
            cq = world.GetCommandQueue();
            cq.EnableReuse = true;
            queryColorToBeUpdated = world.Query().WithAllComponents(IComponents.Get<PGDTransform, PGDPosition, Start, Target>()).WithoutAnyTags(ITags.Get<Disabled>()).WithAllTags(ITags.Get<ColorToBeUpdated>());
        }
        
        protected override void OnUpdate()
        {
            // Debug.Log("ColorUpdateSystem update");
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
                
                Debug.Log("🖱️ 点击了鼠标");
                
                // 不使用 Physics.Raycast（因为 Instanced 渲染没有 Collider）
                // 改用数学方法：计算射线与所有实体的最近距离
                Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
                IEntity clickedEntity = FindNearestEntityOnRay(ray);
                
                if (clickedEntity != null && !clickedEntity.IsDeleted())
                {
                    Debug.Log($"✅ 选中了实体 ID: {clickedEntity.Id}，当前颜色: {CurrentTargetColor}");
                    
                    // 设置正在传播的颜色
                    propagatingColor = CurrentTargetColor;
                    
                    SelectEntity(clickedEntity);
                    
                    // 切换到下一个颜色（轮询）
                    currentColorIndex = (currentColorIndex + 1) % targetColors.Length;
                }
                else
                {
                    Debug.Log("❌ 没有找到实体");
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
                Debug.Log($"✅ 颜色传播完成: {propagatingColor.Value}");
                propagatingColor = null;
            }
        }
        
        // // 建立邻居关系
        // public void BuildNeighborRelations()
        // {
        //     var query = GetQuery();
        //
        //     foreach (var entityA in query.Entities)
        //     {
        //         foreach (var entityB in query.Entities)
        //         {
        //             if (entityA.Id == entityB.Id) continue;
        //
        //             ref var transA = ref entityA.GetComponent<PGDTransform>();
        //             ref var transB = ref entityB.GetComponent<PGDTransform>();
        //
        //             // 提取位置
        //             Vector3 posA = new Vector3(transA.mtr.Translation.X, transA.mtr.Translation.Y,
        //                 transA.mtr.Translation.Z);
        //             Vector3 posB = new Vector3(transB.mtr.Translation.X, transB.mtr.Translation.Y,
        //                 transB.mtr.Translation.Z);
        //
        //             float distance = Vector3.Distance(posA, posB);
        //
        //             if (distance <= neighborDistance)
        //             {
        //                 entityA.AddRelation(new NeighborOf { Target = entityB, Distance = distance }, out _);
        //             }
        //         }
        //     }
        //     Debug.Log($"共 {query.Entities.Count} 个实体建立了邻居关系");
        // }
        
        public void SelectEntity(IEntity entity)
        {
            if (!propagatingColor.HasValue)
            {
                Debug.LogError("⚠️ propagatingColor 未设置！");
                return;
            }
            
            Color colorToUse = propagatingColor.Value;
            
            // // 高亮选中的实体
            // if (entity.HasComponent<CubeColor>())
            // {
            //     // 如果已有颜色组件，修改它
            //     ref var color = ref entity.GetComponent<CubeColor>();
            //     // color.Value = new Vector4(selectedColor.r, selectedColor.g, selectedColor.b, selectedColor.a);
            //     color.Value = selectedColor;
            //     Debug.Log($"✏️ 修改了现有颜色组件");
            // }
            // else
            // {
            //     // 添加新的颜色组件
            //     entity.AddComponent(new CubeColor(targetColor));
            //     Debug.Log($"➕ 添加了新的颜色组件");
            // }

            if (entity.TryGetComponent<CubeColor>(out var cubeColor) && cubeColor.Value.Equals(colorToUse))
            {
                return;
            }
            
            // 添加/修改新的颜色组件
            entity.AddComponent(new CubeColor(colorToUse));
            // entity.AddComponent(new CubeColor(Color.blue));
            Debug.Log($"➕ 更新了新的颜色组件");
            
            // 给所有cube加上待更新颜色的Tag
            foreach (var cubeEntity in GetQuery().Entities)
            {
                cq.AddTag<ColorToBeUpdated>(cubeEntity.Id);
            }
            cq.Apply();
            entity.RemoveTag<ColorToBeUpdated>();

            // 该区域记录命中次数
            RecordHitCounts(entity);
            var directNeighborRelations = entity.GetRelations<NeighborOf>();
            foreach (var directNeighborRelation in directNeighborRelations)
            {
                var neighborEntity = directNeighborRelation.Target;
                RecordHitCounts(neighborEntity);
            }
            
            // // 高亮所有邻居（当实现了 Relation 系统后取消注释）
            // if (entity.HasRelationType<NeighborOf>())
            // {
            //     var neighbors = entity.GetRelations<NeighborOf>();
            //     foreach (var neighbor in neighbors)
            //     {
            //         if (!neighbor.Target.IsDeleted())
            //         {
            //             if (!neighbor.Target.HasComponent<CubeColor>())
            //             {
            //                 neighbor.Target.AddComponent(new CubeColor(neighborColor));
            //             }
            //             else
            //             {
            //                 ref var nColor = ref neighbor.Target.GetComponent<CubeColor>();
            //                 nColor.Value = new Vector4(neighborColor.r, neighborColor.g, neighborColor.b, neighborColor.a);
            //             }
            //         }
            //     }
            //     
            //     Debug.Log($"✨ 高亮了 {neighbors.Count} 个邻居");
            // }
        }

        public void updateNeighborColors(Color color)
        {
            var cq = world.GetCommandQueue();
            var queryRelation = world.QueryRelation<NeighborOf>();
            queryRelation.ForEachEntity((ref NeighborOf neighborOf, IEntity entity) =>
            {
                // 筛选出当前已经是目标颜色的方块，对其邻居进行染色
                if (entity.TryGetComponent<CubeColor>(out var cubeColor) && cubeColor.Value.Equals(color))
                {
                    ref var targetEntity = ref neighborOf.Target;
                    if (!targetEntity.HasComponent<CubeColor>() ||
                        !targetEntity.GetComponent<CubeColor>().Value.Equals(color))
                    {
                        cq.AddComponent(targetEntity.Id, new CubeColor(color));
                        cq.RemoveTag<ColorToBeUpdated>(targetEntity.Id);
                    }
                }
            });
            cq.Apply();
            // Debug.Log($"处理了{queryRelation.EntityCount}个实体");
        }

        private IEntity FindNearestEntityOnRay(Ray ray)
        {
            var query = GetQuery();
            IEntity nearest = default;

            Vector3 origin = ray.origin;
            Vector3 direction = ray.direction.normalized;

            const float cubeRadius = 0.6f;              // 方块半径（米），根据阵列大小可调
            // const float depthBias = 0.015f;             // 深度权重，越靠近摄像机权重越高
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

                Vector3 toPoint = entityPos - origin;
                float alongRay = Vector3.Dot(toPoint, direction);
                if (alongRay < 0f) continue; // 在相机后方

                Vector3 closestPoint = origin + direction * alongRay;
                float perpDistance = Vector3.Distance(closestPoint, entityPos);

                // 根据深度扩展容差，越远的实体允许稍大的偏差
                float dynamicRadius = cubeRadius + alongRay * 0.01f;
                if (perpDistance > dynamicRadius) continue;

                float score = perpDistance + alongRay * depthBias;
                if (score < bestScore)
                {
                    bestScore = score;
                    nearest = entity;
                }
            }

            if (nearest == null)
            {
                Debug.Log("❌ 没有找到与射线足够接近的实体");
            }

            return nearest;
        }

        private void RecordHitCounts(IEntity entity)
        {
            if (!entity.HasComponent<HitCounter>())
            {
                entity.AddComponent<HitCounter>(new HitCounter{ counts = 1 });
            }
            else
            {
                entity.AddComponent(new HitCounter { counts = entity.GetComponent<HitCounter>().counts + 1 });
            }
        }
    }
}