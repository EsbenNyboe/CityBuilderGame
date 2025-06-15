using Inventory;
using Unity.Entities;
using Unity.Mathematics;

namespace StorageNS
{
    public struct StorageRequest : IComponentData
    {
        /// <summary>
        /// The request receiver, which should be a storage-container.
        /// </summary>
        public int2 GridCell;

        public StorageRequestType RequestType;

        /// <summary>
        /// The entity that sent the request.
        /// </summary>
        public Entity RequesterEntity;

        /// <summary>
        /// The item type.
        /// </summary>
        public InventoryItem ItemType;
    }

    public enum StorageRequestType
    {
        Deposit,
        Withdraw
    }
}