using System.Linq;
using System.Collections.Generic;
using Turbo.Plugins.Default;

namespace Turbo.Plugins.RNN
{
    public class UnpullableAndBlockedElitePlugin : BasePlugin, IInGameWorldPainter
    {
        public GroundLabelDecorator NoPullDecorator { get; set; }
        public GroundLabelDecorator WallBlockedDecorator { get; set; }

        // Separate minimap decorators: red for unpullable, orange/yellow for wall-blocked
        public MapShapeDecorator MinimapUnpullableDecorator { get; set; }
        public MapShapeDecorator MinimapWallBlockedDecorator { get; set; }

        // Max distance (yards) to check wall/unpullable — elites beyond this are skipped
        public float MaxDetectionRange { get; set; } = 80f;

        // ⭐ Add ONLY elite SNOs here (use your SNO logger)
        public HashSet<uint> UnpullableEliteSNOs = new HashSet<uint>()
        {
            // Example:
            // 428763, // Executioner (elite)
            // 429112, // Soul Ripper (elite)
            // 430221, // Mallet Lord (elite)
            // 430555, // Wildwood Bulwark (elite)
        };

        // ⭐ Fallback names (used ONLY if SNO not known yet)
        private readonly List<string> UnpullableNames = new List<string>()
        {
            "mallet lord",
            "colossal golgor",
            "punisher",
            "armaddon",
            "sepsis the reckoner",
            "hellhide tremor",
            "maniacal golgor",
            "demonic tremor",
            "executioner",
            "manglemaw",
            "bloated malachor",
            "wildwood bulwark" // stays, but Juggernaut logic overrides it
        };

        public UnpullableAndBlockedElitePlugin()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            // Ground label for unpullable — uses built-in OffsetY (screen-space) instead of world coord shift
            NoPullDecorator = new GroundLabelDecorator(Hud)
            {
                TextFont = Hud.Render.CreateFont("tahoma", 7f, 255, 255, 60, 60, true, false, false),
                BackgroundBrush = Hud.Render.CreateBrush(160, 0, 0, 0, 0),
                BorderBrush = Hud.Render.CreateBrush(200, 255, 0, 0, 2),
                ForceOnScreen = true,
                CenterBaseLine = true,
                OffsetY = -30f,   // screen-space offset upward — reliable on any terrain
            };

            // Ground label for wall-blocked
            WallBlockedDecorator = new GroundLabelDecorator(Hud)
            {
                TextFont = Hud.Render.CreateFont("tahoma", 7f, 255, 255, 200, 60, true, false, false),
                BackgroundBrush = Hud.Render.CreateBrush(160, 0, 0, 0, 0),
                BorderBrush = Hud.Render.CreateBrush(200, 255, 200, 0, 2),
                ForceOnScreen = true,
                CenterBaseLine = true,
                OffsetY = -30f,
            };

            // Minimap: red circle for unpullable
            MinimapUnpullableDecorator = new MapShapeDecorator(Hud)
            {
                Brush = Hud.Render.CreateBrush(220, 255, 0, 0, 2),
                Radius = 6f,
                ShapePainter = new CircleShapePainter(Hud)
            };

            // Minimap: orange/yellow circle for wall-blocked
            MinimapWallBlockedDecorator = new MapShapeDecorator(Hud)
            {
                Brush = Hud.Render.CreateBrush(220, 255, 180, 0, 2),
                Radius = 6f,
                ShapePainter = new CircleShapePainter(Hud)
            };

        }

        public void PaintWorld(WorldLayer layer)
        {
            if (!Hud.Game.IsInGame || Hud.Game.Me.IsInTown)
                return;

            var player = Hud.Game.Me.FloorCoordinate;

            // Only collect wall actors once per frame, and only if there are monsters in range
            var wallActors = Hud.Game.Actors
                .Where(a => a.SnoActor.Sno == ActorSnoEnum._monsteraffix_waller_model)
                .ToList();

            foreach (var monster in Hud.Game.Monsters)
            {
                if (!monster.IsElite || !monster.IsAlive)
                    continue;

                // Distance cull — skip elites too far away to matter (uses cached property)
                if (monster.CentralXyDistanceToMe > MaxDetectionRange)
                    continue;

                bool isUnpullable = IsUnpullable(monster);
                bool wallBetween = IsWallBetween(player, monster.FloorCoordinate, wallActors);

                // Minimap markers — different colors for each case
                if (isUnpullable)
                    MinimapUnpullableDecorator.Paint(monster, monster.FloorCoordinate, null);
                else if (wallBetween)
                    MinimapWallBlockedDecorator.Paint(monster, monster.FloorCoordinate, null);

                // Ground layer painting
                if (layer == WorldLayer.Ground)
                {
                    if (isUnpullable)
                    {
                        NoPullDecorator.Paint(monster, monster.FloorCoordinate, "⛔");
                    }
                    else if (wallBetween)
                    {
                        WallBlockedDecorator.Paint(monster, monster.FloorCoordinate, "⚠");
                    }
                }
            }
        }

        // Cleaned-up helper: encapsulates all unpullable detection logic
        private bool IsUnpullable(IMonster monster)
        {
            // Minions and bosses are never marked unpullable
            if (monster.SummonerId != 0)
                return false;
            if (monster.Rarity == ActorRarity.Boss)
                return false;

            // Juggernauts can be pulled despite their size
            if (monster.AffixSnoList.Any(a => a.Affix == MonsterAffix.Juggernaut))
                return false;

            uint sno = monster.SnoMonster.Sno;

            // Primary: exact SNO match
            if (UnpullableEliteSNOs.Contains(sno))
                return true;

            // Fallback: localized name match (only when SNO not yet catalogued)
            string name = monster.SnoMonster.NameLocalized.ToLowerInvariant();
            return UnpullableNames.Contains(name);
        }

        private bool IsWallBetween(IWorldCoordinate player, IWorldCoordinate elite, IEnumerable<IActor> walls)
        {
            float dx = elite.X - player.X;
            float dy = elite.Y - player.Y;
            float lenSq = dx * dx + dy * dy;

            if (lenSq < 0.01f)
                return false;

            foreach (var wall in walls)
            {
                float t = ((wall.FloorCoordinate.X - player.X) * dx +
                           (wall.FloorCoordinate.Y - player.Y) * dy) / lenSq;

                if (t < 0 || t > 1) continue;

                float closestX = player.X + t * dx;
                float closestY = player.Y + t * dy;

                float distSq =
                    (wall.FloorCoordinate.X - closestX) * (wall.FloorCoordinate.X - closestX) +
                    (wall.FloorCoordinate.Y - closestY) * (wall.FloorCoordinate.Y - closestY);

                if (distSq <= 16f)
                    return true;
            }

            return false;
        }
    }
}