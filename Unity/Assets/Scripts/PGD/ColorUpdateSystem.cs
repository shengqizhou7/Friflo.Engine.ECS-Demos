using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;

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
            if (Input.GetMouseButtonDown(0))
            {
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
            Debug.Log($"➕ 更新了新的颜色组件");
            
            // 给所有cube加上待更新颜色的Tag
            foreach (var cubeEntity in GetQuery().Entities)
            {
                cq.AddTag<ColorToBeUpdated>(cubeEntity.Id);
            }
            cq.Apply();
            entity.RemoveTag<ColorToBeUpdated>();
            
            // 获取实体位置用于调试
            if (entity.HasComponent<PGDTransform>())
            {
                ref var trans = ref entity.GetComponent<PGDTransform>();
                Vector3 pos = new Vector3(trans.mtr.Translation.X, trans.mtr.Translation.Y, trans.mtr.Translation.Z);
                Debug.Log($"📍 实体位置: {pos}");
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
            // var cq = world.GetCommandQueue();
            // GetQuery().ForEachEntity(((ref PGDTransform trans, ref PGDPosition pos, ref Start _, ref Target _, IEntity entity) =>
            // {
            //     if (entity.TryGetComponent<CubeColor>(out var cubeColor) && cubeColor.Value.Equals(color))
            //     {
            //         return;
            //     }
            //     // 当前是未被染色的drone
            //     // 判断其邻居们是否有目标颜色，一旦检测到一个邻居，则该drone需要染色
            //     var relations = entity.GetRelations<NeighborOf>();
            //     // var beRelations = entity.GetBeRelatedLinks<NeighborOf>();
            //     foreach (var relation in relations)
            //     {
            //         if (relation.Target.TryGetComponent<CubeColor>(out var cubeColor))
            //         {
            //             
            //         }
            //     }
            //     cq.AddComponent(entity.Id, new CubeColor(color));
            // }));
            // cq.Apply();
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
        
        /// <summary>
        /// 使用射线查找最近的实体（数学方法，不依赖物理系统）
        /// 缺陷：不支持立方体阵列
        /// </summary>
        // private IEntity FindNearestEntityOnRay(Ray ray)
        // {
        //     // var query = world.Query().WithAllComponents(IComponents.Get<PGDTransform, PGDPosition, Start, Target>()).WithoutAnyTags(ITags.Get<Disabled>());
        //     var query = GetQuery();
        //     IEntity nearest = default;
        //     float minDistance = float.MaxValue;
        //     float maxClickDistance = 1.5f; // 最大点击距离（单位：米）
            
        //     Vector3 rayOrigin = ray.origin;
        //     Vector3 rayDirection = ray.direction.normalized;
            
        //     foreach (var entity in query.Entities)
        //     {
        //         ref var trans = ref entity.GetComponent<PGDTransform>();
                
        //         Vector3 entityPos = new Vector3(
        //             trans.mtr.Translation.X, 
        //             trans.mtr.Translation.Y, 
        //             trans.mtr.Translation.Z
        //         );
                
        //         // 计算射线到点的最短距离
        //         // 公式：distance = ||(P - O) - ((P - O) · d) * d||
        //         Vector3 toPoint = entityPos - rayOrigin;
        //         float dotProduct = Vector3.Dot(toPoint, rayDirection);
                
        //         // 如果点在射线后面，跳过
        //         if (dotProduct < 0) continue;
                
        //         // 计算射线上最近点到实体的距离
        //         Vector3 closestPointOnRay = rayOrigin + rayDirection * dotProduct;
        //         float distance = Vector3.Distance(closestPointOnRay, entityPos);
                
        //         // 找到距离最近且在点击范围内的实体
        //         if (distance < minDistance && distance < maxClickDistance)
        //         {
        //             minDistance = distance;
        //             nearest = entity;
        //         }
        //     }
            
        //     Debug.Log($"🎯 最近实体距离射线: {minDistance:F2}m");
        //     return nearest;
        // }
        private IEntity FindNearestEntityOnRay(Ray ray)
        {
            var query = GetQuery();
            Debug.Log($"{query.EntityCount} ");
            IEntity nearest = default;

            float minAlongRay = float.MaxValue;      // 离摄像机最近的深度
            float minPerpDistance = float.MaxValue;  // 同深度时比较垂直距离
            const float epsilon = 0.1f;              // 增大容差，避免浮点精度问题
            float maxPerpDistance = 2.0f;            // 增大垂直阈值到 2.0 米

            Vector3 rayOrigin = ray.origin;
            Vector3 rayDirection = ray.direction.normalized;
            
            int candidateCount = 0;
            int selectedCount = 0;

            foreach (var entity in query.Entities)
            {
                ref var trans = ref entity.GetComponent<PGDTransform>();
                Vector3 entityPos = new Vector3(
                    trans.mtr.Translation.X,
                    trans.mtr.Translation.Y,
                    trans.mtr.Translation.Z
                );

                Vector3 toPoint = entityPos - rayOrigin;
                float alongRay = Vector3.Dot(toPoint, rayDirection);
                if (alongRay < 0f) continue;              // 背向摄像机

                Vector3 closestPoint = rayOrigin + rayDirection * alongRay;
                float perpDistance = Vector3.Distance(closestPoint, entityPos);
                
                if (perpDistance > maxPerpDistance) continue;
                
                candidateCount++;

                // 核心逻辑：优先选择沿射线最近的实体（遮挡效果）
                bool closerAlongRay = alongRay < minAlongRay - epsilon;
                bool sameDepthButCloser =
                    Mathf.Abs(alongRay - minAlongRay) <= epsilon &&
                    perpDistance < minPerpDistance - epsilon;

                if (closerAlongRay || sameDepthButCloser)
                {
                    minAlongRay = alongRay;
                    minPerpDistance = perpDistance;
                    nearest = entity;
                    selectedCount++;
                }
            }

            if (nearest != null)
            {
                Debug.Log($"🎯 选中实体 ID:{nearest.Id} | 垂直距离: {minPerpDistance:F3}m | 深度: {minAlongRay:F3}m | 候选数: {candidateCount} | 更新次数: {selectedCount}");
            }
            else if (candidateCount > 0)
            {
                Debug.LogWarning($"⚠️ 找到 {candidateCount} 个候选实体，但没有选中任何实体（逻辑可能有误）");
            }
            else
            {
                Debug.Log($"❌ 没有找到候选实体（垂直距离 > {maxPerpDistance}m）");
            }
            
            return nearest;
        }
        
        // /// <summary>
        // /// 根据位置查找最近的实体（备用方法）
        // /// </summary>
        // private IEntity FindNearestEntity(Vector3 position)
        // {
        //     var query = world.Query().WithAllComponents(IComponents.Get<PGDTransform, PGDPosition, Start, Target>()).WithoutAnyTags(ITags.Get<Disabled>());
        //     IEntity nearest = default;
        //     float minDistance = float.MaxValue;
        //     
        //     foreach (var entity in query.Entities)
        //     {
        //         ref var trans = ref entity.GetComponent<PGDTransform>();
        //         
        //         // 修复：Z 坐标应该用 Translation.Z 而不是 Translation.X
        //         Vector3 entityPos = new Vector3(
        //             trans.mtr.Translation.X, 
        //             trans.mtr.Translation.Y, 
        //             trans.mtr.Translation.Z
        //         );
        //         
        //         float distance = Vector3.Distance(position, entityPos);
        //         if (distance < minDistance && distance < 2f)  // 2米范围内
        //         {
        //             minDistance = distance;
        //             nearest = entity;
        //         }
        //     }
        //     
        //     return nearest;
        // }
    }
}