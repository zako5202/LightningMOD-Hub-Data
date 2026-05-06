using System.Linq;
using Turbo.Plugins.Default;

namespace Turbo.Plugins.RNN
{
    public class NecroCorpseCounter : BasePlugin, IInGameWorldPainter
    {
        public IFont Font { get; set; }
        public IBrush BackgroundBrush { get; set; }
        public IBrush BorderBrush { get; set; }

        public float CorpseRange { get; set; } = 60f;

        public NecroCorpseCounter()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            // Same compact font as your Siphon Blood box
            Font = Hud.Render.CreateFont("tahoma", 9, 255, 255, 200, 50, true, false, true);
            Font.SetShadowBrush(255, 0, 0, 0, true);

            // Same box style
            BackgroundBrush = Hud.Render.CreateBrush(160, 0, 0, 0, 0);
            BorderBrush = Hud.Render.CreateBrush(200, 255, 200, 50, 1);
        }

        public void PaintWorld(WorldLayer layer)
        {
            if (layer != WorldLayer.Ground)
                return;

            var me = Hud.Game.Me;
            if (me == null)
                return;

            if (me.HeroClassDefinition.HeroClass != HeroClass.Necromancer)
                return;

            // Only show if corpse‑using skills are equipped
            bool hasCorpseSkill =
                me.Powers.UsedSkills.Any(s =>
                    s.SnoPower.Sno == Hud.Sno.SnoPowers.Necromancer_Devour.Sno ||
                    s.SnoPower.Sno == Hud.Sno.SnoPowers.Necromancer_CorpseLance.Sno
                );

            if (!hasCorpseSkill)
                return;

            // Count corpses
            int corpseCount = Hud.Game.Actors
                .Where(a => a.SnoActor != null &&
                            a.SnoActor.Sno == ActorSnoEnum._p6_necro_corpse_flesh &&
                            a.CentralXyDistanceToMe <= CorpseRange)
                .Count();

            if (corpseCount <= 0)
                return;

            // Build text
            string text = $"C: {corpseCount}";
            var layout = Font.GetTextLayout(text);

            // Pulse when corpses are low (1–2)
            if (corpseCount > 0 && corpseCount < 3)
            {
                float pulse = (float)(0.5 + 0.5 * System.Math.Sin(Hud.Game.CurrentGameTick / 5.0));
                int r = (int)(255 * pulse);
                int g = (int)(200 * pulse);
                int b = 50;

                BorderBrush = Hud.Render.CreateBrush(255, r, g, b, 1);
            }
            else
            {
                BorderBrush = Hud.Render.CreateBrush(200, 255, 200, 50, 1);
            }

            // Screen position (same style as your Siphon Blood offset)
            var screen = me.FloorCoordinate.ToScreenCoordinate();
            float x = screen.X - 120f;
            float y = screen.Y + 110f;

            float w = layout.Metrics.Width + 6f;
            float h = layout.Metrics.Height + 4f;

            float left = x - w / 2f;
            float top = y;

            // Draw box
            BackgroundBrush.DrawRectangle(left, top, w, h);
            BorderBrush.DrawRectangle(left, top, w, h);

            // Draw text
            Font.DrawText(layout, x - layout.Metrics.Width / 2f, y + 2f);
        }
    }
}
