using Unity.Collections;
using Unity.Entities;

namespace UnitState.SocialLogic
{
    public struct SocialEvaluationManager : IComponentData
    {
        public NativeQueue<Entity> SocialEvaluationQueue;
    }
}