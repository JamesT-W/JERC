using JERC.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class Angle : IEquatable<Angle>
    {
        public float pitch;
        public float yaw;
        public float roll;

        public Angle(string angle)
        {
            if (string.IsNullOrWhiteSpace(angle))
                return;

            var angleSplit = angle.Split(" ");

            if (angleSplit.Count() != 3)
                return;

            float.TryParse(angleSplit[0], Globalization.Style, Globalization.Culture, out pitch);
            float.TryParse(angleSplit[1], Globalization.Style, Globalization.Culture, out yaw);
            float.TryParse(angleSplit[2], Globalization.Style, Globalization.Culture, out roll);
        }

        public Angle(float pitch, float yaw, float roll)
        {
            this.pitch = pitch;
            this.yaw = yaw;
            this.roll = roll;
        }


        public bool Equals(Angle other)
        {
            if (pitch == other.pitch && yaw == other.yaw && roll == other.roll)
                return true;

            return false;
        }


        public override int GetHashCode()
        {
            int hashPitch = pitch == 0 ? 0 : pitch.GetHashCode();
            int hashYaw = yaw == 0 ? 0 : yaw.GetHashCode();
            int hashRoll = roll == 0 ? 0 : roll.GetHashCode();

            return hashPitch ^ hashYaw ^ hashRoll;
        }
    }
}
