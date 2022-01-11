using UnityEngine;
using Verse;

namespace Micro
{
    internal class Micro_Mod : Mod
    {
        public Micro_Mod(ModContentPack content) : base(content)
        {
            Micro_Mod.Settings = base.GetSettings<Micro_Setting>();
        }

        public override string SettingsCategory()
        {
            return "Micro_Setting";
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Micro_Mod.Settings.DoSettingsWindowContents(inRect);
        }

        // Token: 0x04000001 RID: 1
        public static Micro_Setting Settings;
    }
}