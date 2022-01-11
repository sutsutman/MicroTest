using UnityEngine;
using Verse;

namespace Micro
{
    [StaticConstructorOnStartup]
    public class Micro_Setting : ModSettings
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref BoosterRot, "ChangeBoosterRot", false);
        }
        public void DoSettingsWindowContents(Rect canvas)
        {
            Listing_Standard listing_Standard = new Listing_Standard
            {
                ColumnWidth = (canvas.width - 34f) / 2f
            };
            listing_Standard.Begin(canvas);

            listing_Standard.CheckboxLabeled("BoosterRot".Translate("Change BoosterRot"), ref BoosterRot, null);
            listing_Standard.Gap(10f);
            listing_Standard.GapLine(10f);

            //끝
            listing_Standard.End();
        }
        static Micro_Setting()
        {
            LongEventHandler.ExecuteWhenFinished(delegate ()
            { });
        }

        public static bool BoosterRot = false;
    }
}