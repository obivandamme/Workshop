namespace Workshop.KIS
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using UnityEngine;

    public class KIS_Shared
    {
        private static Type KIS_Shared_class;

        private static MethodInfo kis_GetPartVolume;

        public static float GetPartVolume(Part partPrefab)
        {
            return (float)kis_GetPartVolume.Invoke(null, new object[] { partPrefab });
        }

        internal static void Initialize(Assembly kisAssembly)
        {
            KIS_Shared_class = kisAssembly.GetTypes().First(t => t.Name.Equals("KIS_Shared"));
            kis_GetPartVolume = KIS_Shared_class.GetMethod("GetPartVolume");
        }
    }

    public class ModuleKISInventory
    {
        public enum InventoryType { Container, Pod, Eva }

        private static Type ModuleKISInventory_class;

        private static FieldInfo kis_invType;

        private static FieldInfo kis_podSeat;

        private static FieldInfo kis_maxVolume;

        private static FieldInfo kis_showGui;

        private static FieldInfo kis_items;

        private static MethodInfo kis_GetContentVolume;

        private static MethodInfo kis_isFull;

        private static MethodInfo kis_AddItem;

        private readonly object _obj;

        public ModuleKISInventory(object obj)
        {
            _obj = obj;
        }

        public InventoryType invType
        {
            get
            {
                return (InventoryType)Enum.Parse(typeof(InventoryType), kis_invType.GetValue(_obj).ToString());
            }
        }

        public int podSeat
        {
            get
            {
                return (int)kis_podSeat.GetValue(_obj);
            }
        }

        public float maxVolume
        {
            get
            {
                return (float)kis_maxVolume.GetValue(_obj);
            }
        }

        public bool showGui
        {
            get
            {
                return (bool)kis_showGui.GetValue(_obj);
            }
        }

        public Part part
        {
            get
            {
                return ((PartModule)_obj).part;
            }
        }

        public Dictionary<int, KIS_Item> items
        {
            get
            {
                var dict = new Dictionary<int, KIS_Item>();
                var inventoryItems = (IDictionary)kis_items.GetValue(_obj);

                foreach (DictionaryEntry entry in inventoryItems)
                {
                    dict.Add((int)entry.Key, new KIS_Item(entry.Value));
                }

                return dict;
            }
        }

        public float GetContentVolume()
        {
            return (float)kis_GetContentVolume.Invoke(_obj, null);
        }

        public bool isFull()
        {
            return (bool)kis_isFull.Invoke(_obj, null);
        }

        public KIS_Item AddItem(Part partPrefab)
        {
            var obj = kis_AddItem.Invoke(_obj, new object[] { partPrefab, 1f, -1 });
            return new KIS_Item(obj);
        }

        internal static void Initialize(Assembly kisAssembly)
        {
            ModuleKISInventory_class = kisAssembly.GetTypes().First(t => t.Name.Equals("ModuleKISInventory"));
            kis_invType = ModuleKISInventory_class.GetField("invType");
            kis_podSeat = ModuleKISInventory_class.GetField("podSeat");
            kis_maxVolume = ModuleKISInventory_class.GetField("maxVolume");
            kis_showGui = ModuleKISInventory_class.GetField("showGui");
            kis_items = ModuleKISInventory_class.GetField("items");
            kis_AddItem = ModuleKISInventory_class.GetMethod("AddItem", new[] { typeof(Part), typeof(float), typeof(int) });
            kis_GetContentVolume = ModuleKISInventory_class.GetMethod("GetContentVolume");
            kis_isFull = ModuleKISInventory_class.GetMethod("isFull");
        }
    }

    public class KIS_Item
    {
        public struct ResourceInfo
        {
            private static Type ResourceInfo_struct;

            private static FieldInfo kis_resourceName;

            private static FieldInfo kis_amount;

            private static FieldInfo kis_maxAmount;

            private readonly object _obj;

            public string resourceName
            {
                get
                {
                    return (string)kis_resourceName.GetValue(_obj);
                }
            }

            public double amount
            {
                get
                {
                    return (double)kis_amount.GetValue(_obj);
                }
            }

            public double maxAmount
            {
                get
                {
                    return (double)kis_maxAmount.GetValue(_obj);
                }
            }

            public ResourceInfo(object obj)
            {
                _obj = obj;
            }

            public static void Initialize(Assembly kisAssembly)
            {
                ResourceInfo_struct = kisAssembly.GetTypes().First(t => t.Name.Equals("ResourceInfo"));
                kis_resourceName = ResourceInfo_struct.GetField("resourceName");
                kis_amount = ResourceInfo_struct.GetField("amount");
                kis_maxAmount = ResourceInfo_struct.GetField("maxAmount");
            }
        }

        private static Type KIS_Item_class;

        private static FieldInfo kis_icon;

        private static FieldInfo kis_availablePart;

        private static FieldInfo kis_quantity;

        private static MethodInfo kis_GetResources;

        private static MethodInfo kis_SetResource;

        private static MethodInfo kis_EnableIcon;

        private static MethodInfo kis_DisableIcon;

        private static MethodInfo kis_StackRemove;

        private readonly object _obj;

        public KIS_Item(object obj)
        {
            _obj = obj;
        }

        public KIS_IconViewer Icon
        {
            get
            {
                var kisIcon = kis_icon.GetValue(_obj);
                if (kisIcon == null)
                {
                    return null;
                }
                return new KIS_IconViewer(kisIcon);
            }
        }

        public AvailablePart availablePart
        {
            get
            {
                return (AvailablePart)kis_availablePart.GetValue(_obj);
            }
        }

        public float quantity
        {
            get { return (float) kis_quantity.GetValue(_obj); }
        }

        public List<ResourceInfo> GetResources()
        {
            var list = (IList)kis_GetResources.Invoke(_obj, null);
            return list.Cast<object>().Select(o => new ResourceInfo(o)).ToList();
        }

        public void SetResource(string name, int amount)
        {
            kis_SetResource.Invoke(_obj, new object[] { name, amount });
        }

        public void EnableIcon(int resolution)
        {
            kis_EnableIcon.Invoke(_obj, new object[] { resolution });
        }

        public void DisableIcon()
        {
            kis_DisableIcon.Invoke(_obj, null);
        }

        public void StackRemove(float quantity)
        {
            kis_StackRemove.Invoke(_obj, new object[] { quantity });
        }

        internal static void Initialize(Assembly kisAssembly)
        {
            KIS_Item_class = kisAssembly.GetTypes().First(t => t.Name.Equals("KIS_Item"));
            kis_icon = KIS_Item_class.GetField("icon");
            kis_availablePart = KIS_Item_class.GetField("availablePart");
            kis_quantity = KIS_Item_class.GetField("quantity");
            kis_GetResources = KIS_Item_class.GetMethod("GetResources");
            kis_SetResource = KIS_Item_class.GetMethod("SetResource");
            kis_EnableIcon = KIS_Item_class.GetMethod("EnableIcon");
            kis_DisableIcon = KIS_Item_class.GetMethod("DisableIcon");
            kis_StackRemove = KIS_Item_class.GetMethod("StackRemove");
        }
    }

    public class KIS_IconViewer
    {
        private static Type KIS_IconViewer_class;

        private static FieldInfo kis_texture;

        private readonly object _obj;

        public Texture texture
        {
            get
            {
                return (Texture)kis_texture.GetValue(_obj);
            }
        }

        public KIS_IconViewer(object obj)
        {
            _obj = obj;
        }

        public KIS_IconViewer(Part p, int resolution)
            : this(Activator.CreateInstance(KIS_IconViewer_class, new object[] { p, resolution }))
        {
        }

        internal static void Initialize(Assembly kisAssembly)
        {
            KIS_IconViewer_class = kisAssembly.GetTypes().First(t => t.Name.Equals("KIS_IconViewer"));
            kis_texture = KIS_IconViewer_class.GetField("texture");
        }
    }

    public class KISWrapper
    {
        public static bool Initialize()
        {
            var kisAssembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name.Equals("KIS", StringComparison.InvariantCultureIgnoreCase));
            if (kisAssembly == null)
            {
                return false;
            }

            KIS_Shared.Initialize(kisAssembly.assembly);
            ModuleKISInventory.Initialize(kisAssembly.assembly);
            KIS_Item.Initialize(kisAssembly.assembly);
            KIS_IconViewer.Initialize(kisAssembly.assembly);
            KIS_Item.ResourceInfo.Initialize(kisAssembly.assembly);
            return true;
        }

        public static List<ModuleKISInventory> GetInventories(Vessel vessel)
        {
            var inventories = new List<ModuleKISInventory>();
            foreach (var part in vessel.parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    if (module.moduleName == "ModuleKISInventory")
                    {
                        inventories.Add(new ModuleKISInventory(module));
                    }
                }
            }
            return inventories;
        }
    }
}
