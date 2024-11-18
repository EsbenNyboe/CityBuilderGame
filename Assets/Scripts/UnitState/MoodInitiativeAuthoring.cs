using Unity.Entities;
using UnityEngine;

namespace UnitState
{
    public partial struct MoodInitiative : IComponentData
    {
        public float Initiative;
    }

    public class MoodInitiativeAuthoring : MonoBehaviour
    {
        [SerializeField] private float _initiative;

        public class MoodInitiativeBaker : Baker<MoodInitiativeAuthoring>
        {
            public override void Bake(MoodInitiativeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MoodInitiative { Initiative = authoring._initiative });
            }
        }
    }

    public partial struct MoodInitiative
    {
        public readonly bool HasInitiative()
        {
            return Initiative >= 1f;
        }

        public void UseInitiative()
        {
            Initiative = 0;
        }
    }
}