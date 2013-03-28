// Guids.cs
// MUST match guids.h
using System;

namespace DaWeng.Jumpy
{
    static class GuidList
    {
        public const string guidJumpyPkgString = "7562916a-8cb7-44e5-8005-6b8c08ecf9e0";
        public const string guidJumpyCmdSetString = "b7529de4-713f-4f48-a2d1-df6b202e607f";

        public static readonly Guid guidJumpyCmdSet = new Guid(guidJumpyCmdSetString);
    };
}