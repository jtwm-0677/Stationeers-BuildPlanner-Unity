using System.Collections.Generic;

namespace StationeersBuildPlanner.Editor
{
    /// <summary>
    /// Maps mesh names to their corresponding texture names.
    /// Stationeers uses a paintable material system where mesh names don't match texture names.
    /// </summary>
    public static class TextureMappings
    {
        /// <summary>
        /// Maps mesh name patterns to texture names.
        /// Key: mesh name or pattern (supports StartsWith matching)
        /// Value: texture name (without extension)
        /// </summary>
        public static readonly Dictionary<string, string> MeshToTexture = new Dictionary<string, string>
        {
            // Steel Frames - build states
            { "WallFrame0", "StructureFrame_BuildState0" },
            { "WallFrame1", "StructureFrame_BuildState1" },
            { "WallFrame2", "StructureFrame_BuildState2" },
            { "WallFrame3", "StructureFrame" },  // Completed state

            // Iron Frames - build states
            { "WallFrameIron0", "StructureFrameIron_BuildState0" },
            { "WallFrameIron1", "StructureFrameIron_BuildState1" },
            { "WallFrameIron2", "StructureFrameIron" },  // Completed state

            // Frame variants
            { "StructureFrameCorner", "StructureFrameCorner" },
            { "StructureFrameCornerCut", "StructureFrameCornerCut" },
            { "StructureFrameSide", "StructureFrameSide" },
            { "StructureFrame", "StructureFrame" },
            { "StructureFrameIron", "StructureFrameIron" },

            // A-Frames
            { "AFrameStripes", "AFrameStripes" },
            { "AFrameWIP", "AFrameWIP" },
            { "DynamicAFrameStripes", "AFrameStripes" },
            { "DynamicAFrameWIP", "AFrameWIP" },

            // Item versions (inventory items)
            { "ItemSteelFrames", "ItemSteelFrames" },
            { "ItemIronFrames", "ItemIronFrames" },
            { "ItemIronWallFrames", "ItemIronWallFrames" },
            { "ItemWallFrames", "ItemWallFrames" },
        };

        /// <summary>
        /// Pattern-based mappings for common prefixes.
        /// These are checked if no exact match is found.
        /// </summary>
        public static readonly Dictionary<string, string> PatternMappings = new Dictionary<string, string>
        {
            // Structure prefixes - try to find matching texture with same name
            { "Structure", "" },  // Empty means use mesh name as texture name
            { "Item", "" },
            { "Dynamic", "" },
        };

        /// <summary>
        /// Get the texture name for a given mesh name.
        /// Returns null if no mapping found.
        /// </summary>
        public static string GetTextureForMesh(string meshName)
        {
            // Remove common suffixes
            string cleanName = meshName
                .Replace("_outline", "")
                .Replace("Destroyed", "");

            // Try exact match first
            if (MeshToTexture.TryGetValue(cleanName, out string textureName))
            {
                return textureName;
            }

            // Try StartsWith matching on the mapping keys
            foreach (var kvp in MeshToTexture)
            {
                if (cleanName.StartsWith(kvp.Key))
                {
                    return kvp.Value;
                }
            }

            // Fallback: use the clean mesh name as texture name
            return cleanName;
        }
    }
}
