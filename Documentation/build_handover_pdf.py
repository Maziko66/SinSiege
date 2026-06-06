# -*- coding: utf-8 -*-
"""
Generates the SinSiege Developer Handover PDF.
Run:  python build_handover_pdf.py
Output: SinSiege_Developer_Handover.pdf  (same folder)
"""

import os
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.platypus import (
    BaseDocTemplate, PageTemplate, Frame, Paragraph, Spacer, Preformatted,
    Table, TableStyle, PageBreak, NextPageTemplate, KeepTogether
)
from reportlab.platypus.tableofcontents import TableOfContents
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
import os as _os
_symp = r'C:\Windows\Fonts\seguisym.ttf'
pdfmetrics.registerFont(TTFont('Sym', _symp if _os.path.exists(_symp) else r'C:\Windows\Fontsrial.ttf'))

# ----------------------------------------------------------------------------
# Palette
# ----------------------------------------------------------------------------
CRIMSON   = colors.HexColor("#8B1E2D")
CRIMSON_L = colors.HexColor("#A93343")
SLATE     = colors.HexColor("#1F2933")
SLATE_MED = colors.HexColor("#3E4C59")
GREY      = colors.HexColor("#5C6773")
LIGHT     = colors.HexColor("#9AA5B1")
RULE      = colors.HexColor("#D9DCE1")

CODE_BG   = colors.HexColor("#F5F5F8")
CODE_BD   = colors.HexColor("#D4D4DC")

INFO_BG   = colors.HexColor("#EAF1FB"); INFO_BD = colors.HexColor("#3F6FB5")
WARN_BG   = colors.HexColor("#FBEBE9"); WARN_BD = colors.HexColor("#C0392B")
TIP_BG    = colors.HexColor("#E9F7EF"); TIP_BD  = colors.HexColor("#1E8449")

TBL_HEAD  = colors.HexColor("#2D3A45")
TBL_ALT   = colors.HexColor("#F2F3F6")

# ----------------------------------------------------------------------------
# Styles
# ----------------------------------------------------------------------------
styles = getSampleStyleSheet()

def S(name, **kw):
    styles.add(ParagraphStyle(name, **kw))

S("CoverTitle", fontName="Helvetica-Bold", fontSize=40, leading=44,
  textColor=CRIMSON, alignment=TA_CENTER, spaceAfter=6)
S("CoverSub", fontName="Helvetica", fontSize=15, leading=20,
  textColor=SLATE, alignment=TA_CENTER, spaceAfter=4)
S("CoverMeta", fontName="Helvetica", fontSize=10.5, leading=16,
  textColor=GREY, alignment=TA_CENTER)

S("H1", fontName="Helvetica-Bold", fontSize=20, leading=24, textColor=CRIMSON,
  spaceBefore=10, spaceAfter=8, keepWithNext=True)
S("H2", fontName="Helvetica-Bold", fontSize=14.5, leading=18, textColor=SLATE,
  spaceBefore=12, spaceAfter=5, keepWithNext=True)
S("H3", fontName="Helvetica-Bold", fontSize=11.5, leading=15, textColor=CRIMSON_L,
  spaceBefore=9, spaceAfter=3, keepWithNext=True)

S("Body", fontName="Helvetica", fontSize=9.7, leading=14.5, textColor=SLATE_MED,
  spaceAfter=6, alignment=TA_LEFT)
S("BodyTight", parent=styles["Body"], spaceAfter=2)
S("LiBullet", parent=styles["Body"], leftIndent=14, bulletIndent=4, spaceAfter=3)
S("LiBullet2", parent=styles["Body"], leftIndent=28, bulletIndent=18, spaceAfter=2)
S("Lead", fontName="Helvetica-Oblique", fontSize=10.2, leading=15,
  textColor=GREY, spaceAfter=8)

S("CodeBlk", fontName="Courier", fontSize=7.6, leading=10.2, textColor=SLATE,
  spaceAfter=0, spaceBefore=0)
S("CalloutTitle", fontName="Helvetica-Bold", fontSize=9.2, leading=12, spaceAfter=2)
S("CalloutBody", fontName="Helvetica", fontSize=9.2, leading=13, textColor=SLATE_MED)

S("TH", fontName="Helvetica-Bold", fontSize=8.6, leading=11, textColor=colors.white)
S("TD", fontName="Helvetica", fontSize=8.6, leading=11.5, textColor=SLATE_MED)
S("TDmono", fontName="Courier", fontSize=8.0, leading=11, textColor=SLATE)
S("TDb", fontName="Helvetica-Bold", fontSize=8.6, leading=11.5, textColor=SLATE)

S("TOCTitle", fontName="Helvetica-Bold", fontSize=22, leading=26, textColor=CRIMSON,
  spaceAfter=12)
S("TOC1", fontName="Helvetica-Bold", fontSize=11, leading=20, textColor=SLATE)
S("TOC2", fontName="Helvetica", fontSize=9.5, leading=15, textColor=SLATE_MED, leftIndent=16)
S("TOC3", fontName="Helvetica", fontSize=8.8, leading=13, textColor=GREY, leftIndent=32)

# ----------------------------------------------------------------------------
# Story helpers
# ----------------------------------------------------------------------------
story = []
_bk = [0]

def _key():
    _bk[0] += 1
    return f"bk{_bk[0]}"

def h1(text):
    p = Paragraph(text, styles["H1"]); p._toc = (0, text, _key()); story.append(p)
    line()

def h2(text):
    p = Paragraph(text, styles["H2"]); p._toc = (1, text, _key()); story.append(p)

def h3(text):
    p = Paragraph(text, styles["H3"]); p._toc = (2, text, _key()); story.append(p)

def body(text):
    story.append(Paragraph(text, styles["Body"]))

def lead(text):
    story.append(Paragraph(text, styles["Lead"]))

def bullets(items, style="LiBullet"):
    for it in items:
        story.append(Paragraph(it, styles[style], bulletText="•"))

def numbered(items):
    for i, it in enumerate(items, 1):
        story.append(Paragraph(it, styles["LiBullet"], bulletText=f"{i}."))

def space(h=6):
    story.append(Spacer(1, h))

def line(color=RULE, thickness=0.8, pad=4):
    t = Table([[""]], colWidths=[170*mm], rowHeights=[0.1])
    t.setStyle(TableStyle([("LINEBELOW", (0,0), (-1,-1), thickness, color)]))
    story.append(Spacer(1, pad)); story.append(t); story.append(Spacer(1, pad))

def code(text):
    text = text.strip("\n")
    inner = Preformatted(text, styles["CodeBlk"])
    t = Table([[inner]], colWidths=[170*mm])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0,0), (-1,-1), CODE_BG),
        ("BOX", (0,0), (-1,-1), 0.6, CODE_BD),
        ("LEFTPADDING", (0,0), (-1,-1), 8),
        ("RIGHTPADDING", (0,0), (-1,-1), 8),
        ("TOPPADDING", (0,0), (-1,-1), 6),
        ("BOTTOMPADDING", (0,0), (-1,-1), 6),
    ]))
    story.append(t); story.append(Spacer(1, 6))

def callout(kind, title, text):
    bg, bd = {"info": (INFO_BG, INFO_BD), "warn": (WARN_BG, WARN_BD),
              "tip": (TIP_BG, TIP_BD)}[kind]
    icon = {"info": "ℹ", "warn": "⚠", "tip": "✓"}[kind]
    cell = [Paragraph(f"<font face='Sym'>{icon}</font>  " + title, styles["CalloutTitle"]),
            Paragraph(text, styles["CalloutBody"])]
    t = Table([[cell]], colWidths=[170*mm])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0,0), (-1,-1), bg),
        ("LINEBEFORE", (0,0), (-1,-1), 3, bd),
        ("BOX", (0,0), (-1,-1), 0.4, bd),
        ("LEFTPADDING", (0,0), (-1,-1), 10),
        ("RIGHTPADDING", (0,0), (-1,-1), 8),
        ("TOPPADDING", (0,0), (-1,-1), 6),
        ("BOTTOMPADDING", (0,0), (-1,-1), 6),
    ]))
    story.append(t); story.append(Spacer(1, 7))

def table(headers, rows, col_widths, mono_cols=()):
    data = [[Paragraph(h, styles["TH"]) for h in headers]]
    for r in rows:
        cells = []
        for ci, val in enumerate(r):
            st = "TDmono" if ci in mono_cols else "TD"
            cells.append(Paragraph(str(val), styles[st]))
        data.append(cells)
    t = Table(data, colWidths=col_widths, repeatRows=1)
    ts = [
        ("BACKGROUND", (0,0), (-1,0), TBL_HEAD),
        ("LINEBELOW", (0,0), (-1,0), 0.6, TBL_HEAD),
        ("ROWBACKGROUNDS", (0,1), (-1,-1), [colors.white, TBL_ALT]),
        ("GRID", (0,0), (-1,-1), 0.4, RULE),
        ("VALIGN", (0,0), (-1,-1), "MIDDLE"),
        ("LEFTPADDING", (0,0), (-1,-1), 5),
        ("RIGHTPADDING", (0,0), (-1,-1), 5),
        ("TOPPADDING", (0,0), (-1,-1), 3.5),
        ("BOTTOMPADDING", (0,0), (-1,-1), 3.5),
    ]
    t.setStyle(TableStyle(ts))
    story.append(t); story.append(Spacer(1, 8))

# ============================================================================
# CONTENT
# ============================================================================

# ---- Cover ----
story.append(Spacer(1, 70*mm))
story.append(Paragraph("SinSiege", styles["CoverTitle"]))
story.append(Paragraph("Developer Handover &amp; Codebase Guide", styles["CoverSub"]))
story.append(Spacer(1, 6))
story.append(Paragraph("Current state of the code, the project architecture, and how to "
                       "use the custom Unity tooling (Sheet Fetcher, Enemy Creator, Wave Editor).",
                       styles["CoverMeta"]))
story.append(Spacer(1, 30*mm))
story.append(Paragraph("Unity 6000.3.8f1  ·  Universal Render Pipeline 2D  ·  FMOD Audio",
                       styles["CoverMeta"]))
story.append(Paragraph("Document generated June 2026", styles["CoverMeta"]))
story.append(NextPageTemplate("toc"))
story.append(PageBreak())

# ---- TOC ----
story.append(Paragraph("Contents", styles["TOCTitle"]))
toc = TableOfContents()
toc.levelStyles = [styles["TOC1"], styles["TOC2"], styles["TOC3"]]
story.append(toc)
story.append(NextPageTemplate("body"))
story.append(PageBreak())

# ===========================================================================
h1("1. Introduction")
lead("SinSiege is a 2D tower-defense / wave-survival hybrid built in Unity 6. "
     "It carries a seven-deadly-sins theme (the first playable region is &ldquo;Lust&rdquo;).")

body("The player controls a hero character on a map and also <b>builds towers</b> on fixed zones. "
     "Together they defend a central <b>Base</b> against <b>waves</b> of enemies. Enemies travel "
     "along authored <b>routes</b> toward the Base, while a parallel <b>horde</b> mechanic spawns "
     "enemies from the screen edges that chase the player directly. Killing enemies drops "
     "<b>Souls</b> (the in-game currency) used to build/merge towers and buy upgrades. Characters, "
     "towers, upgrades and all UI text are data-driven and localized into 12 languages.")

h2("1.1 Tech stack")
table(
    ["Area", "Choice", "Notes"],
    [
        ["Engine", "Unity 6000.3.8f1", "Open with this exact version via Unity Hub."],
        ["Rendering", "URP 17.3 (2D Renderer)", "2D feature set 2.0.2, Pixel Perfect, Sprite tools."],
        ["Camera", "Cinemachine 3.1.3", "Player cam + build-mode cam, swapped at runtime."],
        ["Input", "Input System 1.18", "InputSystem_Actions.inputactions at Assets root."],
        ["UI / Text", "uGUI 2.0 + TextMeshPro", "Localized via LocalizedText components."],
        ["Audio", "FMOD Studio", "External integration; see Assets/Plugins/FMOD and Assets/Fmod."],
        ["Post / Capture", "Post Processing 3.5, Recorder 5.1", "Recorder used for trailer/demo capture."],
    ],
    [30*mm, 48*mm, 92*mm])

h2("1.2 Opening &amp; running the project")
numbered([
    "Install <b>Unity 6000.3.8f1</b> (Unity Hub <font face='Sym'>&rarr;</font> Installs). The FMOD and URP packages "
    "restore automatically from <font face='Courier'>Packages/manifest.json</font>.",
    "Open the repository root folder as a project. The first import is slow (it regenerates "
    "<font face='Courier'>Library/</font>).",
    "Press Play from <b>Assets/Scenes/MainMenu.unity</b> for the full flow, or open "
    "<b>Assets/Scenes/Lust1.unity</b> to jump straight into gameplay.",
])
callout("info", "Scenes in the project",
        "<b>MainMenu</b> &mdash; title screen, character/level select. "
        "<b>Lust1</b> &mdash; the main gameplay level. "
        "<b>Gokhan Testing</b> &mdash; a scratch/experimental scene, not shipped. "
        "Use the <b>Scenes</b> menu (Ctrl+G) to jump between them quickly.")

h2("1.3 Source control")
bullets([
    "Standard Unity <font face='Courier'>.gitignore</font> &mdash; <font face='Courier'>Library/</font>, "
    "<font face='Courier'>obj/</font>, <font face='Courier'>Logs/</font>, <font face='Courier'>Builds/</font> "
    "and the generated <font face='Courier'>*.csproj</font> / <font face='Courier'>*.sln</font> files are not committed.",
    "<font face='Courier'>.gitattributes</font> only normalizes line endings and pins FMOD bundle/plist "
    "files to LF. <b>Git LFS is not currently configured</b> &mdash; large art/audio binaries are stored directly.",
    "Asset <font face='Courier'>.meta</font> files <b>must</b> be committed alongside their assets.",
])

# ===========================================================================
h1("2. Project Layout")
body("Everything game-specific lives under <font face='Courier'>Assets/</font>. The folders you will "
     "touch most often:")

table(
    ["Path", "What lives here"],
    [
        ["Assets/Scripts", "All runtime gameplay code (managers, mechanics, data, UI, towers, enemies)."],
        ["Assets/Scripts/Editor", "Editor-only code that ships next to gameplay: Sheet Fetcher, Localization Sync, WaveSO inspector."],
        ["Assets/Editor", "Standalone editor tools: Enemy Creator, Wave Editor, Scene Quick Access, button drawer."],
        ["Assets/Prefabs", "Enemies, Towers, Bullets, Coins, Levels, Player, UI, Persistent, Props."],
        ["Assets/Resources", "Assets loaded by name at runtime: ReferencesSO, LocalizationData, Upgrades/, Characters/, Materials/."],
        ["Assets/Scriptable Objects", "Authoring data assets: Waves/ (per-level WaveSO files) and Routes/."],
        ["Assets/Scenes", "MainMenu, Lust1, Gokhan Testing."],
        ["Assets/Sprites / Animation / Materials / Shaders", "Art, animation clips/controllers, materials and shaders."],
        ["Assets/Fmod / SinSiegeFMOD / FMODAssets", "FMOD banks, project link and generated audio GUIDs."],
    ],
    [52*mm, 118*mm])

callout("warn", "Two editor-script locations",
        "Editor tooling is split between <b>Assets/Editor</b> and <b>Assets/Scripts/Editor</b>. "
        "Both compile into Unity's editor assembly purely because each path contains a folder named "
        "<font face='Courier'>Editor</font>. When adding a new tool, drop it in either &mdash; but be "
        "consistent and never reference editor code from runtime scripts (it will break player builds).")

h2("2.1 Runtime script groups")
table(
    ["Folder under Scripts/", "Responsibility"],
    [
        ["System", "Bootstrapping &amp; cross-cutting: LevelInitializer, PersistentManager refs, GameState, MasterDictionary, ReferencesSO/Refs, Config."],
        ["Managers", "Per-scene services: GameManager, WaveManager, BuildManager, TowerManager, UpgradeManager, Music/Sound, Save/Scene/Persistent."],
        ["Wave Related", "WaveSO + WaveSpawnData, WaveGroup + WaveSlot (the data the Wave Editor authors)."],
        ["Data", "LevelData (routes/segments/waves on the level prefab), CharacterData."],
        ["Enemies / Bosses", "Enemy.cs movement &amp; combat; Boss, MammonArm."],
        ["Towers", "Base, Tower, TowerGeneric, TowerChapel, TowerManager, TowerZone."],
        ["Mechanics", "Player, Shop, Coin, Upgrade(Data), Minimap, Combat/ (Bullet, FireMethods)."],
        ["UI", "Combat HUD, build/merge menus, menu buttons, cards, sliders."],
        ["Sheets Related", "Localization runtime: LocalizationManager/Data, LocalizedText, LanguageDropdown."],
    ],
    [44*mm, 126*mm])

# ===========================================================================
h1("3. Architecture &amp; Current State")
lead("This section describes how the running game is wired together &mdash; read it before changing "
     "managers or the spawning pipeline.")

h2("3.1 Bootstrapping &amp; lifecycle")
body("The game uses an explicit <b>Init() pattern</b> rather than relying on Unity's Awake/Start "
     "ordering. Two objects drive startup:")
bullets([
    "<b>PersistentManager</b> &mdash; a <font face='Courier'>DontDestroyOnLoad</font> singleton "
    "(in the Persistent prefab) that survives scene loads. It owns <b>GameState</b>, "
    "<b>LocalizationManager</b>, <b>SaveManager</b> and <b>SceneManager</b>. Each of those exposes a "
    "static <font face='Courier'>Instance =&gt; PersistentManager.Instance.X</font> accessor.",
    "<b>LevelInitializer</b> &mdash; a per-gameplay-scene singleton. On Awake it finds every scene "
    "manager (<font face='Courier'>FindManagersAndObjects()</font>), calls each manager's "
    "<font face='Courier'>Init()</font> in a deliberate order, reads the chosen level index from "
    "GameState, and loads it.",
])
code(
"// LevelInitializer.Awake -> FindManagersAndObjects() calls, in order:\n"
"Player.Init();\n"
"ArrowManager.Init();  BuildManager.Init();  GameManager.Init();\n"
"MouseManager.Init();  WaveManager.Init();\n"
"\n"
"// Then SetLevelIndex() reads PersistentManager.GameState.LevelIndex,\n"
"// and LoadLevel() pushes the matching LevelData into the WaveManager:\n"
"WaveManager.SetLevelData(levelDatas[levelIndex]);\n"
"WaveManager.ResetWavesAndRoutes();\n"
"WaveManager.GetWavesAndRoutesFromLevelData();   // caches route paths"
)
callout("info", "How a level is selected",
        "From the menu, <font face='Courier'>SceneManager.StartLevelWithData(sceneName, levelIndex)</font> "
        "stores the index on <b>GameState</b> (firing <font face='Courier'>OnLevelChanged</font>) and loads "
        "the scene. <b>LevelInitializer.levelDatas</b> is an ordered list assigned in the Inspector; the "
        "level index selects which <font face='Courier'>LevelData</font> the WaveManager uses. Keep that "
        "list, the scene Build Settings and the menu's indices in sync.")

h2("3.2 Scene managers at a glance")
table(
    ["Manager", "Role"],
    [
        ["GameManager", "Central gameplay hub: base health UI, Souls/coin spawning &amp; denomination, build/combat camera swap, mouse world position."],
        ["WaveManager", "Drives spawning from LevelData (see 3.3). Owns wave timer, route blocking, horde spawning, win condition."],
        ["BuildManager", "Tower placement / build-mode UI (builder, manager and merge menus)."],
        ["TowerManager", "Live tower registry &amp; tower-side operations."],
        ["UpgradeManager", "Periodic upgrade choices (every 5th wave); applies UpgradeData effects."],
        ["MusicManager", "FMOD music; SetCombatToTrue/False toggles the combat parameter."],
        ["SoundManager", "FMOD one-shot SFX (e.g. enemy death)."],
        ["SortingManager", "2D sprite sort-order helpers."],
        ["ArrowManager / MouseManager", "Off-screen target arrows; custom cursor &amp; click routing."],
        ["SaveManager", "SaveGame / LoadGame / DeleteSave (PlayerPrefs/JSON persistence)."],
        ["SceneManager", "Wraps Unity scene loading; StartGame, StartLevelWithData, current scene name."],
    ],
    [40*mm, 130*mm])

h2("3.3 Wave / Route / Spawning system")
body("This is the heart of the game and the data the <b>Wave Editor</b> authors. The data lives on the "
     "<b>level prefab root</b> in a <font face='Courier'>LevelData</font> component.")

h3("Data model")
bullets([
    "<b>LevelData</b> holds three authored lists: <font face='Courier'>mapRoutes</font>, "
    "<font face='Courier'>waveGroups</font>, and <font face='Courier'>availableSegments</font> "
    "(a reusable segment pool), plus <font face='Courier'>spawnPoints</font>.",
    "<b>PathSegment</b> = a name + a spawn point + an ordered list of waypoint Transforms (children "
    "of a <font face='Courier'>Waypoints</font> object on the prefab).",
    "<b>MapRoute</b> = a name + ordered <font face='Courier'>pathSegments</font>. At runtime "
    "<font face='Courier'>GetCalculatedPath()</font> flattens the spawn point + every waypoint and "
    "<b>auto-appends the Base position</b> (it calls <font face='Courier'>FindFirstObjectByType&lt;Base&gt;()</font>).",
    "<b>WaveGroup</b> = a list of <b>WaveSlot</b>. A <b>WaveSlot</b> pairs a <font face='Courier'>WaveSO</font> "
    "with a <font face='Courier'>routeIndex</font> (an index into <font face='Courier'>mapRoutes</font>).",
    "<b>WaveSO</b> (ScriptableObject) = the actual enemy script: pre-wave cooldown, default spawn "
    "interval, an <font face='Courier'>enemySpawns</font> list, optional horde config, and cached "
    "Gold/Exp totals.",
    "<b>WaveSpawnData</b> = one spawn entry: enemy prefab, count, optional interval override, and a "
    "stat-modification mode (None / Multiplier / CustomValue) with the corresponding fields.",
])

h3("Runtime flow (WaveManager)")
numbered([
    "On Init, each route's path is flattened once into <font face='Courier'>_cachedPaths</font>.",
    "A countdown timer runs; the <b>Start Wave</b> button can skip it. When it hits zero the current "
    "<font face='Courier'>WaveGroup</font> starts.",
    "Each non-empty slot becomes a <b>WaveSpawnerState</b>. <font face='Courier'>ExpandByCount()</font> "
    "flattens each entry so it appears <i>count</i> times.",
    "All slots in a group run <b>simultaneously</b>, but slots sharing the same "
    "<font face='Courier'>routeIndex</font> are <b>serialized left-to-right</b> &mdash; an earlier "
    "same-route slot blocks later ones until it finishes (route blocking).",
    "Each spawn instantiates the enemy at the route's start, assigns it the cached path and the "
    "WaveManager, then <font face='Courier'>ApplyConfigToEnemy()</font> applies multipliers or custom values.",
    "Hordes (if any slot enables one) spawn from random screen edges with "
    "<font face='Courier'>followPlayer = true</font> while normal enemies are still alive.",
    "The group is complete when all spawners are empty and no enemies remain; the wave index advances. "
    "Every 5th wave triggers <font face='Courier'>UpgradeManager.TimeToUpgrade()</font>. When the last "
    "group ends, <font face='Courier'>allWavesCompleted</font> is set.",
])
callout("info", "Route index is positional",
        "A <font face='Courier'>WaveSlot.routeIndex</font> is simply the position of the route in "
        "<font face='Courier'>LevelData.mapRoutes</font>. Reordering or deleting routes shifts those "
        "indices &mdash; re-check wave/route assignments in the Wave Editor afterwards.")

h2("3.4 Enemies")
body("<font face='Courier'>Enemy.cs</font> carries serialized base stats "
     "(<font face='Courier'>_moveSpeed, _health, damage, exp, coinValue, sliderOffset</font>) exposed "
     "read-only as <font face='Courier'>BaseHealth/BaseSpeed/...</font>. It moves along its assigned path "
     "(or follows the player when <font face='Courier'>followPlayer</font> is set), flips its sprite by "
     "travel direction, sorts by Y, spawns a world-space health bar on first damage, and on death calls "
     "<font face='Courier'>GameManager.SpawnCoins()</font>. <font face='Courier'>InitializeStats()</font> "
     "lets the WaveManager override stats per spawn.")
bullets([
    "Enemy prefabs live in <font face='Courier'>Assets/Prefabs/Enemies/&lt;Category&gt;/</font> where "
    "category is a sin name (Lust, Greed, &hellip;), plus <font face='Courier'>Generic</font>, "
    "<font face='Courier'>GenericHorde</font>, <font face='Courier'>LustHorde</font>, <font face='Courier'>Bosses</font>.",
    "<b>EnemyDatabase</b> (a ScriptableObject) lists every enemy for the inspector pickers. It has an "
    "<b>Auto-Find All Enemies</b> context-menu item that rescans the project.",
    "New enemies are created with the <b>Enemy Creator</b> tool (Section 4.3) as prefab variants.",
])
callout("warn", "Open TODO: enemies do not yet damage the Base",
        "When an enemy reaches the end of its path, <font face='Courier'>Enemy.Update()</font> currently "
        "just destroys it (there is a <font face='Courier'>// TODO: Deal damage to base?</font>). Base "
        "health UI exists but nothing decrements it from path completion yet &mdash; this is the most "
        "visible gameplay gap to wire up.")

h2("3.5 Towers, combat &amp; economy")
bullets([
    "<b>Tower hierarchy:</b> <font face='Courier'>Base</font> (the HQ), <font face='Courier'>Tower</font> / "
    "<font face='Courier'>TowerGeneric</font> (stats + firing), specialised "
    "<font face='Courier'>TowerChapel</font>, plus <font face='Courier'>TowerManager</font> and "
    "<font face='Courier'>TowerZone</font> (placement zones).",
    "<b>TowerGeneric</b> exposes the balance fields the Sheet Fetcher writes "
    "(<font face='Courier'>towerName, tier, animationInit, attackRangeDefault, attackIntervalDefault, "
    "attackDamageDefault, bulletSpeed, bulletCount, spreadAngle, bulletHealth, bulletIsSpinning, "
    "isAOEBullet, targetTagDefault</font>).",
    "<b>Combat:</b> <font face='Courier'>FireMethods</font> (targeting, TargetTag enum), "
    "<font face='Courier'>Bullet</font>, <font face='Courier'>CrossBulletImpact</font>.",
    "<b>Economy:</b> currency is &ldquo;Souls&rdquo;. <font face='Courier'>GameManager.SpawnCoins(value,pos)</font> "
    "breaks a value into coin denominations (parallel <font face='Courier'>CoinPrefabs</font> / "
    "<font face='Courier'>CoinValues</font> lists).",
    "<b>Upgrades:</b> <font face='Courier'>UpgradeData</font> assets (UpgradeType enum: AttackInterval, "
    "Damage, BulletSpeed, BulletCount, BulletHealth, MoveSpeed, MaxHealth, Custom) drive "
    "<font face='Courier'>UpgradeManager</font>.",
])

h2("3.6 Shared registries")
bullets([
    "<b>ReferencesSO</b> (in <font face='Courier'>Resources</font>, fetched via "
    "<font face='Courier'>Refs.R</font>) is the central registry: the character list, a color palette, "
    "and <font face='Courier'>TowerReferences[]</font> (name + prefab, indexed by tower ID).",
    "<b>MasterDictionary</b> is a static catalog of enums: GameLanguage (12), Characters (8), Towers (18), "
    "UpgradeRarity (7) and rarity colors, plus the MainMenu scene name constant.",
])

h2("3.7 Localization")
body("Text is data-driven. <font face='Courier'>LocalizationData</font> (a ScriptableObject in "
     "<font face='Courier'>Resources</font>) holds language codes + key/value rows. "
     "<font face='Courier'>LocalizationManager</font> builds a fast key<font face='Sym'>&rarr;</font>string map for the current "
     "language and raises <font face='Courier'>OnLanguageChanged</font>; <font face='Courier'>LocalizedText</font> "
     "components subscribe and refresh. The data is baked from a Google Sheet with the "
     "<b>Localization Sync</b> tool (Section 4.2).")

h2("3.8 Known rough edges")
callout("warn", "Things to be aware of when taking over",
        "&bull; Enemies reaching the Base are destroyed without dealing damage (TODO above).<br/>"
        "&bull; <font face='Courier'>MasterDictionary.RarityColors</font> uses "
        "<font face='Courier'>new Color(0-255&hellip;)</font> instead of Color32, so those struct "
        "values are out of the 0&ndash;1 range (the parallel <font face='Courier'>Color32[]</font> "
        "array is the correct one).<br/>"
        "&bull; Two enemy-picker UIs coexist: the Wave Editor's built-in grid (scans the Enemies folder) "
        "and <font face='Courier'>EnemySelectorPopup</font> (uses EnemyDatabase). They can drift apart.<br/>"
        "&bull; <font face='Courier'>Level_..._OLD.prefab</font> and the &ldquo;Gokhan Testing&rdquo; scene "
        "are scratch artifacts.<br/>"
        "&bull; Heavy use of <font face='Courier'>FindFirstObjectByType</font> in Init paths &mdash; fine "
        "today, but watch it if scenes grow.")

# ===========================================================================
h1("4. Custom Tools")
lead("The project ships several editor tools that make content authoring fast. They live under the "
     "<b>Tools</b> and <b>Scenes</b> menus. This is the part most relevant to day-to-day content work.")

h2("4.0 Menu map")
table(
    ["Menu item", "Tool", "Section"],
    [
        ["Tools &rsaquo; Sheet Fetcher &rsaquo; Open Menu", "Sheet Fetcher window", "4.1"],
        ["Tools &rsaquo; Sheet Fetcher &rsaquo; Sync Upgrades / Characters / Towers", "One-click sync (uses saved URLs)", "4.1"],
        ["Tools &rsaquo; Localization &rsaquo; Open Downloader", "Localization Sync", "4.2"],
        ["Tools &rsaquo; Enemy Creator", "Enemy Creator", "4.3"],
        ["Tools &rsaquo; Wave Editor", "Wave / Segment / Route editor", "4.4"],
        ["Scenes &rsaquo; Open Scene List  (Ctrl+G)", "Scene Quick Access", "4.5"],
        ["(WaveSO inspector) Calculate Total Gold &amp; Exp", "WaveSO editor button", "4.5"],
        ["(EnemyDatabase context menu) Auto-Find All Enemies", "Rebuild enemy list", "4.5"],
    ],
    [70*mm, 70*mm, 18*mm])

# ---- 4.1 Sheet Fetcher ----
h2("4.1 Sheet Fetcher")
body("<b>File:</b> <font face='Courier'>Assets/Scripts/Editor/SheetFetcher.cs</font>")
body("Syncs game-balance data from <b>Google Sheets</b> directly into ScriptableObjects and tower "
     "prefabs, so designers can tune numbers in a spreadsheet and pull them into Unity with one click.")

h3("Opening &amp; usage")
numbered([
    "Open <b>Tools &rsaquo; Sheet Fetcher &rsaquo; Open Menu</b>. The window has three sections: "
    "<b>Upgrades</b>, <b>Characters</b>, <b>Towers</b>.",
    "Paste the normal Google Sheets share link into a section's <b>CSV Link</b> field. The tool "
    "auto-converts a <font face='Courier'>/edit</font> URL into a <font face='Courier'>/export?format=csv</font> "
    "URL, so a plain share link works. URLs are remembered per section in EditorPrefs.",
    "Click <b>Sync</b> to write assets, or <b>Debug (Print Rows)</b> to dump the first few parsed rows "
    "to the Console &mdash; use Debug first to confirm the columns line up.",
])
callout("info", "Sheet must be readable &amp; column order matters",
        "Share the sheet as <b>Anyone with the link &ndash; Viewer</b> (or publish to web). The first "
        "row is treated as a header and skipped. The separator (comma or semicolon) is auto-detected, "
        "and decimals parse under both invariant and Turkish cultures.")

h3("Column mappings")
body("<b>Upgrades</b> <font face='Sym'>&rarr;</font> one <font face='Courier'>UpgradeData</font> asset per row in "
     "<font face='Courier'>Assets/Resources/Upgrades</font>:")
table(["Col", "Field", "Col", "Field"],
    [
        ["0", "upgradeID", "5", "value (float)"],
        ["1", "upgradeName", "6", "isMultiplier (TRUE/1)"],
        ["3", "upgradeLevel", "8", "secondaryValue"],
        ["4", "upgradeType (enum)", "9", "ternaryValue"],
    ],
    [12*mm, 60*mm, 12*mm, 60*mm], mono_cols=(0,2))

body("<b>Characters</b> <font face='Sym'>&rarr;</font> one <font face='Courier'>CharacterData</font> asset per row in "
     "<font face='Courier'>Assets/Resources/Characters</font> (needs &ge; 7 columns):")
table(["Col", "Field", "Col", "Field"],
    [
        ["0", "id (Characters enum name)", "4", "damage"],
        ["1", "characterName", "5", "attackSpeed"],
        ["2", "fullName", "6", "movementSpeed"],
        ["3", "desc", "", ""],
    ],
    [12*mm, 60*mm, 12*mm, 60*mm], mono_cols=(0,2))

body("<b>Towers</b> <font face='Sym'>&rarr;</font> writes onto the <font face='Courier'>TowerGeneric</font> component of the "
     "prefab at <font face='Courier'>ReferencesSO.TowerReferences[ID]</font>. No new assets are created "
     "and nothing is deleted.")
table(["Col", "Field", "Col", "Field"],
    [
        ["0", "towerName", "8", "attackDamageDefault"],
        ["1", "ID (index into TowerReferences)", "9", "bulletSpeed"],
        ["2", "animationInit", "10", "bulletCount"],
        ["3", "tier", "11", "spreadAngle"],
        ["4", "mergeContent (skipped)", "12", "bulletHealth"],
        ["5", "targetTag (enum)", "13", "bulletIsSpinning"],
        ["6", "attackRangeDefault", "14", "isAOEBullet"],
        ["7", "attackIntervalDefault", "", ""],
    ],
    [12*mm, 62*mm, 12*mm, 58*mm], mono_cols=(0,2))

callout("warn", "Upgrades &amp; Characters sync is destructive",
        "After writing, the Upgrades and Characters sync <b>deletes any .asset in the target folder "
        "that is not present in the sheet</b>. Keep those folders for synced data only &mdash; do not "
        "hand-place unrelated assets there. The Towers sync does <b>not</b> delete (it edits existing "
        "prefabs in place), but the tower <b>ID column must be a valid index</b> into "
        "<font face='Courier'>ReferencesSO.TowerReferences</font> or the row is skipped.")

h3("Quick recipe")
numbered([
    "Edit numbers in the Google Sheet.",
    "In Unity: <b>Tools &rsaquo; Sheet Fetcher</b>, click <b>Debug</b> on the relevant section, confirm "
    "the Console rows look right.",
    "Click <b>Sync</b>. Check the green &ldquo;Sync Complete&rdquo; log and the updated assets.",
    "For towers, make sure the prefab IDs/order in <font face='Courier'>ReferencesSO</font> match the "
    "sheet's ID column first.",
])

# ---- 4.2 Localization Sync ----
h2("4.2 Localization Sync")
body("<b>File:</b> <font face='Courier'>Assets/Scripts/Editor/LocalizationSync.cs</font>")
body("A sibling of the Sheet Fetcher dedicated to UI text. Open <b>Tools &rsaquo; Localization &rsaquo; "
     "Open Downloader</b>, paste the CSV link, and click <b>Fetch &amp; Bake Data</b>.")
bullets([
    "It downloads the sheet and writes <font face='Courier'>Assets/Resources/LocalizationData.asset</font> "
    "(creating it if missing).",
    "Sheet shape: the <b>header row</b> is <font face='Courier'>key, en, tr, fr, &hellip;</font> &mdash; "
    "the first column is the lookup key, each following column is a language code. Every bake "
    "<b>clears and rebuilds</b> all entries.",
    "Language codes map to <font face='Courier'>MasterDictionary.GameLanguage</font> in "
    "<font face='Courier'>LocalizationManager.GetLanguageCode()</font> (en, tr, fr, it, de, pt, ru, pl, "
    "kr, jp, zh, tc).",
    "At runtime, call <font face='Courier'>LocalizationManager.Instance.GetLocalizedValue(key)</font>; "
    "missing keys return <font face='Courier'>MISSING: key</font> so they are easy to spot.",
])

# ---- 4.3 Enemy Creator ----
h2("4.3 Enemy Creator")
body("<b>File:</b> <font face='Courier'>Assets/Editor/Enemy Creator/EnemyCreatorWindow.cs</font>")
body("Creates a new enemy as a <b>Prefab Variant</b> of an existing base enemy prefab. You enter the "
     "Enemy.cs stats, pick a sprite, and either generate a looping animation from a sliced sprite sheet "
     "or assign an existing Animator Controller. Open via <b>Tools &rsaquo; Enemy Creator</b>.")

h3("Fields")
table(["Field", "Meaning"],
    [
        ["Base Enemy Prefab", "Template to make a Variant of. Must have an Enemy component on its root. Selecting one auto-fills stats and folders."],
        ["New Enemy Name", "File name of the created prefab (sanitised)."],
        ["Prefab Folder", "Output folder (defaults to the base prefab's folder). Use &hellip; to browse; must be inside Assets."],
        ["Enemy Stats", "Move Speed, Health, Damage, Exp, Coin Value, Health-Bar Offset. &ldquo;Reset stats from base&rdquo; re-reads the base."],
        ["Animation mode", "Create From Sheet · Use Existing Controller · None."],
        ["Sprite Sheet (sliced)", "A texture imported as Sprite Mode = Multiple and sliced into frames (Create-From-Sheet mode)."],
        ["Samples (fps)", "Animation sample rate / frames per second for the generated clip (default 8)."],
        ["Animation Folder", "Where the generated .anim / .controller are written."],
        ["Animator Controller", "An existing controller (Use-Existing mode)."],
        ["Default Sprite", "Resting sprite for the SpriteRenderer; empty = the sheet's first frame."],
    ],
    [44*mm, 126*mm])

h3("What &ldquo;Create Enemy&rdquo; does")
numbered([
    "If Create-From-Sheet: loads the sheet's sub-sprites (natural-sorted frame_0, frame_1, &hellip;), "
    "builds a <b>looping</b> sprite clip at the chosen fps, and creates an AnimatorController from it.",
    "Instantiates the base prefab, writes the stat overrides + default sprite + controller through "
    "SerializedObject so they are recorded as variant overrides.",
    "Saves as a Prefab <b>Variant</b> at <font face='Courier'>[Folder]/[Name].prefab</font> "
    "(clip: <font face='Courier'>[Name]_Move.anim</font>, controller: "
    "<font face='Courier'>[Name]_Animc.controller</font>) and pings it in the Project window.",
])
callout("tip", "After creating an enemy",
        "Place it in the correct sin sub-folder under <font face='Courier'>Assets/Prefabs/Enemies/</font> "
        "(the folder name drives category grouping in the picker), then run <b>Auto-Find All Enemies</b> "
        "on the EnemyDatabase so it appears in the database-backed picker. Because it is a Variant, "
        "later edits to the base prefab propagate automatically.")

# ---- 4.4 Wave Editor ----
h2("4.4 Wave Editor")
body("<b>Files:</b> <font face='Courier'>Assets/Editor/Wave Editor/</font> &mdash; WaveEditorWindow.cs, "
     "EnemySelectorPopup.cs, WaveSpawnDataDrawer.cs.")
body("The main level-authoring tool: it edits a level's <b>waves, path segments and routes</b>. Open "
     "via <b>Tools &rsaquo; Wave Editor</b>, then pick a level from the dropdown (any prefab with a "
     "<font face='Courier'>LevelData</font> component under "
     "<font face='Courier'>Assets/Prefabs/Levels</font>).")

callout("info", "It edits the level live in Prefab Mode",
        "Selecting a level opens it in Unity's <b>Prefab Mode</b> and binds to the live "
        "<font face='Courier'>LevelData</font>. Every edit goes through one SerializedObject (or an "
        "Undo-recorded Transform move), so references can't desync. <b>Saving</b> uses the normal Prefab "
        "Mode save &mdash; Auto Save, or Ctrl+S. The top bar shows a green &ldquo;Saved&rdquo; / amber "
        "&ldquo;Unsaved&rdquo; indicator.")

body("A top toolbar switches between three modes: <b>Waves</b>, <b>Segments</b>, <b>Routes</b>. The "
     "logical authoring order is Segments <font face='Sym'>&rarr;</font> Routes <font face='Sym'>&rarr;</font> Waves.")

h3("Waves mode")
bullets([
    "<b>Left timeline:</b> ordered <b>Wave</b> groups, each containing <b>spawner slots</b>. Use "
    "&plus; Add Wave, &plus; Add spawner, <font face='Sym'>&uarr;</font>/<font face='Sym'>&darr;</font> to reorder, &times; to delete.",
    "Each slot has a colour-coded <b>route dropdown</b> and a <b>wave</b> reference.",
    "<b>Assign a wave</b> three ways: select a slot then click <b>Assign</b> in the Library tab; "
    "<b>drag a WaveSO</b> from the Project window onto the slot; or <b>Create New Wave &amp; Assign</b>.",
    "<b>Right panel &mdash; Wave Editor tab:</b> rename the wave, see live Gold/Exp totals, set Timing "
    "(Pre-Wave Cooldown, Default Spawn Interval), edit the <b>Enemy Spawns</b> list, and toggle a "
    "<b>Horde</b> (interval + its own spawn list).",
    "Each spawn entry has a clickable enemy sprite (opens a grid picker of all prefabs with an Enemy "
    "under <font face='Courier'>Assets/Prefabs/Enemies</font>), a <b>count</b>, an optional per-entry "
    "<b>interval override</b>, and a <b>Stat Mode</b>: None, Multiplier (HP/Speed/Damage/Gold/Exp &times;), "
    "or CustomValue (absolute Health/Speed/Damage/Gold/Exp).",
    "<b>Library tab:</b> all WaveSO assets grouped by folder (the current level's folder is highlighted), "
    "with search, create, delete and an assigned-count badge. New waves are created under "
    "<font face='Courier'>Assets/Scriptable Objects/Waves/[LevelName]</font>.",
])

h3("Segments mode")
bullets([
    "Manages the level's reusable <b>segment pool</b> (<font face='Courier'>availableSegments</font>).",
    "Create a segment, give it a name, choose a <b>Spawn Point</b> (dropdown populated from the level's "
    "<font face='Courier'>Spawn Points</font> child object), then build its ordered <b>waypoint list</b> "
    "by adding from <b>Available in Prefab</b> (children of the <font face='Courier'>Waypoints</font> "
    "child object). Reorder with Up/Dn.",
    "<b>Scene handles:</b> with a segment selected, click a white dot to add a waypoint, a green dot to "
    "remove it, and drag the position handles to move waypoints. Spawn points are shown in orange with a "
    "dotted line to the first waypoint.",
])

h3("Routes mode")
bullets([
    "Create a <b>route</b>, name it, then build its <b>Segment Sequence</b> with <b>&plus; Add Segment "
    "from Pool</b> (a menu of pooled segments). Reorder / delete sequence entries.",
    "The first segment in a route <b>must have a Spawn Point</b> (the editor warns if not).",
    "<b>Scene handles</b> draw each route's path in a distinct colour; the selected route's waypoints "
    "are movable.",
    "A route's position in the list <b>is</b> its index &mdash; that's the "
    "<font face='Courier'>routeIndex</font> a wave slot points at (and what "
    "<font face='Courier'>WaveManager._cachedPaths</font> uses).",
])

callout("warn", "Level prefab requirements",
        "For Segments/Routes to work, the level prefab root needs child objects named exactly "
        "<font face='Courier'>Waypoints</font> (holding waypoint Transforms) and "
        "<font face='Courier'>Spawn Points</font> (holding spawn Transforms), and the gameplay scene "
        "needs a <font face='Courier'>Base</font> object (routes auto-append its position as the path "
        "end). The level's <font face='Courier'>LevelData</font> must also be registered in "
        "<font face='Courier'>LevelInitializer.levelDatas</font> to be playable.")

# ---- 4.5 Supporting tools ----
h2("4.5 Supporting editor utilities")
bullets([
    "<b>WaveSpawnDataDrawer + EnemySelectorPopup</b> &mdash; a custom property drawer used when editing a "
    "<font face='Courier'>WaveSO</font> in the <i>normal</i> Inspector. It shows a large sprite button "
    "that opens a popup of enemies <b>categorised by sin folder</b>, backed by the EnemyDatabase "
    "(previews are cached). If no EnemyDatabase exists it logs an error &mdash; create one via "
    "<b>Create &rsaquo; TowerDefense &rsaquo; EnemyDatabase</b>.",
    "<b>WaveSOEditor</b> &mdash; adds a <b>Calculate Total Gold &amp; Exp</b> button to the WaveSO "
    "inspector (the Wave Editor recalculates these automatically; this is for manual edits).",
    "<b>SceneQuickAccess</b> &mdash; <b>Scenes &rsaquo; Open Scene List</b> (Ctrl+G): a popup list of "
    "every scene in <font face='Courier'>Assets/Scenes</font> for fast switching (prompts to save first).",
    "<b>EnemyDatabase.FindAllEnemies</b> &mdash; right-click the EnemyDatabase asset <font face='Sym'>&rarr;</font> "
    "<b>Auto-Find All Enemies</b> to rescan the project and rebuild its list.",
])

# ===========================================================================
h1("5. Common Workflows")
lead("Step-by-step recipes for the most frequent content tasks.")

h2("5.1 Add a new enemy")
numbered([
    "<b>Tools &rsaquo; Enemy Creator</b>. Assign a Base Enemy Prefab, set stats, pick a sliced sprite "
    "sheet (or a controller), click <b>Create Enemy</b>.",
    "Move/keep the prefab in the right <font face='Courier'>Assets/Prefabs/Enemies/&lt;Sin&gt;/</font> folder.",
    "Select the EnemyDatabase asset <font face='Sym'>&rarr;</font> <b>Auto-Find All Enemies</b>.",
    "The enemy now appears in the Wave Editor's spawn picker.",
])

h2("5.2 Create or edit a wave")
numbered([
    "<b>Tools &rsaquo; Wave Editor</b>, pick the level.",
    "Open the <b>Library</b> tab, <b>Create New Wave</b> (or select one).",
    "In the <b>Wave Editor</b> tab add spawn entries, choose enemies, counts, intervals and stat mode; "
    "optionally enable a Horde.",
    "Add a Wave group / spawner in the timeline and <b>Assign</b> the wave to a slot; set its route.",
    "Ctrl+S (or rely on Auto Save) to save the level prefab.",
])

h2("5.3 Build a new level")
numbered([
    "Duplicate an existing level prefab in <font face='Courier'>Assets/Prefabs/Levels</font> "
    "(it already has a <font face='Courier'>LevelData</font>, <font face='Courier'>Waypoints</font> and "
    "<font face='Courier'>Spawn Points</font> structure to copy).",
    "Lay out waypoints and spawn points as children of those objects. Ensure the gameplay scene has a "
    "<font face='Courier'>Base</font>.",
    "In the Wave Editor: build <b>Segments</b>, assemble them into <b>Routes</b>, then author "
    "<b>Waves</b> and assign them to routes.",
    "Add the new <font face='Courier'>LevelData</font> to <b>LevelInitializer.levelDatas</b> (note its "
    "index), add the scene to Build Settings, and make the menu pass the matching level index via "
    "<font face='Courier'>SceneManager.StartLevelWithData</font>.",
])

h2("5.4 Tune balance, characters or upgrades")
numbered([
    "Edit the Google Sheet(s).",
    "<b>Tools &rsaquo; Sheet Fetcher</b> <font face='Sym'>&rarr;</font> Debug to verify <font face='Sym'>&rarr;</font> Sync the relevant section "
    "(Upgrades / Characters / Towers). Remember the destructive delete for Upgrades/Characters.",
])

h2("5.5 Add a tower")
numbered([
    "Create the tower prefab with a <font face='Courier'>TowerGeneric</font> (or subclass) component.",
    "Add it to <font face='Courier'>ReferencesSO.TowerReferences</font> at the intended ID (index).",
    "Add an entry to <font face='Courier'>MasterDictionary.Towers</font> if it needs an enum.",
    "Put its stats in the towers sheet (ID column = that index) and run the Towers sync.",
])

h2("5.6 Add or translate UI text")
numbered([
    "Add a key row (and translations) in the localization sheet.",
    "<b>Tools &rsaquo; Localization &rsaquo; Open Downloader</b> <font face='Sym'>&rarr;</font> <b>Fetch &amp; Bake Data</b>.",
    "Reference the key from a <font face='Courier'>LocalizedText</font> component or "
    "<font face='Courier'>GetLocalizedValue(key)</font>.",
])

# ===========================================================================
h1("6. Conventions, Gotchas &amp; Cheat-Sheet")

h2("6.1 Conventions to respect")
bullets([
    "<b>Init() pattern:</b> managers are wired by <font face='Courier'>LevelInitializer</font>, not by "
    "ad-hoc Awake order. New managers should expose an <font face='Courier'>Init()</font> and be added "
    "to that flow.",
    "<b>Resources by name:</b> <font face='Courier'>ReferencesSO</font> and "
    "<font face='Courier'>LocalizationData</font> are loaded by exact name from "
    "<font face='Courier'>Resources</font> &mdash; don't rename or move them.",
    "<b>Enemy folder names = categories</b> in pickers; keep the sin sub-folders.",
    "<b>Route index is positional</b> &mdash; reordering routes rebinds wave slots.",
    "<b>Editor code stays in Editor folders</b> and is fenced with "
    "<font face='Courier'>#if UNITY_EDITOR</font> where it sits beside runtime code.",
])

h2("6.2 Easy mistakes")
bullets([
    "Running Upgrades/Characters sync with stray assets in the target folder &mdash; they get deleted.",
    "Forgetting to save the level prefab after Wave Editor changes (watch the Saved/Unsaved badge).",
    "Adding a level prefab but not registering its LevelData in LevelInitializer (it won't load).",
    "Tower sheet IDs that don't match TowerReferences indices (rows silently skipped).",
    "Creating an enemy but not re-running EnemyDatabase Auto-Find (missing from the database picker).",
])

h2("6.3 Tools cheat-sheet")
table(["Shortcut / Menu", "Does"],
    [
        ["Ctrl+G", "Open the Scene list popup."],
        ["Tools &rsaquo; Wave Editor", "Author waves / segments / routes for a level."],
        ["Tools &rsaquo; Enemy Creator", "Make a new enemy prefab variant."],
        ["Tools &rsaquo; Sheet Fetcher", "Pull Upgrades / Characters / Towers from Sheets."],
        ["Tools &rsaquo; Localization", "Bake UI text from a Sheet."],
    ],
    [55*mm, 115*mm])

h2("6.4 Key file index")
table(["File", "Purpose"],
    [
        ["System/LevelInitializer.cs", "Per-scene bootstrap; wires managers, loads the level."],
        ["System/ReferencesSO.cs + Refs.cs", "Central registry (characters, colors, tower prefabs)."],
        ["Managers/WaveManager.cs", "Runtime spawning, routes, hordes, win condition."],
        ["Wave Related/WaveSO.cs", "Wave data + WaveSpawnData + stat-mod modes."],
        ["Data/LevelData.cs", "Routes, segments, wave groups (on the level prefab)."],
        ["Enemies/Enemy.cs", "Enemy movement, health, death, stat overrides."],
        ["Editor/Wave Editor/WaveEditorWindow.cs", "The Wave Editor tool."],
        ["Editor/Enemy Creator/EnemyCreatorWindow.cs", "The Enemy Creator tool."],
        ["Scripts/Editor/SheetFetcher.cs", "The Sheet Fetcher tool."],
        ["Scripts/Editor/LocalizationSync.cs", "The Localization baker."],
    ],
    [70*mm, 100*mm])

space(8)
line(CRIMSON, 1.0)
story.append(Paragraph(
    "End of document &mdash; SinSiege Developer Handover. Generated from the codebase with reportlab.",
    styles["CoverMeta"]))

# ============================================================================
# Build with TOC (two-pass) + page footer
# ============================================================================
class DocTemplate(BaseDocTemplate):
    def afterFlowable(self, flowable):
        if isinstance(flowable, Paragraph) and hasattr(flowable, "_toc"):
            level, text, key = flowable._toc
            self.canv.bookmarkPage(key)
            self.canv.addOutlineEntry(text, key, level=level, closed=(level > 0))
            self.notify("TOCEntry", (level, text, self.page, key))

def footer(canvas, doc):
    canvas.saveState()
    canvas.setFont("Helvetica", 8)
    canvas.setFillColor(LIGHT)
    canvas.drawString(20*mm, 12*mm, "SinSiege — Developer Handover")
    canvas.drawRightString(190*mm, 12*mm, f"{doc.page}")
    canvas.setStrokeColor(RULE)
    canvas.setLineWidth(0.4)
    canvas.line(20*mm, 15*mm, 190*mm, 15*mm)
    canvas.restoreState()

def blank(canvas, doc):
    pass

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "SinSiege_Developer_Handover.pdf")
doc = DocTemplate(OUT, pagesize=A4,
                  leftMargin=20*mm, rightMargin=20*mm,
                  topMargin=18*mm, bottomMargin=20*mm,
                  title="SinSiege — Developer Handover",
                  author="SinSiege")

frame = Frame(doc.leftMargin, doc.bottomMargin,
              doc.width, doc.height, id="main")
doc.addPageTemplates([
    PageTemplate(id="cover", frames=[frame], onPage=blank),
    PageTemplate(id="toc", frames=[frame], onPage=footer),
    PageTemplate(id="body", frames=[frame], onPage=footer),
])

doc.multiBuild(story)
print("Wrote", OUT)
