using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Rotations.Interfaces
{
    public interface IRotationTracker
    {
        public ulong DiscordID { get; }

        // This is left blank because set rotations use integers to track,
        // whereas the rotations that use hashes (like Ada-1) are longs.
    }
}
