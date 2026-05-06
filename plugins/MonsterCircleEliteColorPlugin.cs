using Turbo.Plugins.Default;
using System.Linq;

namespace Turbo.Plugins.RNN
{
    public class MonsterCircleEliteColorPlugin : BasePlugin, IInGameWorldPainter
    {
        private IBrush BlueRing;
        private IBrush YellowRing;
        private IBrush RedRing;
        private IBrush PurpleRing;
        private IBrush HoverGreenRing;

        public MonsterCircleEliteColorPlugin()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            // ⭐ Solid, non-transparent rings (no fill)
            BlueRing    = Hud.Render.CreateBrush(255, 0, 160, 255, 7);   // Champion
            YellowRing  = Hud.Render.CreateBrush(255, 255, 255, 0, 7);   // Rare
            RedRing     = Hud.Render.CreateBrush(255, 255, 0, 0, 7);     // Juggernaut
            PurpleRing  = Hud.Render.CreateBrush(255, 255, 0, 255, 7);   // Boss

            // ⭐ Hover highlight ring
            HoverGreenRing = Hud.Render.CreateBrush(255, 50, 255, 50, 6);
        }

        private bool HasAffix(IMonster m, MonsterAffix afx)
        {
            return m.AffixSnoList.Any(a => a.Affix == afx);
        }

        public void PaintWorld(WorldLayer layer)
        {
            if (!Hud.Game.IsInGame || Hud.Game.Me.IsInTown)
                return;

            var hovered = Hud.Game.SelectedActor as IMonster;

            foreach (var monster in Hud.Game.AliveMonsters)
            {
                if (!monster.IsAlive)
                    continue;

                // Skip illusion clones
                if (HasAffix(monster, MonsterAffix.Illusionist) && monster.SummonerAcdDynamicId != 0)
                    continue;

                // ─────────────────────────────────────────────
                //   BOSS (Purple)
                // ─────────────────────────────────────────────
                if (monster.Rarity == ActorRarity.Boss)
                {
                    PurpleRing.DrawWorldEllipse(7f, -1, monster.FloorCoordinate);

                    if (hovered == monster)
                        HoverGreenRing.DrawWorldEllipse(7f, -1, monster.FloorCoordinate);

                    continue;
                }

                // ─────────────────────────────────────────────
                //   CHAMPION (Blue)
                // ─────────────────────────────────────────────
                if (monster.Rarity == ActorRarity.Champion)
                {
                    BlueRing.DrawWorldEllipse(6f, -1, monster.FloorCoordinate);

                    if (hovered == monster)
                        HoverGreenRing.DrawWorldEllipse(6f, -1, monster.FloorCoordinate);

                    continue;
                }

                // ─────────────────────────────────────────────
                //   RARE (Yellow or Red if Juggernaut)
                // ─────────────────────────────────────────────
                if (monster.Rarity == ActorRarity.Rare)
                {
                    if (HasAffix(monster, MonsterAffix.Juggernaut))
                    {
                        RedRing.DrawWorldEllipse(6f, -1, monster.FloorCoordinate);

                        if (hovered == monster)
                            HoverGreenRing.DrawWorldEllipse(6f, -1, monster.FloorCoordinate);
                    }
                    else
                    {
                        YellowRing.DrawWorldEllipse(6f, -1, monster.FloorCoordinate);

                        if (hovered == monster)
                            HoverGreenRing.DrawWorldEllipse(6f, -1, monster.FloorCoordinate);
                    }

                    continue;
                }

                // Rare Minions — no circle
                if (monster.Rarity == ActorRarity.RareMinion)
                    continue;
            }
        }
    }
}
