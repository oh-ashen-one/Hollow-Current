using System;
using System.Collections.Generic;

namespace HollowCurrent
{
    // Every playable map. Values are persisted (PlayerPrefs "hc.map", WorldLayout),
    // so append new maps at the end and never renumber.
    public enum MapId { Bellwether, Castle }

    public sealed class MapInfo
    {
        public readonly MapId Id;
        // Scene file name without extension; also the runtime scene name.
        public readonly string SceneName;
        // Title-screen name of the map and the name of its hunter.
        public readonly string DisplayName, HunterName;
        // Where Prepare persists generated meshes, materials and reflection cubemaps.
        // Bellwether keeps the historical root folder so its saved scene references stay valid.
        public readonly string LayoutFolder;
        public MapInfo(MapId id,string sceneName,string displayName,string hunterName,string layoutFolder)
        {Id=id;SceneName=sceneName;DisplayName=displayName;HunterName=hunterName;LayoutFolder=layoutFolder;}
        public string ScenePath => MapCatalog.SceneFolder+"/"+SceneName+".unity";
        public string BakeReceiptPath => MapCatalog.LayoutRoot+"/"+SceneName+"Bake.json";
        public string LightingSettingsPath => MapCatalog.LayoutRoot+"/"+SceneName+"Lighting.lighting";
        public override string ToString()=>Id.ToString();
    }

    // The only place scene names, bake receipts and map display names are spelled.
    public static class MapCatalog
    {
        public const string SceneFolder="Assets/HollowCurrent/Scenes";
        public const string LayoutRoot="Assets/HollowCurrent/Art/LayoutData";
        public const string PreferenceKey="hc.map";
        static readonly MapInfo[] maps=
        {
            new MapInfo(MapId.Bellwether,"Bellwether","The Bellwether House","Split Grin",LayoutRoot),
            new MapInfo(MapId.Castle,"Castle","Haunted Castle","Count Claude",LayoutRoot+"/Castle"),
        };
        public static IReadOnlyList<MapInfo> All => maps;
        public static MapInfo Get(MapId id)
        {
            foreach(var map in maps)if(map.Id==id)return map;
            throw new ArgumentOutOfRangeException(nameof(id),id,"Unknown map");
        }
        public static bool TryParse(string value,out MapId id)
        {
            id=MapId.Bellwether;
            if(string.IsNullOrEmpty(value))return false;
            foreach(var map in maps)
                if(string.Equals(map.Id.ToString(),value,StringComparison.OrdinalIgnoreCase)||string.Equals(map.SceneName,value,StringComparison.OrdinalIgnoreCase))
                {id=map.Id;return true;}
            return false;
        }
        public static bool TryFromSceneName(string sceneName,out MapId id)
        {
            id=MapId.Bellwether;
            foreach(var map in maps)if(string.Equals(map.SceneName,sceneName,StringComparison.Ordinal)){id=map.Id;return true;}
            return false;
        }
        // Stored preference as an int; out-of-range or missing values fall back to Bellwether.
        public static MapId FromPreference(int stored)=>stored>=0&&stored<maps.Length?(MapId)stored:MapId.Bellwether;
    }
}
