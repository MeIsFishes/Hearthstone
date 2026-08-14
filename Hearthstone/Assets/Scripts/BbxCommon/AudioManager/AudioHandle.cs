using System;

namespace BbxCommon
{
    public readonly struct AudioHandle : IEquatable<AudioHandle>
    {
        internal readonly int Id;
        internal readonly int Version;

        internal AudioHandle(int id, int version)
        {
            Id = id;
            Version = version;
        }

        public bool IsValid => Id > 0 && Version > 0;

        public bool Equals(AudioHandle other)
        {
            return Id == other.Id && Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            return obj is AudioHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ Version;
            }
        }

        public static bool operator ==(AudioHandle left, AudioHandle right) => left.Equals(right);
        public static bool operator !=(AudioHandle left, AudioHandle right) => !left.Equals(right);
    }
}
