using Server;
using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Spells;
using Server.Spells.Eighth;
using Server.Spells.Seventh;
using Server.Spells.Sixth;
using Server.Spells.Fifth;
using Server.Spells.Fourth;
using Server.Spells.Third;
using Server.Spells.Mysticism;
using Server.Spells.Spellweaving;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;
using Server.Targeting;

namespace Server.Engines.ArenaSystem
{
    public class ArenaRegion : BaseRegion
    {
        public PVPArena Arena { get; set; }

        public ArenaRegion(PVPArena arena)
            : base(String.Format("Duel Arena {0}", arena.Definition.Name),
                    arena.Definition.Map, 
                    Region.DefaultPriority, 
                    arena.Definition.RegionBounds)
        {
            Arena = arena;
        }

        public override bool OnDoubleClick(Mobile m, object o)
        {
            if (Arena.CurrentDuel != null)
            {
                var duel = Arena.CurrentDuel;

                if (o is BasePotion && duel.PotionRules != PotionRules.All)
                {
                    if (duel.PotionRules == PotionRules.None || o is BaseHealPotion)
                    {
                        return false;
                    }
                }
            }

            if (o is Corpse && ((Corpse)o).Owner != m)
            {
                m.SendLocalizedMessage(1010054); // You cannot loot that corpse!!
                return false;
            }

            return base.OnDoubleClick(m, o);
        }

        public override bool AllowFlying(Mobile from)
        {
            if (Arena.CurrentDuel != null && !Arena.CurrentDuel.RidingFlyingAllowed)
            {
                return false;
            }

            return base.AllowFlying(from);
        }

        public override bool OnBeginSpellCast(Mobile m, ISpell spell)
        {
            if (Arena.CurrentDuel != null)
            {
                var duel = Arena.CurrentDuel;

                if (duel.InPreFight)
                {
                    m.SendLocalizedMessage(502629); // You cannot cast spells here.
                    return false;
                }

                if (!duel.RidingFlyingAllowed && spell is FlySpell)
                {
                    m.SendLocalizedMessage(1115997); // The rules prohibit riding a mount or flying.
                    return false;
                }

                if(!duel.FieldSpellsAllowed && (spell is FireFieldSpell || spell is ParalyzeFieldSpell || spell is PoisonFieldSpell || spell is EnergyFieldSpell
                    || spell is WallOfStoneSpell))
                {
                    m.SendLocalizedMessage(1010391); // A magical aura surrounds you and prevents the spell.
                    return false;
                }
            }

            return base.OnBeginSpellCast(m, spell);
        }

        public override bool OnTarget(Mobile m, Target t, object o)
        {
            ArenaDuel duel = Arena.CurrentDuel;

            if (t is TeleportSpell.InternalTarget && Region.Find(m.Location, m.Map) != this)
            {
                m.SendLocalizedMessage(501035); // You cannot teleport from here to the destination.
                return false;
            }

            return base.OnTarget(m, t, o);
        }

        public override bool CheckTravel(Mobile traveller, Point3D p, TravelCheckType type)
        {
            return type > TravelCheckType.Mark;
        }

        public override void OnDeath(Mobile m)
        {
            if (Arena != null && Arena.CurrentDuel != null && Arena.CurrentDuel.HasBegun && !Arena.CurrentDuel.InPreFight)
            {
                Arena.CurrentDuel.HandleDeath(m);
            }

            base.OnDeath(m);
        }

        public override bool AllowHarmful(Mobile from, IDamageable target)
        {
            if (Arena != null && Arena.CurrentDuel != null && Arena.CurrentDuel.Complete)
            {
                return false;
            }

            return true;
        }

        public override bool OnResurrect(Mobile m)
        {
            bool res = base.OnResurrect(m);

            if (Arena != null)
            {
                Timer.DelayCall<Mobile>(TimeSpan.FromSeconds(.2), mob => Arena.RemovePlayer(mob), m);
            }

            return res;
        }

        public bool AllowItemEquip(PlayerMobile pm, Item item)
        {
            ArenaDuel duel = Arena.CurrentDuel;

            if (duel != null && !duel.RangedWeaponsAllowed && item is BaseRanged)
            {
                pm.SendLocalizedMessage(1115996); // The rules prohibit the use of ranged weapons!
                return false;
            }

            return true;
        }
    }
}