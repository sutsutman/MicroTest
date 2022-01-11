using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Micro
{
    [DefOf]
    public static class BoosterActivedDefOf
    {
        public static ThingDef Mincho_SpaceBooster;

        public static StatDef Mincho_SpaceBoosterSkipRadius;

        public static JobDef Micro_CastSkip;

        public static ThingDef Micro_PawnSkipper;
    }

    //comps of the pack
    public class Mincho_Dodge : Verb
    {
        public static IntVec3 Mincho_DodgeTarget;

        private float cachedEffectiveRange = -1f;

        protected override float EffectiveRange
        {
            get
            {
                //This part makes the code get the defs for Mincho_SpaceBooster
                if ((double)this.cachedEffectiveRange < 0.0)
                {
                    this.cachedEffectiveRange = this.EquipmentSource.GetStatValue(BoosterActivedDefOf.Mincho_SpaceBoosterSkipRadius);
                }
                return this.cachedEffectiveRange;
            }
        }

        public override bool MultiSelect => true;

        protected override bool TryCastShot()
        {
            CompReloadable reloadableCompSource = this.ReloadableCompSource;
            Pawn casterPawn = this.CasterPawn;
            //it is changed to fix a bug that if it has 0 fuel it cannot run
            if (casterPawn == null || reloadableCompSource == null || !reloadableCompSource.CanBeUsed)
            {
                return false;
            }
            IntVec3 cell = this.currentTarget.Cell;
            Map map = casterPawn.Map;
            ReloadableCompSource.UsedOnce();
            if (Micro_Setting.BoosterRot)
            {
                casterPawn.rotationTracker.FaceCell(cell);
            }
            //This part was changed makes the code idenpendent from Royalty
            PawnFlyer newThing = PawnFlyer.MakeFlyer(BoosterActivedDefOf.Micro_PawnSkipper, casterPawn, cell);

            if (newThing == null)
            {
                return false;
            }
            GenSpawn.Spawn((Thing)newThing, cell, map);
            return true;
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            Map map = this.CasterPawn.Map;
            IntVec3 intVec3 = RCellFinder.BestOrderedGotoDestNear(target.Cell, this.CasterPawn, new Predicate<IntVec3>(AcceptableDestination));


            //reloadableCompSource.UsedOnce(); is moved from TryCastShot to here to balance in case player tries to cheat the system by repetedly skip but than cancle the action
            Mincho_DodgeTarget = intVec3;
            //This part were changed makes the code idenpendent from Royalty
            Job job = JobMaker.MakeJob(BoosterActivedDefOf.Micro_CastSkip, (LocalTargetInfo)intVec3);
            job.verbToUse = (Verb)this;
            if (!this.CasterPawn.jobs.TryTakeOrderedJob(job))
            {
                return;
            }
            FleckMaker.Static(intVec3, map, FleckDefOf.FeedbackGoto);

            bool AcceptableDestination(IntVec3 c) => Mincho_Dodge.ValidJumpTarget(map, c) && this.CanHitTargetFrom(this.caster.Position, (LocalTargetInfo)c);
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true) => this.caster != null && this.CanHitTarget(target) && Mincho_Dodge.ValidJumpTarget(this.caster.Map, target.Cell) && ReloadableUtility.CanUseConsideringQueuedJobs(this.CasterPawn, this.EquipmentSource);

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            float num = this.EffectiveRange * this.EffectiveRange;
            IntVec3 cell = targ.Cell;
            return (double)this.caster.Position.DistanceToSquared(cell) <= (double)num && GenSight.LineOfSight(root, cell, this.caster.Map);
        }

        public override void OnGUI(LocalTargetInfo target)
        {
            if (this.CanHitTarget(target) && Mincho_Dodge.ValidJumpTarget(this.caster.Map, target.Cell))
            {
                base.OnGUI(target);
            }
            else
            {
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
            }
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (target.IsValid && Mincho_Dodge.ValidJumpTarget(this.caster.Map, target.Cell))
            {
                GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
            }
            GenDraw.DrawRadiusRing(this.caster.Position, this.EffectiveRange, Color.white, (Func<IntVec3, bool>)(c => GenSight.LineOfSight(this.caster.Position, c, this.caster.Map) && Mincho_Dodge.ValidJumpTarget(this.caster.Map, c)));
        }

        public static bool ValidJumpTarget(Map map, IntVec3 cell)
        {
            if (!cell.IsValid || !cell.InBounds(map) || cell.Impassable(map) || !cell.Walkable(map) || cell.Fogged(map))
            {
                return false;
            }
            Building edifice = cell.GetEdifice(map);
            return edifice == null || !(edifice is Building_Door buildingDoor) || buildingDoor.Open;
        }
    }

    //Job_Drivers
    public class Micro_CastSkip : JobDriver_CastVerbOnceStatic
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            this.pawn.Map.pawnDestinationReservationManager.Reserve(this.pawn, this.job, this.job.targetA.Cell);
            //This snippet of code was added to make the action happen instantanous
            pawn.stances.CancelBusyStanceHard();
            return true;
        }
    }

    //Player flyer MicroTest_CastSkipper
    public class Micro_Skipper : PawnFlyer
    {
        private static float Linermotion(float x) { return 0; }
        private static readonly Func<float, float> FlightSpeed;
        private static readonly Func<float, float> FlightCurveHeight = new Func<float, float>(Micro_Skipper.Linermotion);
        private Material cachedShadowMaterial;
        private Effecter flightEffecter;
        private int positionLastComputedTick = -1;
        private Vector3 groundPos;
        private Vector3 effectivePos;
        private float effectiveHeight;

        private Material ShadowMaterial
        {
            get
            {
                if ((UnityEngine.Object)this.cachedShadowMaterial == (UnityEngine.Object)null && !this.def.pawnFlyer.shadow.NullOrEmpty())
                {
                    this.cachedShadowMaterial = MaterialPool.MatFrom(this.def.pawnFlyer.shadow, ShaderDatabase.Transparent);
                }
                return this.cachedShadowMaterial;
            }
        }

        static Micro_Skipper()
        {
            AnimationCurve animationCurve = new AnimationCurve();
            animationCurve.AddKey(0.0f, 0.0f);
            animationCurve.AddKey(0.1f, 0.15f);
            animationCurve.AddKey(1f, 1f);
            Micro_Skipper.FlightSpeed = new Func<float, float>(animationCurve.Evaluate);
        }

        public override Vector3 DrawPos
        {
            get
            {
                this.RecomputePosition();
                return this.effectivePos;
            }
        }

        protected override bool ValidateFlyer() => true;

        private void RecomputePosition()
        {
            if (this.positionLastComputedTick == this.ticksFlying)
            {
                return;
            }
            this.positionLastComputedTick = this.ticksFlying;
            float num = (float)this.ticksFlying / (float)this.ticksFlightTime;
            float t = Micro_Skipper.FlightSpeed(num);
            this.effectiveHeight = Micro_Skipper.FlightCurveHeight(t);
            this.groundPos = Vector3.Lerp(this.startVec, this.DestinationPos, t);
            Vector3 vector3_1 = new Vector3(0.0f, 0.0f, 2f);
            Vector3 vector3_2 = Altitudes.AltIncVect * this.effectiveHeight;
            double effectiveHeight = (double)this.effectiveHeight;
            Vector3 vector3_3 = vector3_1 * (float)effectiveHeight;
            this.effectivePos = this.groundPos + vector3_2 + vector3_3;
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            this.RecomputePosition();
            this.DrawShadow(this.groundPos, this.effectiveHeight);
            this.FlyingPawn.DrawAt(this.effectivePos, flip);
        }

        private void DrawShadow(Vector3 drawLoc, float height)
        {
            Material shadowMaterial = this.ShadowMaterial;
            if ((UnityEngine.Object)shadowMaterial == (UnityEngine.Object)null)
            {
                return;
            }
            float num = Mathf.Lerp(1f, 0.6f, height);
            Vector3 s = new Vector3(num, 1f, num);
            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetTRS(drawLoc, Quaternion.identity, s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMaterial, 0);
        }

        protected override void RespawnPawn()
        {
            this.LandingEffects();
            base.RespawnPawn();
        }

        public override void Tick()
        {
            if (this.flightEffecter == null && this.def.pawnFlyer.flightEffecterDef != null)
            {
                this.flightEffecter = this.def.pawnFlyer.flightEffecterDef.Spawn();
                this.flightEffecter.Trigger((TargetInfo)(Thing)this, TargetInfo.Invalid);
            }
            else
            {
                this.flightEffecter?.EffectTick((TargetInfo)(Thing)this, TargetInfo.Invalid);
            }
            base.Tick();
        }

        private void LandingEffects()
        {
            if (this.def.pawnFlyer.soundLanding != null)
            {
                this.def.pawnFlyer.soundLanding.PlayOneShot((SoundInfo)new TargetInfo(this.Position, this.Map));
            }
            FleckMaker.ThrowDustPuff(this.DestinationPos + Gen.RandomHorizontalVector(0.5f), this.Map, 2f);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            this.flightEffecter?.Cleanup();
            base.Destroy(mode);
        }
    }
}