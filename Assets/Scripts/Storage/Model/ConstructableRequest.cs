using Unity.Entities;
using Unity.Mathematics;

namespace StorageNS
{
    public struct ConstructableRequest : IComponentData
    {
        /// <summary>
        /// The request receiver, which should be a constructable.
        /// </summary>
        public int2 GridCell;

        /// <summary>
        /// Retrieve, if above 0.
        /// Deposit, if below 0.
        /// </summary>
        public int RequestAmount;

        /// <summary>
        /// The entity that sent the request.
        /// </summary>
        public Entity RequesterEntity;
    }
}