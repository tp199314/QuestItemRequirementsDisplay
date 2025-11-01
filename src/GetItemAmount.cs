using HarmonyLib;
using ItemStatsSystem;
using System.Collections.Generic;
using System.Linq;

namespace QuestItemRequirementsDisplay
{
    [HarmonyPatch(typeof(PlayerStorage), "OnDestroy")]
    public class PlayerStorageOnDestroyPatch
    {
        static void Prefix(PlayerStorage __instance)
        {
            GetItemAmount.CachedPlayerStorageItemsCount();
        }
    }

    public class GetItemAmount
    {
        // Cached player storage inventory items count when player is outside of safe-house
        private static readonly Dictionary<int, int> PlayerStorageItemsCount = new Dictionary<int, int>();

        /// <summary>
        /// Cached player storage inventory items count when player is outside of safe-house
        /// </summary>
        public static void CachedPlayerStorageItemsCount()
        {
            PlayerStorageItemsCount.Clear();
            var stack = new Stack<Item>();

            var storage = PlayerStorage.Inventory;
            if (storage?.Content == null)
                return;

            foreach (var item in storage.Content)
            {
                if (item != null)
                    stack.Push(item);
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == null)
                    continue;

                if (PlayerStorageItemsCount.TryGetValue(current.TypeID, out int existing))
                    PlayerStorageItemsCount[current.TypeID] = existing + current.StackCount;
                else
                    PlayerStorageItemsCount[current.TypeID] = current.StackCount;

                var slots = current.Slots;
                if (slots == null)
                    continue;
                foreach (var slot in slots)
                {
                    if (slot?.Content != null)
                        stack.Push(slot.Content);
                }
            }
        }

        /// <summary>
        /// Get the total amount of the specified item type ID across all inventories.
        /// </summary>
        /// <param name="typeID"></param>
        /// <returns></returns>
        public static int GetTotalItemAmount(int typeID)
        {
            var totalAmount = InCharacterInventory(typeID)
                            + InCharacterEquipment(typeID)
                            + InPetInventory(typeID)
                            + InPlayerStorage(typeID);
            return totalAmount;
        }

        /// <summary>
        /// Get the total amount of the specified item type ID in the character's inventory.
        /// </summary>
        /// <param name="typeID"></param>
        /// <returns></returns>
        public static int InCharacterInventory(int typeID)
        {
            var inventory = LevelManager.Instance.MainCharacter.CharacterItem.Inventory;
            var amount = FromInventory(inventory, typeID);
            return amount;
        }

        /// <summary>
        /// Get the total amount of the specified item type ID in the character's equipment.
        /// </summary>
        /// <param name="typeID"></param>
        /// <returns></returns>
        public static int InCharacterEquipment(int typeID)
        {
            var characterEquipments = LevelManager.Instance.MainCharacter.CharacterItem.Slots;
            var inventory = new List<Item>();
            foreach (var slot in characterEquipments)
            {
                if (slot?.Content != null)
                {
                    inventory.Add(slot.Content);
                }
            }
            return FromInventory(inventory, typeID);
        }

        /// <summary>
        /// Get the total amount of the specified item type ID in the pet's inventory.
        /// </summary>
        /// <param name="typeID"></param>
        /// <returns></returns>
        public static int InPetInventory(int typeID)
        {
            var inventory = LevelManager.Instance.PetProxy.Inventory;
            var amount = FromInventory(inventory, typeID);
            return amount;
        }

        /// <summary>
        /// Get the total amount of the specified item type ID in the player's storage.
        /// </summary>
        /// <param name="typeID"></param>
        /// <returns></returns>
        public static int InPlayerStorage(int typeID)
        {
            var amount = 0;
            if (PlayerStorage.Inventory == null)
            {
                amount += PlayerStorageItemsCount.TryGetValue(typeID, out int cachedAmount) ? cachedAmount : 0;
            }
            else
            {
                amount += FromInventory(PlayerStorage.Inventory, typeID);
            }
            return amount;
        }

        /// <summary>
        /// Get the total amount of the specified item type ID in the given inventory.
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="typeID"></param>
        /// <returns></returns>
        private static int FromInventory(Inventory inventory, int typeID)
        {
            if (inventory == null) return 0;

            return GetTotalAmountInInventory(inventory.FindAll(item => item != null), typeID);
        }
        private static int FromInventory(List<Item> inventory, int typeID)
        {
            return GetTotalAmountInInventory(inventory, typeID);
        }

        /// <summary>
        /// Get the total amount of the specified item type ID in the given inventory (Include all nested slots).
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="typeID"></param>
        /// <returns></returns>
        private static int GetTotalAmountInInventory(List<Item> inventory, int typeID)
        {
            if (inventory == null)
                return 0;

            int amount = 0;
            var itemStack = new Stack<Item>();

            foreach (var item in inventory)
            {
                if (item != null)
                    itemStack.Push(item);
            }

            while (itemStack.Count > 0)
            {
                var item = itemStack.Pop();
                if (item == null)
                    continue;

                if (item.TypeID == typeID)
                    amount += item.StackCount;

                var slots = item.Slots;
                if (slots == null)
                    continue;

                foreach (var slot in slots)
                {
                    if (slot?.Content != null)
                        itemStack.Push(slot.Content);
                }
            }
            return amount;
        }
    }
}
