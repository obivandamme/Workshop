namespace Workshop.KIS
{
    using System;
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

        private static MethodInfo kis_DeleteItem;

        private readonly object obj;

        public ModuleKISInventory(object obj)
        {
            this.obj = obj;
        }

        public InventoryType invType
        {
            get
            {
                return (InventoryType)Enum.Parse(typeof(InventoryType), kis_invType.GetValue(this.obj).ToString());
            }
        }

        public int podSeat
        {
            get
            {
                return (int)kis_podSeat.GetValue(this.obj);
            }
        }

        public float maxVolume
        {
            get
            {
                return (float)kis_maxVolume.GetValue(this.obj);
            }
        }

        public bool showGui
        {
            get
            {
                return (bool)kis_showGui.GetValue(this.obj);
            }
        }

        public Part part
        {
            get
            {
                return ((PartModule)this.obj).part;
            }
        }

        public Dictionary<int, KIS_Item> items
        {
            get
            {
                var dict = (Dictionary<int, object>)kis_items.GetValue(this.obj);
                return dict.ToDictionary(i => i.Key, i => new KIS_Item(i.Value));
            }
        }

        public float GetContentVolume()
        {
            return (float)kis_GetContentVolume.Invoke(this.obj, null);
        }

        public bool isFull()
        {
            return (bool)kis_isFull.Invoke(this.obj, null);
        }

        public KIS_Item AddItem(Part partPrefab)
        {
            return (KIS_Item)kis_AddItem.Invoke(this.obj, new object[] { partPrefab });
        }

        public void DeleteItem(int slot)
        {
            kis_DeleteItem.Invoke(this.obj, new object[] { slot });
        }

        internal static void Initialize(Assembly kisAssembly)
        {
            ModuleKISInventory_class = kisAssembly.GetTypes().First(t => t.Name.Equals("ModuleKISInventory"));
            kis_invType = ModuleKISInventory_class.GetField("invType");
            kis_podSeat = ModuleKISInventory_class.GetField("podSeat");
            kis_maxVolume = ModuleKISInventory_class.GetField("maxVolume");
            kis_showGui = ModuleKISInventory_class.GetField("showGUI");
            kis_items = ModuleKISInventory_class.GetField("items");
            kis_AddItem = ModuleKISInventory_class.GetMethod("AddItem");
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

            private readonly object obj;

            public string resourceName
            {
                get
                {
                    return (string)kis_resourceName.GetValue(this.obj);
                }
            }

            public double amount
            {
                get
                {
                    return (double)kis_amount.GetValue(this.obj);
                }
            }

            public double maxAmount
            {
                get
                {
                    return (double)kis_maxAmount.GetValue(this.obj);
                }
            }

            public ResourceInfo(object obj)
            {
                this.obj = obj;
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

        private static MethodInfo kis_GetResources;

        private static MethodInfo kis_SetResource;

        private static MethodInfo kis_EnableIcon;

        private static MethodInfo kis_DisableIcon;

        private readonly object obj;

        public KIS_Item(object obj)
        {
            this.obj = obj;
        }

        public KIS_IconViewer icon
        {
            get
            {
                return new KIS_IconViewer(kis_icon.GetValue(this.obj));
            }
        }

        public AvailablePart availablePart
        {
            get
            {
                return (AvailablePart)kis_availablePart.GetValue(this.obj);
            }
        }

        public List<ResourceInfo> GetResources()
        {
            var list = (List<object>)kis_GetResources.Invoke(this.obj, null);
            return list.Select(o => new ResourceInfo(o)).ToList();
        }

        public void SetResource(string name, int amount)
        {
            kis_SetResource.Invoke(this.obj, new object[] { name, amount });
        }

        public void EnableIcon(int resolution)
        {
            kis_EnableIcon.Invoke(this.obj, new object[] { resolution });
        }

        public void DisableIcon()
        {
            kis_DisableIcon.Invoke(this.obj, null);
        }

        internal static void Initialize(Assembly kisAssembly)
        {
            KIS_Item_class = kisAssembly.GetTypes().First(t => t.Name.Equals("KIS_Item"));
            kis_icon = KIS_Item_class.GetField("icon");
            kis_availablePart = KIS_Item_class.GetField("availablePart");
            kis_GetResources = KIS_Item_class.GetMethod("GetResources");
            kis_SetResource = KIS_Item_class.GetMethod("SetResource");
            kis_EnableIcon = KIS_Item_class.GetMethod("EnableIcon");
            kis_DisableIcon = KIS_Item_class.GetMethod("DisableIcon");
        }
    }

    public class KIS_IconViewer
    {
        private static Type KIS_IconViewser_class;

        private static FieldInfo kis_texture;

        private readonly object obj;

        public Texture texture
        {
            get
            {
                return (Texture)kis_texture.GetValue(this.obj);
            }
        }

        public KIS_IconViewer(object obj)
        {
            this.obj = obj;
        }

        public KIS_IconViewer(Part p, int resolution) : this(Activator.CreateInstance(KIS_IconViewser_class, new object[] { p, resolution }))
        {
        }

        internal static void Initialize(Assembly kisAssembly)
        {
            KIS_IconViewser_class = kisAssembly.GetTypes().First(t => t.Name.Equals("KIS_IconViewer"));
            kis_texture = KIS_IconViewser_class.GetField("texture");
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
