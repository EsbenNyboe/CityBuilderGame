using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public partial class DotsSoundManagerSystem : SystemBase
{
    protected override void OnCreate()
    {
        EntityManager.AddComponent<DotsSoundManager>(SystemHandle);
        SystemAPI.SetComponent(SystemHandle, new DotsSoundManager
        {
            ChopSoundRequests = new NativeQueue<float3>(Allocator.Persistent),
            DestroyTreeSoundRequests = new NativeQueue<float3>(Allocator.Persistent)
        });
    }

    protected override void OnUpdate()
    {
        var dotsSoundManager = SystemAPI.GetComponent<DotsSoundManager>(SystemHandle);
        var chopSoundRequests = dotsSoundManager.ChopSoundRequests;
        while (chopSoundRequests.Count > 0)
        {
            SoundManager.Instance.PlayChopSound(chopSoundRequests.Dequeue());
        }

        var destroyTreeSoundRequests = dotsSoundManager.DestroyTreeSoundRequests;
        while (destroyTreeSoundRequests.Count > 0)
        {
            SoundManager.Instance.PlayDestroyTreeSound(destroyTreeSoundRequests.Dequeue());
        }
    }

    protected override void OnDestroy()
    {
        var dotsSoundManager = SystemAPI.GetComponent<DotsSoundManager>(SystemHandle);
        dotsSoundManager.ChopSoundRequests.Dispose();
        dotsSoundManager.DestroyTreeSoundRequests.Dispose();
    }
}