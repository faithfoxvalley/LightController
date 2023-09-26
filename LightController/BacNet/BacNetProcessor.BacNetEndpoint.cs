using System;
using System.Collections.Generic;
using System.IO.BACnet;

namespace LightController.BacNet
{
    public partial class BacNetProcessor
    {
        private class BacNetEndpoint : IEquatable<BacNetEndpoint>
        {
            public uint DeviceId { get; }
            public BacnetObjectId ObjectId { get; }

            public BacNetEndpoint(uint deviceId, BacnetObjectId objectId)
            {
                DeviceId = deviceId;
                ObjectId = objectId;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as BacNetEndpoint);
            }

            public bool Equals(BacNetEndpoint other)
            {
                return other is not null &&
                       DeviceId == other.DeviceId &&
                       EqualityComparer<BacnetObjectId>.Default.Equals(ObjectId, other.ObjectId);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(DeviceId, ObjectId);
            }

            public static bool operator ==(BacNetEndpoint left, BacNetEndpoint right)
            {
                return EqualityComparer<BacNetEndpoint>.Default.Equals(left, right);
            }

            public static bool operator !=(BacNetEndpoint left, BacNetEndpoint right)
            {
                return !(left == right);
            }
        }
    }
}
