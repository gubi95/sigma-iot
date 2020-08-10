using System;

namespace Sigma.IoT.Data
{
    public class UnitData
    {
        public UnitData(DateTime dateTime, int value)
        {
            DateTime = dateTime;
            Value = value;
        }

        public DateTime DateTime { get; }

        public int Value { get; }

        protected bool Equals(UnitData other)
        {
            return DateTime.Equals(other.DateTime) && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnitData)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (DateTime.GetHashCode() * 397) ^ Value;
            }
        }
    }
}
