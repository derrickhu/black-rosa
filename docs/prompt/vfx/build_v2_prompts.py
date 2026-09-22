#!/usr/bin/env python3
"""按 §4.0 的风格骨架批量生成 v2 特效 prompt，避免每份 prompt 另起炉灶重写风格。

骨架来自火字那两份验证过的 prompt：平涂硬边、四档纯色、最外一档深色当描边、
白底、无网格线、单调平缓的 8 格渐变。这里只替换「画什么」和「配色 / 渐变往哪走」。

跑：/usr/bin/python3 docs/prompt/vfx/build_v2_prompts.py
"""

from pathlib import Path

OUT = Path(__file__).resolve().parent

# 所有 prompt 共用的底子。{...} 由每条目填。
SHEET = """2D game VFX sprite sheet for a top-down shooter. Layout: exactly {cols} columns x {rows} rows = {cells} separate square cells, evenly spaced, each cell holding one complete effect. PURE FLAT WHITE background (#FFFFFF) everywhere, including the gaps between cells. ABSOLUTELY NO GRID LINES, no cell borders, no dividing lines, no boxes, no outlines around the cells - the cells are separated by empty white space only. Each effect is CENTERED inside its own cell with a wide empty white margin on all four sides, and must not touch or cross where a neighbouring cell would begin. No frame numbers, no labels, no text, no watermark, no floor, no shadow, no character, no creature.

{subject}

Rendering style, follow exactly: flat cel-shaded shapes built from HARD-EDGED color steps, like vector shapes stacked on top of each other. Exactly four steps, each a solid uniform color with a crisp visible boundary and no blending between them: {palette}. That dark outer rim must be present and unbroken in every cell - it is what separates the effect from a light cream background in game. NO airbrush, NO smooth gradient, NO soft glow, NO blur, NO bloom, NO haze, NO semi-transparent wisps, NO highlights other than the small bright core. Edges are sharp and graphic.

{ramp}

Keep every cell's effect fully inside its own cell with a wide empty white margin around it.
"""

# 8 格单调渐变的通用说明。火那版验证过：必须强调「相邻格只差一点」，
# 否则窗口跨在突变处会来回跳，看着像闪帧而不是升星。
RAMP8 = """The 8 cells are ONE SMOOTH CONTINUOUS PROGRESSION, read left to right along the top row first (cells 1,2,3,4) and then left to right along the bottom row (cells 5,6,7,8). Across this progression the projectile grows steadily stronger: the core diameter increases GRADUALLY AND EVENLY from about 35 percent of the cell in cell 1 to about 55 percent of the cell in cell 8, and {deepen}, with the dark outer rim getting thicker step by step. EACH CELL MUST BE ONLY SLIGHTLY DIFFERENT FROM THE CELL BEFORE IT - the change from one cell to the next must be small and even, like 8 evenly spaced samples along a single ramp. Do NOT split the sheet into two contrasting groups, do NOT make the bottom row jump abruptly larger or darker than the top row, and do NOT restart the progression at cell 5.

On top of that steady ramp, the trailing tail also shifts a little from cell to cell so the frames read as alive: the tail leans slightly left, then stretches, then leans slightly right, then pulls shorter, and repeats. Keep the core itself perfectly centered horizontally in every cell so the sequence does not wobble."""

HEAD = ("Subject in every cell: a {name} projectile flying straight UP. "
        "The head is a {core}, sitting in the upper half of the cell. "
        "Below the head hangs a SHORT {tail}, no longer than the head's own width, "
        "with two or three small {fleck}. The head is the main read; the tail is a small accent.")

SHOTS = {
    "ice": dict(
        name="ICE SHARD",
        core="COMPACT ANGULAR CRYSTAL, a faceted six-sided shard with sharp straight edges and pointed corners, clearly geometric rather than round",
        tail="jagged trail of frost splinters",
        fleck="sharp ice slivers",
        palette=("a small white-hot core, then pale icy white-blue, then vivid saturated cyan, "
                 "then a DEEP DARK NAVY BLUE outer rim that traces the whole silhouette including the tail"),
        deepen="the color deepens GRADUALLY from pale cyan in cell 1 to deep saturated blue in cell 8, and the crystal grows one or two more facets",
    ),
    "water": dict(
        name="WATER DROPLET",
        core="COMPACT ROUND DROPLET, a clean teardrop with a smooth bulging bottom and a blunt rounded top, clearly round and liquid",
        tail="splashing trail of small droplets",
        fleck="round water beads",
        palette=("a small white core, then light sky blue, then vivid saturated medium blue, "
                 "then a DEEP DARK TEAL BLUE outer rim that traces the whole silhouette including the tail"),
        deepen="the color deepens GRADUALLY from light sky blue in cell 1 to deep saturated blue-teal in cell 8",
    ),
    "poison": dict(
        name="POISON BLOB",
        core="COMPACT LUMPY BLOB, a rounded gooey mass with two or three soft bulges on its edge and one thick drip hanging off the bottom, clearly viscous",
        tail="trail of falling goo drips",
        fleck="round bubbles",
        palette=("a small pale yellow-green core, then bright acid green, then vivid saturated grass green, "
                 "then a DEEP DARK PURPLE-BLACK outer rim that traces the whole silhouette including the drips"),
        deepen="the color deepens GRADUALLY from bright acid green in cell 1 to dark sickly green with more purple in cell 8, and one more bulge appears on the blob",
    ),
    "earth": dict(
        name="ROCK CHUNK",
        core="COMPACT ANGULAR BOULDER, a chunky irregular rock with flat straight facets and blunt corners, clearly solid and heavy",
        tail="trail of tumbling grit and small stones",
        fleck="angular pebbles",
        palette=("a small pale sand-yellow highlight facet, then warm ochre tan, then vivid saturated terracotta orange-brown, "
                 "then a DEEP DARK CHOCOLATE BROWN outer rim that traces the whole silhouette including the grit"),
        deepen="the color deepens GRADUALLY from light sandy tan in cell 1 to dark heavy brown in cell 8, and the rock gains one or two more facets",
    ),
    "explode": dict(
        name="BOMB CORE",
        core="COMPACT ROUND BOMB SHELL, a dark solid sphere with a single bright spark burst sitting on top of it like a lit fuse, the sphere clearly dark and the spark clearly bright",
        tail="trail of short spark streaks",
        fleck="bright spark dots",
        palette=("a small white-hot spark core, then bright lemon yellow on the spark only, then a vivid saturated "
                 "charcoal-to-dark-grey sphere body, then a NEAR-BLACK outer rim that traces the whole silhouette"),
        deepen="the spark on top grows GRADUALLY brighter and wider from a thin flicker in cell 1 to a full crown of sparks in cell 8, while the sphere body stays dark",
    ),
}

# 状态层：挂在怪身上，各占一块不重叠的区域，所以形状和比例都不一样。
STATUS = {
    "ice_crust": dict(
        cols=2, rows=2, cells=4,
        subject=("Subject in every cell: a CRUST OF ICE HANGING DOWN FROM ABOVE, meant to be laid over the head and "
                 "shoulders of a character sprite so the character looks frozen from the top down. Shape: a solid jagged "
                 "cap of ice spanning about 85 percent of the cell width along the TOP edge of the cell, from which FIVE "
                 "OR SIX pointed ICICLES hang straight DOWN to different lengths, the longest reaching about 60 percent "
                 "down the cell height. The icicles are narrow triangles with sharp tips pointing down, separated by "
                 "narrow gaps of plain white background so the character underneath can still be read through the gaps. "
                 "A few small ice slivers float beside the tips. The whole mass hangs from the top edge - the bottom "
                 "portion of the cell is empty white."),
        palette=("a white-hot glint sliver on each icicle, then pale icy white-blue, then vivid saturated cyan, "
                 "then a DEEP DARK NAVY BLUE outer rim tracing every icicle's silhouette"),
        ramp=("The four cells are one seamless loop of the same ice, so the overall silhouette and the width of the cap "
              "stay consistent across all four; only the glints and the small floating slivers move. Cell 1: glints sit "
              "high on the icicles. Cell 2: glints slide down, slivers drift out. Cell 3: glints near the tips, slivers "
              "widest apart. Cell 4: glints return toward the top, slivers settle back."),
    ),
    "dot_marks": dict(
        cols=2, rows=2, cells=4,
        subject=("Each of the four cells holds a DIFFERENT status marker for a character sprite, and they must NOT look "
                 "like variations of one another. Cell 1 (top left): a single POISON BUBBLE - one round gooey bubble with "
                 "a small bright spot, about 40 percent of the cell, centered, nothing else in the cell. Cell 2 (top "
                 "right): a STUN RING - a flat horizontal ellipse ring seen in slight perspective, like a halo lying "
                 "almost flat, with four small four-pointed stars spaced around the ring, the ring about 80 percent of "
                 "the cell wide and 35 percent tall, centered. Cell 3 (bottom left): a CONFUSION SPIRAL - a single open "
                 "spiral curl drawn as a tapering ribbon that winds around about one and a half turns, about 70 percent "
                 "of the cell, centered. Cell 4 (bottom right): a GROUND RIPPLE - two concentric flat ellipse rings lying "
                 "flat on the ground seen from a low angle, very wide and squashed, about 90 percent of the cell wide and "
                 "only 30 percent tall, centered."),
        palette=("for the poison bubble a pale yellow-green core then bright acid green then saturated grass green then a "
                 "DEEP DARK PURPLE-BLACK rim; for the stun ring a white core then bright lemon yellow then saturated "
                 "amber then a DEEP DARK BROWN rim; for the confusion spiral a white core then light lilac then "
                 "saturated violet then a DEEP DARK INDIGO rim; for the ground ripple a white core then pale aqua then "
                 "saturated teal then a DEEP DARK TEAL rim. Every one of the four has its own unbroken dark outer rim"),
        ramp=("These four are four unrelated markers, not an animation and not a progression - do not make them share a "
              "shape, and do not repeat the same object in more than one cell. Each one is a single clean graphic symbol "
              "readable at small size."),
    ),
}


# 道族 / 词组的弹体。规划里「道族不带色，免得把弹体搅浑」，所以这批一律
# 墨黑 + 骨白，不给元素色 —— 玩家一眼就能分出「这是招式，不是元素」。
# 它们不吃星级渐变（星改的是威力和动词，不是热度），每个只出一张。
WORDS = """2D game VFX sprite sheet for a top-down shooter. Layout: exactly 3 columns x 2 rows = 6 separate square cells, evenly spaced, each cell holding one complete projectile. PURE FLAT WHITE background (#FFFFFF) everywhere, including the gaps between cells. ABSOLUTELY NO GRID LINES, no cell borders, no dividing lines, no boxes, no outlines around the cells - the cells are separated by empty white space only. Each projectile is CENTERED inside its own cell with a wide empty white margin on all four sides. No frame numbers, no labels, no text, no watermark, no floor, no shadow, no character.

All six are INK-AND-BONE projectiles for a brush-ink themed game: the body is near-black ink, the inner highlight is warm bone white, and there is exactly one small crimson accent on each. They must NOT use blue, green, orange or any elemental color - keeping them ink-colored is what tells the player "this is a technique, not an element".

Rendering style, follow exactly: flat cel-shaded shapes built from HARD-EDGED color steps, like vector shapes stacked on top of each other. Exactly four steps, each a solid uniform color with a crisp visible boundary and no blending between them: a warm bone-white core stripe, then warm light grey, then near-black ink for the body, then a PURE BLACK outer rim tracing the whole silhouette. One small crimson wedge sits somewhere on each shape as the single accent. NO airbrush, NO smooth gradient, NO soft glow, NO blur, NO gloss, NO shiny specular streak, NO semi-transparent parts. Edges are sharp and graphic.

Every projectile points straight UP, is symmetric left-to-right about its own vertical axis, and fills about 60 percent of its cell height. The six silhouettes must be UNMISTAKABLY DIFFERENT from each other at a glance - do not make them variations of one blade shape.

Cell 1 (top left) EXECUTION CRESCENT: one broad curved reaper crescent, like a thick sickle blade with its horns pointing up and a heavy blunt base, the bone-white stripe running along the inner curve.

Cell 2 (top middle) DOUBLE CLEAVE: two separate narrow blade slashes crossing each other in a tall X, one leaning left and one leaning right, both tapering to points at top and bottom.

Cell 3 (top right) KNOCKBACK WEDGE: one squat blunt wedge like a flat-faced battering ram, very wide and short with a broad flat top edge and a narrow stem below, plus three short straight force lines under the stem.

Cell 4 (bottom left) ARROW VOLLEY: three narrow arrowheads of the same size side by side in a tight fan, the middle one slightly higher, each a simple triangular head on a short thin shaft.

Cell 5 (bottom middle) PIERCING SPIKE: one single very long and very thin needle spike, sharply pointed at the top, at most one sixth as wide as it is tall, with a small collar ring near its base.

Cell 6 (bottom right) SPLITTING FORK: one dart whose upper half forks into TWO diverging prongs making a clear Y shape, with a single solid tail below the fork.

Keep every cell's projectile fully inside its own cell with a wide empty white margin around it.
"""


# ---- 12 张招牌两两 ----------------------------------------------------------
# 每对只换「体」槽一张图。它们是两元素融合，所以要**比单元素弹更繁**一点
# —— 「组合越多越炫」是玩家能看出叠了几个字的唯一依据。
#
# 配色必须锚在游戏调色板上，凭印象会串味。尤其两个反直觉的：
#   雷 Thunder = #5B4FD1 靛紫（不是黄色！），高光 #C6C0FF 淡薰衣草
#   毒 Poison  = #7A3FA0 紫，高光 #A8D84B 酸绿
# 其余：金 #C9922A / 木 #4E7A33 / 风 #6F9C86 青灰绿 / 土 #8A6134 / 炸 #DB3847
LOOP4 = """The four cells are ONE SEAMLESS LOOP of the same projectile, not a progression and not a size ramp. The overall silhouette, size and color must stay CONSISTENT across all four cells - only the small accents move: the sparks, arcs, drips or slivers shift position from cell to cell and return to the start, so the four frames can cycle forever. Do NOT make any cell bigger, smaller, darker or lighter than the others."""

PAIR_HEAD = ("Subject in every cell: a {name} projectile flying straight UP, about 55 percent of the cell "
             "across. {shape} It must clearly read as TWO ELEMENTS FUSED INTO ONE projectile, and should look "
             "noticeably busier and more elaborate than a plain single-element projectile - this extra "
             "richness is how the player sees that two glyphs are stacked. Keep it centered and symmetric "
             "left-to-right about its own vertical axis.")

PAIRS = {
    "frostfire": dict(  # 火+冰 霜火
        name="FROSTFIRE",
        shape="A COMPACT ANGULAR ICE CRYSTAL forms the core, and ORANGE FLAMES lick outward from between its "
              "facets, wrapping its lower half. Ice and fire touch directly - the crystal is not melting, the "
              "flames grow out of it.",
        palette="a white-hot glint on the crystal, then pale icy white-blue, then vivid saturated cyan for the "
                "crystal body, with the flames in bright tangerine orange, and a DEEP DARK NAVY BLUE outer rim "
                "tracing the crystal while a DARK RED-BROWN rim traces the flames",
    ),
    "scorchbolt": dict(  # 火+雷 焦雷
        name="SCORCHED-THUNDER",
        shape="A ROUND FLAMING BALL forms the core, and FOUR SHARP ZIGZAG LIGHTNING BOLTS stab outward from it "
              "in four directions, each a narrow angular zigzag with straight segments and hard corners.",
        palette="a white-hot core, then bright golden yellow, then vivid saturated tangerine orange for the ball, "
                "with the lightning bolts in vivid INDIGO-VIOLET (the color of a violet crayon, not yellow) edged "
                "in pale lavender, and a DEEP DARK RED-BROWN outer rim on the ball plus a DARK INDIGO rim on the bolts",
    ),
    "hailbolt": dict(  # 冰+雷 霰雷
        name="HAIL-THUNDER",
        shape="A FACETED ICE SHARD forms the core, and THIN ANGULAR ELECTRIC ARCS crawl across its surface and "
              "jump off its corners in short jagged forks.",
        palette="a white glint on the shard, then pale icy white-blue, then vivid saturated cyan for the shard, "
                "with the electric arcs in vivid INDIGO-VIOLET (violet crayon, not yellow) cored with pale "
                "lavender, and a DEEP DARK NAVY BLUE outer rim tracing everything",
    ),
    "blightfire": dict(  # 火+毒 燎毒
        name="BLIGHT-FIRE",
        shape="A LUMPY GOOEY BLOB forms the core and it is BURNING WITH GREEN FLAMES - the flames have the exact "
              "shape of fire, tongues rising and curling, but they are acid green instead of orange. Two thick "
              "drips hang off the blob's bottom.",
        palette="a pale yellow-green core, then bright acid green, then vivid saturated grass green for the "
                "flame tongues, with the blob body a deeper murky green, and a DEEP DARK PURPLE-BLACK outer rim "
                "tracing the whole silhouette including the drips",
    ),
    "conduct": dict(  # 水+雷 导电
        name="CONDUCTING-WATER",
        shape="A SMOOTH ROUND WATER DROPLET forms the core, and BRANCHING ELECTRIC ARCS run INSIDE it like "
              "cracks of light, with two short arcs escaping off its sides.",
        palette="a white core, then light sky blue, then vivid saturated medium blue for the droplet, with the "
                "internal arcs in vivid INDIGO-VIOLET (violet crayon, not yellow) cored with pale lavender, and "
                "a DEEP DARK TEAL BLUE outer rim tracing the droplet",
    ),
    "moltengold": dict(  # 金+火 熔金
        name="MOLTEN-GOLD",
        shape="A THICK ROUNDED BLOB OF MOLTEN METAL forms the core, with a heavy bright rim-light along its top "
              "and THREE MOLTEN DRIPS hanging off its bottom, the drips glowing hot orange at their tips.",
        palette="a pale cream highlight, then the yellow of a new gold coin, then a deeper honey gold for the "
                "body, with the hot drip tips in bright tangerine orange, and a DEEP DARK CHOCOLATE BROWN outer "
                "rim tracing the whole silhouette",
    ),
    "wardgold": dict(  # 金+重 镇金
        name="WARDING-GOLD",
        shape="A SQUAT HEAVY GOLD SEAL BLOCK forms the core - a thick rectangular ingot seen at a slight angle "
              "so its top face and front face both show, clearly solid and massive rather than round. Three "
              "short straight weight lines sit under it.",
        palette="a pale cream highlight on the top face, then the yellow of a new gold coin for the top face, "
                "then a deeper honey gold for the front face, and a DEEP DARK CHOCOLATE BROWN outer rim tracing "
                "the whole silhouette",
    ),
    "rotlife": dict(  # 木+毒 蚀生
        name="ROT-LIFE",
        shape="A CURLED SPROUT WITH TWO LEAVES forms the core, and THICK ACID-GREEN GOO coats its lower half and "
              "hangs off it in two drips. The sprout is clearly a living plant shape; the goo is clearly liquid.",
        palette="a pale yellow-green highlight, then bright acid green for the goo, then a deep forest green for "
                "the leaves, and a DEEP DARK PURPLE-BLACK outer rim tracing the whole silhouette including the drips",
    ),
    "ramearth": dict(  # 土+重 夯土
        name="RAMMING-EARTH",
        shape="A MASSIVE ANGULAR BOULDER forms the core, wider and blunter than a normal rock, with a BROAD FLAT "
              "FRONT FACE on top like the head of a battering ram. Four short straight force lines sit beneath it "
              "and a few angular pebbles scatter below.",
        palette="a pale sand-yellow highlight facet, then warm ochre tan, then vivid saturated terracotta "
                "orange-brown for the body, and a DEEP DARK CHOCOLATE BROWN outer rim tracing everything",
    ),
    "coldwind": dict(  # 风+冰 寒风
        name="COLD-WIND",
        shape="A CURLING WIND SWIRL forms the core - one open tapering ribbon that winds around about one turn - "
              "and FOUR SHARP ICE SLIVERS are caught in it, spaced around the curl with their points facing "
              "outward.",
        palette="a pale mint highlight, then a soft sage green-grey for the wind ribbon, with the ice slivers in "
                "vivid saturated cyan cored with pale icy white-blue, and a DEEP DARK TEAL outer rim tracing the "
                "ribbon plus a DEEP NAVY rim on each sliver",
    ),
    "blaze": dict(  # 炸+火 烈爆
        name="BLAZING-BOMB",
        shape="A DARK ROUND BOMB SHELL forms the core, and where its fuse would be there is instead a FULL CROWN "
              "OF FLAMES sitting on top, tongues rising and curling, twice as tall as the fuse would be.",
        palette="a white-hot core in the flames, then bright golden yellow, then vivid saturated tangerine orange "
                "for the flame crown, with the sphere body a flat solid charcoal near-black, and a NEAR-BLACK "
                "outer rim tracing the whole silhouette",
    ),
    "thundercut": dict(  # 斩+雷 雷决
        name="THUNDER-VERDICT",
        shape="A BROAD CURVED REAPER CRESCENT forms the core - a thick sickle blade with its horns pointing up - "
              "and a JAGGED LIGHTNING ARC runs along its cutting edge, following the curve and throwing two short "
              "forks off the horns.",
        palette="a warm bone-white stripe along the blade's inner curve, then warm light grey, then near-black "
                "ink for the blade body, with the lightning arc in vivid INDIGO-VIOLET (violet crayon, not "
                "yellow) cored with pale lavender, and a PURE BLACK outer rim tracing the whole silhouette",
    ),
}


def write(name, text):
    p = OUT / f"{name}.txt"
    p.write_text(text)
    print("wrote", p.relative_to(OUT.parents[2]))


if __name__ == "__main__":
    for key, cfg in SHOTS.items():
        subject = HEAD.format(name=cfg["name"], core=cfg["core"], tail=cfg["tail"], fleck=cfg["fleck"])
        write(f"v2_shot_{key}", SHEET.format(
            cols=4, rows=2, cells=8, subject=subject,
            palette=cfg["palette"], ramp=RAMP8.format(deepen=cfg["deepen"])))
    write("v2_shot_words", WORDS)
    for key, cfg in PAIRS.items():
        write(f"v2_pair_{key}", SHEET.format(
            cols=2, rows=2, cells=4,
            subject=PAIR_HEAD.format(name=cfg["name"], shape=cfg["shape"]),
            palette=cfg["palette"], ramp=LOOP4))
    for key, cfg in STATUS.items():
        write(f"v2_{key}", SHEET.format(
            cols=cfg["cols"], rows=cfg["rows"], cells=cfg["cells"],
            subject=cfg["subject"], palette=cfg["palette"], ramp=cfg["ramp"]))
