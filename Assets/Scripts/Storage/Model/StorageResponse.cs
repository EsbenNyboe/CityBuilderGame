using Unity.Entities;

namespace Storage
{
    public struct StorageResponse : IComponentData
    {
        /// <summary>
        /// Add to inventory, if above 0.
        /// Subtract from inventory, if below 0.
        /// </summary>
        public int ItemAmount;

        /// <summary>
        /// The entity that sent the request.
        /// </summary>
        public Entity RequesterEntity;
    }
}