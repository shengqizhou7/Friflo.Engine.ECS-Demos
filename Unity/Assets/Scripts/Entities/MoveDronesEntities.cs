using UnityEngine;
public class MoveDronesEntities : DroneSystemBase
{
    public override void SetTargetPlane(){}
    public override void SetTargetCube(){}
    public override void SetTargetRing(){}
    public override void SetTargetRings(){}

    public override void IncreaseCount() {
    }

    public override void DecreaseCount() {
    }

    public override void CleanupResources()
    {
    }
    void Start()
    {
        Debug.Log("🟢 MoveDronesEntities 已启动");
    }
}
