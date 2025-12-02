using System;
using UnityEngine;

namespace PGD.Drones
{
    public partial class DroneUpdateTransformSystem : PGDSystem<PGDTransform, PGDPosition, Start, Target>
    {
        private static float s_GlobalTime = 0f;
        public static float Elapsed = 0f;
        public static float waveIntensity = 1.0f;
        public static float Duration = 1000;

        public static int ComplexityLevel = 3;

        protected override void OnAddWorld(IECSWorld world)
        {
            QueryFilter.WithoutAnyTags(ITags.Get<Disabled>());
        }

        protected override void OnUpdate()
        {
            // Debug.Log("PGD DroneUpdateTransformSystem running");
            s_GlobalTime += Time.deltaTime;
            Elapsed += Time.deltaTime * 1000;
            var complete = Math.Min(Elapsed / Duration, 1);

            float deltaTime = Time.deltaTime * 1000f;

            GetQuery().ForEachEntity((ref PGDTransform transform, ref PGDPosition position, ref Start start, ref Target target, IEntity entity) => {
                var basePos = System.Numerics.Vector3.Lerp(start.Value, target.Value, complete);
                // ...
                var finalPos = basePos;

                position.vec3 = finalPos;
                transform.mtr = System.Numerics.Matrix4x4.CreateTranslation(finalPos);
            });
        }
    }
}