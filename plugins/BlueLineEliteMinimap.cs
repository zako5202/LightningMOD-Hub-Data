using System;
using System.Collections.Generic;
using System.Linq;
using Turbo.Plugins;
using Turbo.Plugins.Default;
using SharpDX;

namespace Turbo.Plugins.RNN
{

    public class BlueLineEliteMinimap : BasePlugin, IInGameTopPainter
    {
        public const string Version = "9990.999.999";
        
        public float MainScreenThickness { get; set; } = 6f;
        public float MinimapThickness { get; set; } = 2f;

        public float MaxLinkDistance { get; set; } = 9999f;

        private readonly Color[] PackColors = new Color[]
        {
            Color.Blue,
            Color.Red,
            Color.LimeGreen,
            Color.Orange,
            Color.Purple,
            Color.Cyan,
            Color.Magenta,
            Color.Yellow,
            new Color(255, 128, 0),
            new Color(128, 0, 255),
            new Color(0, 255, 128)
        };

        private Dictionary<int, IBrush> PackBrushesMain { get; set; }
        private Dictionary<int, IBrush> PackBrushesMinimap { get; set; }

        private IFont MinimapDebugFont { get; set; }

        public BlueLineEliteMinimap()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);
            PackBrushesMain = new Dictionary<int, IBrush>();
            PackBrushesMinimap = new Dictionary<int, IBrush>();

            MinimapDebugFont = Hud.Render.CreateFont("arial", 12, 255, 255, 255, 255, false, false, false);
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.BeforeClip) return;
            if (Hud.Game.IsInTown) return;

            DrawLines(false);
            DrawLines(true);
        }

        private void DrawLines(bool onMinimap)
        {
            var sortedPacks = Hud.Game.MonsterPacks
                .OrderBy(p => p.MonstersAlive.FirstOrDefault()?.FloorCoordinate.X ?? 0)
                .ThenBy(p => p.MonstersAlive.FirstOrDefault()?.FloorCoordinate.Y ?? 0)
                .ToList();

            for (int index = 0; index < sortedPacks.Count; index++)
            {
                var pack = sortedPacks[index];

                // ⭐ FILTER: remove illusion clones for ALL elite types
                var elites = pack.MonstersAlive
                    .Where(m =>
                        (m.Rarity == ActorRarity.Champion ||
                         m.Rarity == ActorRarity.Rare ||
                         m.Rarity == ActorRarity.Boss)
                        &&
                        // ⭐ Skip illusion clones ONLY
                        !(m.AffixSnoList.Any(a => a.Affix == MonsterAffix.Illusionist) &&
                          m.SummonerAcdDynamicId != 0)
                    )
                    .ToList();

                if (elites.Count < 2)
                    continue;

                int packId = Math.Abs(pack.GetHashCode());

                var brushes = onMinimap ? PackBrushesMinimap : PackBrushesMain;
                float thickness = onMinimap ? MinimapThickness : MainScreenThickness;

                if (!brushes.TryGetValue(packId, out var brush))
                {
                    int colorIndex = index % PackColors.Length;
                    var color = PackColors[colorIndex];

                    brush = Hud.Render.CreateBrush(255, color.R, color.G, color.B, thickness);
                    brushes[packId] = brush;
                }

                DrawMST(elites, brush, onMinimap);
            }

            if (onMinimap)
            {
                var mapRect = Hud.Render.MinimapUiElement.Rectangle;
                MinimapDebugFont.DrawText("", mapRect.Left + 10, mapRect.Top + 10);
            }
        }

        private void DrawMST(List<IMonster> elites, IBrush brush, bool onMinimap)
        {
            if (elites.Count < 2) return;

            var connected = new HashSet<IMonster>();
            var unconnected = new HashSet<IMonster>(elites);

            var current = elites[0];
            connected.Add(current);
            unconnected.Remove(current);

            while (unconnected.Count > 0)
            {
                float minDistance = float.MaxValue;
                IMonster nearestFrom = null;
                IMonster nearestTo = null;

                foreach (var c in connected)
                {
                    foreach (var u in unconnected)
                    {
                        float dx = c.FloorCoordinate.X - u.FloorCoordinate.X;
                        float dy = c.FloorCoordinate.Y - u.FloorCoordinate.Y;
                        float distance = dx * dx + dy * dy;

                        if (distance < minDistance && distance <= MaxLinkDistance * MaxLinkDistance)
                        {
                            minDistance = distance;
                            nearestFrom = c;
                            nearestTo = u;
                        }
                    }
                }

                if (nearestFrom != null && nearestTo != null)
                {
                    DrawLineBetween(nearestFrom.FloorCoordinate, nearestTo.FloorCoordinate, brush, onMinimap);
                    connected.Add(nearestTo);
                    unconnected.Remove(nearestTo);
                }
                else
                {
                    break;
                }
            }
        }

        private void DrawLineBetween(IWorldCoordinate c1, IWorldCoordinate c2, IBrush brush, bool onMinimap)
        {
            if (onMinimap)
            {
                Hud.Render.GetMinimapCoordinates(c1.X, c1.Y, out float x1, out float y1);
                Hud.Render.GetMinimapCoordinates(c2.X, c2.Y, out float x2, out float y2);

                brush.DrawLine(x1, y1, x2, y2);
                brush.DrawLine(x1, y1, x2, y2);
            }
            else
            {
                var sc1 = c1.ToScreenCoordinate();
                var sc2 = c2.ToScreenCoordinate();

                brush.DrawLine(sc1.X, sc1.Y, sc2.X, sc2.Y);
                brush.DrawLine(sc1.X, sc1.Y, sc2.X, sc2.Y);
            }
        }
    }
}
