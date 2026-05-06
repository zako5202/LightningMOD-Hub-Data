using Turbo.Plugins.Default;
using System.Linq;
using SharpDX;
using System;

namespace Turbo.Plugins.RNN
{
    public class SyphonBloodTargetVisible : BasePlugin, IInGameTopPainter
    {
        public IBrush FillWhite      { get; set; }
        public IBrush FillGreen      { get; set; }
        public IBrush FillRed        { get; set; }
        public IBrush FillOtherNecro { get; set; }
        public IBrush OutlineBlack   { get; set; }

        public IFont LabelFont       { get; set; }

        public bool OnlyElites       { get; set; } = true;
        public int  OffsetY          { get; set; } = -140;
        public float PulseSpeed      { get; set; } = 0.04f;  // lite snabbare än tidigare för att kännas levande

        public SyphonBloodTargetVisible()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            FillWhite      = Hud.Render.CreateBrush(220, 220, 220, 220, 6f);
            FillGreen      = Hud.Render.CreateBrush(255, 40, 220, 100, 7f);
            FillRed        = Hud.Render.CreateBrush(255, 220, 60, 60, 8f);
            FillOtherNecro = Hud.Render.CreateBrush(255, 120, 180, 255, 7f);

            OutlineBlack = Hud.Render.CreateBrush(180, 0, 0, 0, 9f);

            LabelFont = Hud.Render.CreateFont(
                "arial", 9f,
                255, 255, 255, 220,
                true, false,
                0, 0, 0, 140,
                true
            );
        }

        private void DrawMarker(float x, float y, IBrush brush, bool is600, bool isPrimary, long gameTick)
        {
            // Mycket subtil puls: bara ±2–3 pixlar förändring i storlek
            float pulse = 0.94f + 0.12f * (float)Math.Sin(gameTick * PulseSpeed);

            float baseSize = isPrimary ? 50f : (is600 ? 45f : 35f);
            float size = baseSize * pulse;

            float top   = y - size * 0.5f;
            float mid   = y + size * 0.15f;
            float bot   = y + size * 0.55f;

            float left  = x - size * 0.3f;
            float right = x + size * 0.3f;

            // Outline
            OutlineBlack.DrawLine(x, top, x, mid);
            OutlineBlack.DrawLine(left, mid, x, bot);
            OutlineBlack.DrawLine(right, mid, x, bot);

            // Färgad pil
            brush.DrawLine(x, top, x, mid);
            brush.DrawLine(left, mid, x, bot);
            brush.DrawLine(right, mid, x, bot);

            // Text
            string text = is600 ? "600%" : "SB";
            LabelFont.DrawText(text, x - 20, top - 18);
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.BeforeClip) return;
            if (!Hud.Game.IsInGame || Hud.Game.IsInTown) return;

            var necrosChanneling = Hud.Game.Players
                .Where(p => p.HeroClassDefinition?.HeroClass == HeroClass.Necromancer &&
                            p.Powers.BuffIsActive(453563) &&
                            !p.IsDead)
                .ToList();

            if (necrosChanneling.Count == 0) return;

            var me = Hud.Game.Me;
            bool meChanneling = me.Powers.BuffIsActive(453563);

            bool hasOtherNecro = necrosChanneling.Count > (meChanneling ? 1 : 0);

            var targets = Hud.Game.AliveMonsters
                .Where(m => (!OnlyElites || m.IsElite) &&
                            m.GetAttributeValue(Hud.Sno.Attributes.Power_Buff_9_Visual_Effect_D, 453563) == 1)
                .ToList();

            long currentTick = Hud.Game.CurrentGameTick;

            foreach (var mon in targets)
            {
                var pos = mon.FloorCoordinate.ToScreenCoordinate();

                if (pos.X < -50 || pos.Y < -50 ||
                    pos.X > Hud.Window.Size.Width + 50 ||
                    pos.Y > Hud.Window.Size.Height + 50)
                    continue;

                float sx = pos.X;
                float sy = pos.Y + OffsetY;

                bool is600 = mon.GetAttributeValue(Hud.Sno.Attributes.Power_Buff_11_Visual_Effect_D, 453563) == 1;
                bool isMyPrimary = meChanneling && mon.IsSelected;

                IBrush brush;
                if (is600)
                {
                    brush = FillGreen;
                }
                else if (isMyPrimary)
                {
                    brush = FillRed;
                }
                else if (hasOtherNecro)
                {
                    brush = FillOtherNecro;
                }
                else
                {
                    brush = FillWhite;
                }

                DrawMarker(sx, sy, brush, is600, isMyPrimary, currentTick);
            }
        }
    }
}