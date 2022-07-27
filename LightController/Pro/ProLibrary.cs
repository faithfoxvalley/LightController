using System.Collections.Generic;

namespace LightController.Pro
{
    public class ProLibrary
    {
        public string Uuid { get; }

        public string Name { get; }

        public bool HasData { get; private set; } = false;

        private Dictionary<string, Packet.ItemId> libraryItems = new Dictionary<string, Packet.ItemId>();

        public ProLibrary(Packet.ItemId id)
        {
            Uuid = id.uuid;
            Name = id.name;
        }

        public bool ContainsPresentation(string uuid)
        {
            if (!HasData)
                return false;
            return libraryItems.ContainsKey(uuid);
        }

        public void UpdateLibraryData(Packet.LibraryItemList items)
        {
            if(items.update_type == "remove")
            {
                foreach (Packet.ItemId id in items.items)
                {
                    if(libraryItems.ContainsKey(id.uuid))
                        libraryItems.Remove(id.uuid);
                }
            }
            else if(items.update_type == "add" || items.update_type == "all")
            {
                foreach (Packet.ItemId id in items.items)
                {
                    if (!libraryItems.ContainsKey(id.uuid))
                        libraryItems.Add(id.uuid, id);
                }
            }

            HasData = true;
        }
    }
}
