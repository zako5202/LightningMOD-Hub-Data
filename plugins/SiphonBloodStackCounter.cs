using Turbo.Plugins.Default;
using Turbo.Plugins.glq;
using System;

namespace Turbo.Plugins.RNN
{
    public class SiphonBloodStacksAndTimer : BasePlugin, IInGameWorldPainter
    {
        public IFont Font { get; set; }
        public IBrush BackgroundBrush { get; set; }
        public IBrush BorderBrush { get; set; }

        private readonly int[] PossibleIconIndexes = { 10, 1, 0 };

        public SiphonBloodStacksAndTimer()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            Font = Hud.Render.CreateFont("tahoma", 9, 255, 255, 80, 80, true, false, true);
            Font.SetShadowBrush(255, 0, 0, 0, true);

            BackgroundBrush = Hud.Render.CreateBrush(160, 0, 0, 0, 0);
            BorderBrush = Hud.Render.CreateBrush(200, 255, 80, 80, 1);
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

            int stacks = 0;
            double timeLeft = 0;

            foreach (var idx in PossibleIconIndexes)
            {
                stacks = PublicClassPlugin.GetBuffCount(
                    Hud,
                    Hud.Sno.SnoPowers.Necromancer_SiphonBlood.Sno,
                    idx
                );

                timeLeft = PublicClassPlugin.GetBuffLeftTime(
                    Hud,
                    Hud.Sno.SnoPowers.Necromancer_SiphonBlood.Sno,
                    idx
                );

                if (stacks > 0 || timeLeft > 0)
                    break;
            }

            if (stacks <= 0 && timeLeft <= 0)
                return;

            // Build text (two lines)
            string line1 = $"S: {stacks}";
            string line2 = (timeLeft > 0) ? timeLeft.ToString("0.0") + "s" : "∞";
            string fullText = line1 + "\n" + line2;

            var layout = Font.GetTextLayout(fullText);

            // Pulse border when < 1.0s
            if (timeLeft > 0 && timeLeft < 1.0)
            {
                float pulse = (float)(0.5 + 0.5 * Math.Sin(Hud.Game.CurrentGameTick / 5.0));
                int r = (int)(255 * pulse);
                int g = (int)(200 * pulse);
                int b = 80;
                BorderBrush = Hud.Render.CreateBrush(255, r, g, b, 1);
            }
            else
            {
                BorderBrush = Hud.Render.CreateBrush(200, 255, 80, 80, 1);
            }

            // ORIGINAL OFFSET (screen‑anchored)
            var screen = me.FloorCoordinate.ToScreenCoordinate();
            float x = screen.X - -120f;
            float y = screen.Y + 95f;

            float w = layout.Metrics.Width + 6f;
            float h = layout.Metrics.Height + 4f;

            float left = x - w / 2f;
            float top = y;

            // Box
            BackgroundBrush.DrawRectangle(left, top, w, h);
            BorderBrush.DrawRectangle(left, top, w, h);

            // Text
            Font.DrawText(layout, x - layout.Metrics.Width / 2f, y + 2f);
        }
    }
}
