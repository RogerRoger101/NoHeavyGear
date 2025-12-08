using Newtonsoft.Json;
using Oxide.Core;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("NoHeavyGear", "Gattaca", "1.0.3")]
    [Description("Prevents players wearing heavy armor from mounting vehicles")]
    class NoHeavyGear : RustPlugin
    {
        #region Configuration

        private Configuration config;
        private Dictionary<ulong, float> lastWarningTime = new Dictionary<ulong, float>();
        private const float MessageCooldown = 2f; // 2 seconds between messages

        public class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; } = "1.0.3";

            [JsonProperty("Vehicles Affected By Weight Check (short prefab names)")]
            public List<string> VehiclesPrefabs { get; set; } = new List<string>
            {
                "minicopter.entity",
                "scraptransporthelicopter",
                "attackhelicopter.entity",
                "rowboat",
                "submarine.solo.entity",
                "submarine.duo.entity"
            };

            [JsonProperty("Blocked Wear Items (shortnames)")]
            public List<string> BlockedItems { get; set; } = new List<string>
            {
                "heavy.plate.helmet",
                "heavy.plate.jacket",
                "heavy.plate.pants"
            };

            [JsonProperty("Require All Listed Wear Items To Block (true = must wear all; false = any listed item blocks)")]
            public bool RequireAllItems { get; set; } = false;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                // ✅ Clean duplicates if any (from older versions)
                config.VehiclesPrefabs = config.VehiclesPrefabs.Distinct().ToList();
                config.BlockedItems = config.BlockedItems.Distinct().ToList();
            }
            catch
            {
                Puts("Configuration file is invalid or missing. Creating new config with default values.");
                LoadDefaultConfig();
                SaveConfig(); // only save when creating new config
            }
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file with default values.");
            config = new Configuration();
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        private void Unload()
        {
            lastWarningTime.Clear();
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Warn.MountRestrictedItems"] = "Mount blocked. Remove these items before mounting:\n{0}",
                ["Warn.CannotEquipRestrictedWhileMounted"] = "Equip blocked while mounted on this vehicle:\n{0}"
            }, this);
        }

        private string Lang(string key, string id = null, params object[] args) =>
            string.Format(lang.GetMessage(key, this, id), args);

        #endregion

        #region Permissions

        private const string PermissionBypass = "NoHeavyGear.bypass";

        private void Init()
        {
            permission.RegisterPermission(PermissionBypass, this);

            if (config == null)
            {
                PrintError("Configuration failed to load!");
                return;
            }

            Puts("Loaded configuration:");
            Puts($"  - Monitoring {config.VehiclesPrefabs.Count} vehicle type(s)");
            Puts($"  - Blocking {config.BlockedItems.Count} item(s)");
            Puts($"  - Require all items: {config.RequireAllItems}");
        }

        #endregion

        #region Hooks

        private object CanMountEntity(BasePlayer player, BaseMountable mountable)
        {
            if (player == null || mountable == null)
                return null;

            if (permission.UserHasPermission(player.UserIDString, PermissionBypass))
                return null;

            BaseVehicle vehicle = mountable.GetComponentInParent<BaseVehicle>();
            if (vehicle == null)
                return null;

            if (!IsAffectedVehicle(vehicle))
                return null;

            List<string> restrictedItems = GetWornRestrictedItems(player);
            if (restrictedItems.Count == 0)
                return null;

            if (config.RequireAllItems && restrictedItems.Count < config.BlockedItems.Count)
                return null;

            string itemList = string.Join("\n", restrictedItems);
            player.ChatMessage(Lang("Warn.MountRestrictedItems", player.UserIDString, itemList));
            return false;
        }

        private object CanWearItem(PlayerInventory inventory, Item item, int targetSlot)
        {
            if (inventory == null || item == null)
                return null;

            BasePlayer player = inventory.GetComponent<BasePlayer>();
            if (player == null)
                return null;

            if (permission.UserHasPermission(player.UserIDString, PermissionBypass))
                return null;

            if (!config.BlockedItems.Contains(item.info.shortname))
                return null;

            if (!player.isMounted)
                return null;

            BaseMountable mountable = player.GetMounted();
            if (mountable == null)
                return null;

            BaseVehicle vehicle = mountable.GetComponentInParent<BaseVehicle>();
            if (vehicle == null)
                return null;

            if (!IsAffectedVehicle(vehicle))
                return null;

            float currentTime = UnityEngine.Time.realtimeSinceStartup;
            if (lastWarningTime.TryGetValue(player.userID, out float lastTime) &&
                currentTime - lastTime < MessageCooldown)
            {
                return false; // still blocked, no message
            }

            lastWarningTime[player.userID] = currentTime;
            player.ChatMessage(Lang("Warn.CannotEquipRestrictedWhileMounted", player.UserIDString, item.info.displayName.english));
            return false;
        }

        #endregion

        #region Helper Methods

        private bool IsAffectedVehicle(BaseVehicle vehicle)
        {
            if (vehicle == null || config?.VehiclesPrefabs == null)
                return false;

            string shortPrefabName = vehicle.ShortPrefabName;
            return config.VehiclesPrefabs.Any(prefab => shortPrefabName.Contains(prefab));
        }

        private List<string> GetWornRestrictedItems(BasePlayer player)
        {
            List<string> restrictedItems = new List<string>();

            if (config?.BlockedItems == null)
                return restrictedItems;

            foreach (Item item in player.inventory.containerWear.itemList)
            {
                if (config.BlockedItems.Contains(item.info.shortname))
                {
                    restrictedItems.Add(item.info.displayName.english);
                }
            }

            return restrictedItems;
        }

        #endregion
    }
}
