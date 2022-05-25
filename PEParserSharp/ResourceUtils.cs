using PEParserSharp.headers.resources;
using PEParserSharp.misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PEParserSharp
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 2)]
    struct IconDirResEntry
    {
        public byte Width;
        public byte Height;
        public byte Colors;
        public byte Reserved;
        public short Planes;
        public short BitsPerPixel;
        public int ImageSize;
        public short ResourceID;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 2)]
    struct IcoHeader
    {
        public short Reserved1;
        public short ResourceType;
        public short ImageCount;
        public byte Width;
        public byte Height;
        public byte Colors;
        public byte Reserved2;
        public short Planes;
        public short BitsPerPixel;
        public int ImageSize;
        public int Offset;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 2)]
    struct GroupIcon
    {
        public short Reserved;
        public short ResourceType;
        public short ImageCount;
    }

    public enum IconType
    {
        PNG, BMP, ICO
    }

    public class IconResource
    {
        public string Name;
        public byte[] IconData;
        public IconType IconType;
        public int Width;
        public int Height;

        internal IconResource(string name, byte[] data, IconType type, int width, int height)
            => (Name, IconData, IconType, Width, Height) = (name, data, type, width, height);
    }

    public static class ResourceUtils
    {
        public static IList<IconResource> ExtractIconsFromDLL(string iconFile, int minimumSize, int[] iconIndexes)
            => ExtractIcons(new PeFile(iconFile), minimumSize, iconIndexes);

        public static IList<IconResource> ExtractIcons(this PeFile pe, int minimumSize, int[] iconIndexes)
        {
            var icons = new List<IconResource>();
            if (iconIndexes is null || iconIndexes.Length == 0)
                return icons;

            var mainEntry = pe.optionalHeader.tables.FirstOrDefault(x => x.Type == DirEntry.RESOURCE);
            if (mainEntry is null)
                return icons;
            var root = (ResourceDirectoryHeader)mainEntry.data;
            var iconDir = root.entries.FirstOrDefault(x => x.NAME.get() == "Icon")?.directory;
            var iconsGroupDir = root.entries.FirstOrDefault(x => x.NAME.get() == "Group Icon")?.directory;
            if (iconDir is null || iconsGroupDir is null)
                return icons;

            var sizeof_gi = Marshal.SizeOf(typeof(GroupIcon));
            var sizeof_idre = Marshal.SizeOf(typeof(IconDirResEntry));

            foreach (var idx in iconIndexes)
            {
                var group = iconsGroupDir.entries.FirstOrDefault(x => x.NAME.get() == Convert.ToString(idx, 16))?.directory;
                if (group is null)
                    continue;
                var group_data = Array.ConvertAll(group.entries[0].resourceDataEntry.getData(pe.fileBytes), x => unchecked((byte)(x)));

                var icos = new List<IconDirResEntry>();
                GCHandle iconHandle = GCHandle.Alloc(group_data, GCHandleType.Pinned);
                try
                {
                    var baseAddr = iconHandle.AddrOfPinnedObject();
                    var header = Marshal.PtrToStructure<GroupIcon>(baseAddr);
                    while (icos.Count < header.ImageCount)
                    {
                        var ico = Marshal.PtrToStructure<IconDirResEntry>(new IntPtr(baseAddr.ToInt64() + sizeof_gi + icos.Count * sizeof_idre));
                        icos.Add(ico);
                    }
                }
                finally
                {
                    iconHandle.Free();
                }

                int GetIconWidth(IconDirResEntry ic) => ic.Width == 0 ? 256 : ic.Width;
                int GetIconHeight(IconDirResEntry ic) => ic.Height == 0 ? 256 : ic.Height;

                if (icos.Count is 0)
                    continue;
                var sortedIcos = icos.OrderBy(x => GetIconHeight(x));
                var closest_icon_id = GetIconHeight(sortedIcos.Last()) >= minimumSize ? sortedIcos.First(x => GetIconHeight(x) >= minimumSize) : sortedIcos.Last();
                var icon = iconDir.entries.FirstOrDefault(x => Convert.ToInt32(x.NAME.get(), 16) == closest_icon_id.ResourceID)?.directory;
                if (icon is null)
                    continue;

                byte[] data = Array.ConvertAll(icon.entries[0].resourceDataEntry.getData(pe.fileBytes), x => unchecked((byte)(x)));
                if (data.Take(4).Select(x => (int)x).SequenceEqual(new[] { 137, 80, 78, 71 })) // PNG, ok
                {
                    icons.Add(new IconResource(Convert.ToString(idx, 16), data, IconType.PNG, GetIconWidth(closest_icon_id), GetIconHeight(closest_icon_id)));
                }
                else // BMP, header is missing
                {
                    var header = new IcoHeader()
                    {
                        Reserved1 = 0,
                        ResourceType = 1,
                        ImageCount = 1,
                        Width = closest_icon_id.Width,
                        Height = closest_icon_id.Height,
                        Colors = closest_icon_id.Colors,
                        Reserved2 = 0,
                        Planes = closest_icon_id.Planes,
                        BitsPerPixel = closest_icon_id.BitsPerPixel,
                        ImageSize = closest_icon_id.ImageSize,
                        Offset = Marshal.SizeOf(typeof(IcoHeader))
                    };

                    byte[] icoHeader = new byte[header.Offset];
                    GCHandle headerHandle = GCHandle.Alloc(icoHeader, GCHandleType.Pinned);
                    try
                    {
                        Marshal.StructureToPtr(header, headerHandle.AddrOfPinnedObject(), true);
                    }
                    finally
                    {
                        headerHandle.Free();
                    }

                    icons.Add(new IconResource(Convert.ToString(idx, 16), icoHeader.Concat(data).ToArray(), IconType.ICO, header.Width, header.Height));
                }
            }

            return icons;
        }
    }
}
