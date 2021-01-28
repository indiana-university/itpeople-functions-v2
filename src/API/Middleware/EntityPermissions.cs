using System;

namespace API.Data
{
    [Flags]
    public enum EntityPermissions
    {
        Get =  0b0001,      // Fetch
        Post = 0b0010,      // Create
        Put =  0b0100,      // Update
        Delete = 0b1000,    // Remove
    }
}