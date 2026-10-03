#!/usr/bin/env python3
"""v4 命中特效：每种命中一张 2x2 四帧表，画法对标 hit_explode（平涂、硬边色阶、深色外沿、
有烟团和碎块的层次）。写出 docs/prompt/vfx/v4_hit_<key>.txt，生图后放 art_src/vfx/raw/v4_hit_<key>.png，
再用 docs/prompt/runtime/vfx_flat_slice.py hitv_<key> 切成 Assets/Resources/Art/Vfx/hitv_<key>_00..03。

参考图 art_src/vfx/raw/_ref_hit_explode.png 只提供画法。提示词只写正面要求。
"""

from pathlib import Path

HERE = Path(__file__).resolve().parent

HEAD = (
    "2D game hit-impact VFX sprite sheet for a cute casual mobile tower-defense game. "
    "Layout: exactly 2 columns x 2 rows = 4 square cells, evenly spaced, on a plain pure white background (#FFFFFF), "
    "with only empty white space between the cells. Each effect sits centered in its own cell with a wide white margin "
    "on all four sides and stays fully inside its cell. The sheet contains only the effect shapes on white.\n\n"
    "The attached reference image shows four frames of an explosion from the same game. Use it ONLY for the drawing "
    "style: flat cel-shaded vector shapes stacked in layers, each layer one solid color with a crisp edge, "
    "a thick dark outline around every shape, chunky rounded silhouettes, a bright small core, a mid layer, an outer "
    "layer, then secondary pieces flying off. Match that line weight, that chunkiness and that layered read. "
    "Draw the new subject described below.\n\n"
)

FRAMES = (
    "The four cells are one impact read in order: top-left, top-right, bottom-left, bottom-right.\n"
    "Cell 1 (impact flash): a small tight burst, about 40 percent of the cell, bright core with the first layer "
    "just opening.\n"
    "Cell 2 (full burst): the biggest frame, about 80 percent of the cell, every color layer fully open, "
    "the strongest silhouette.\n"
    "Cell 3 (break-up): the main shape breaking into several chunky secondary pieces drifting outward, "
    "about 85 percent of the cell, colors a little darker.\n"
    "Cell 4 (remnant): only a scattered ring of small leftover pieces spread wide near the cell edges, "
    "about 90 percent of the cell, mostly empty white in the middle.\n"
    "All four share one center point so the frames line up when played in place.\n\n"
)

# 每种命中：主体、四层颜色（用实物锚定）、碎块、外沿。
HITS = {
    "fire": (
        "FIRE HIT: a burst of chunky flame tongues fanning out in a star of rounded flame petals.",
        "core like a candle flame center (warm white), then egg-yolk yellow, then tangerine orange, "
        "then chili-pepper red tips",
        "small round embers and curled little flames fly off; in cell 4 a few embers and two small grey-brown "
        "smoke puffs like the reference",
        "dark brown outline like burnt toast",
    ),
    "ice": (
        "ICE HIT: a burst of chunky faceted ice crystals, like a cracked ice cube exploding into big shards.",
        "core like fresh snow (white), then pale ice-pop blue, then swimming-pool cyan, then sapphire blue shard edges",
        "angular ice chunks and small snowflake stars fly off; in cell 4 a ring of small ice cubes and two "
        "pale frost puffs",
        "deep navy outline like dark denim",
    ),
    "water": (
        "WATER HIT: a big splash crown of water, a round splat with rounded droplet petals rising around it.",
        "core like foam on a wave (white), then baby-blue sky, then swimming-pool blue, then deep ocean blue",
        "round water droplets and small teardrops fly off; in cell 4 a ring of small droplets and two tiny "
        "splash puddles",
        "deep navy outline like dark denim",
    ),
    "thunder": (
        "THUNDER HIT: an electric burst with chunky zigzag lightning bolts radiating out from a bright center.",
        "core like a camera flash (white), then lemon yellow, then grape-soda violet, then deep eggplant purple",
        "short zigzag bolt pieces and small spark stars fly off; in cell 4 a ring of tiny zigzags and spark dots",
        "very dark purple outline like a blackberry",
    ),
    "poison": (
        "POISON HIT: a gooey toxic splat with blobby drips and round bubbles bursting out.",
        "core like lime juice (pale yellow-green), then green apple, then grass green, "
        "with a few grape-purple bubbles",
        "round bubbles and slime droplets fly off; in cell 4 a ring of small bubbles and two little green "
        "goo drops",
        "dark green outline like a pine tree",
    ),
    "earth": (
        "EARTH HIT: a burst of chunky rock fragments and a dust cloud, like a clay pot smashing on stone.",
        "core like sand (pale tan), then caramel brown, then terracotta brick, then dark chocolate brown rocks",
        "angular pebbles and rock chips fly off; in cell 4 scattered small pebbles and two beige dust puffs",
        "very dark brown outline like coffee beans",
    ),
    "wind": (
        "WIND HIT: a swirling gust burst, chunky curved wind ribbons spiralling outward from the center.",
        "core like mint ice cream (white-mint), then peppermint green, then jade green, then teal ribbon edges",
        "curled wind ribbons and two or three small green leaves fly off; in cell 4 a ring of small curls and leaves",
        "dark teal outline like a peacock feather",
    ),
    "gold": (
        "GOLD HIT: a shiny burst of treasure, a bright star flash with chunky gold coins popping out.",
        "core like sunlight on a coin (white), then banana yellow, then shiny gold coin yellow, then honey amber",
        "round gold coins and small four-point sparkle stars fly off; in cell 4 a ring of small coins and sparkles",
        "dark amber-brown outline like maple syrup",
    ),
    "wood": (
        "WOOD HIT: a burst of fresh green leaves and wooden splinters, like a branch snapping.",
        "core like a lettuce heart (pale green), then green apple, then basil leaf green, with pine-wood tan splinters",
        "leaves and small wooden splinters fly off spinning; in cell 4 a ring of small leaves and splinters",
        "dark forest-green outline like spinach",
    ),
    "heavy": (
        "HEAVY HIT: a powerful ground-shaking impact, a thick round shockwave ring with jagged spikes and "
        "a heavy dust cloud.",
        "core like milk (white), then light pebble grey, then slate grey, then charcoal grey spikes",
        "chunky stone bits and dust puffs fly off; in cell 4 a wide ring of small grey dust puffs",
        "near-black outline like ink",
    ),
    "ink": (
        "INK HIT: a juicy splash of calligraphy ink with bright sparks, a round ink splat with rounded petals.",
        "core like a spark (white), then coin gold sparkles, then dark graphite grey splat, then ink-black petals",
        "round ink droplets and small gold star sparks fly off; in cell 4 a ring of ink drops and sparks",
        "near-black outline like ink",
    ),
    "stun": (
        "STUN HIT: a dizzy impact, a bright starburst flash with a circle of chunky cartoon stars bursting out.",
        "core like a light bulb (white), then lemon yellow, then sunflower yellow, then marigold orange star tips",
        "five-point cartoon stars fly off spinning; in cell 4 a ring of small stars",
        "dark brown outline like burnt toast",
    ),
    "confuse": (
        "CONFUSE HIT: a dizzy swirl burst, a chunky spiral swirl with puffy candy clouds around it.",
        "core like cotton candy (pale pink), then bubblegum pink, then orchid purple, then plum purple swirl edges",
        "small spirals and round candy puffs fly off; in cell 4 a ring of small spirals",
        "dark plum outline like a ripe plum",
    ),
    "kill": (
        "EXECUTE HIT: a deadly finishing strike, a sharp crimson X slash burst with a bright flash in the center.",
        "core like a camera flash (white), then strawberry pink, then red envelope red, then dark cherry red",
        "sharp red shards and small crescent slashes fly off; in cell 4 a ring of small red shards",
        "near-black outline like ink",
    ),
    "cleave": (
        "CLEAVE HIT: a sweeping sword cut, two chunky curved crescent slash arcs crossing with a bright flash.",
        "core like sunlight (white), then banana yellow, then shiny gold coin yellow, then orange-peel arc edges",
        "small crescent slivers and spark stars fly off; in cell 4 a ring of small crescent slivers",
        "dark brown outline like burnt toast",
    ),
    "knock": (
        "KNOCKBACK HIT: a cartoon POW impact, a chunky spiky impact star with curved motion arcs pushing outward.",
        "core like milk (white), then lemon yellow, then tangerine orange, then tomato red spike tips",
        "curved speed arcs and small round dust puffs fly off; in cell 4 a ring of small arcs and puffs",
        "dark brown outline like burnt toast",
    ),
    "arrow": (
        "ARROW HIT: a sharp piercing impact, a narrow pointed star burst with a few feathers popping out.",
        "core like milk (white), then cream, then coin gold, then tangerine orange points, "
        "with brown sparrow feathers",
        "small feathers and splinter shards fly off; in cell 4 a ring of small feathers",
        "dark brown outline like burnt toast",
    ),
    "fireice": (
        "FROST-FIRE HIT: one burst split down the middle, the left half chunky flame petals and the right half "
        "chunky ice shards, meeting in a bright center.",
        "core like snow (white); flame half egg-yolk yellow then tangerine orange then chili red; "
        "ice half ice-pop blue then swimming-pool cyan then sapphire blue",
        "embers fly off the flame side and ice chips off the ice side; in cell 4 a ring of mixed embers and ice chips",
        "deep navy outline like dark denim",
    ),
}

SLOW = (
    "2D game status-mark sprite sheet for a cute casual mobile tower-defense game, a FROSTY SLOW PATCH that sits on "
    "the ground under a monster's feet, seen from a gentle three-quarter top view so it is a wide flat ellipse. "
    "Layout: exactly 2 columns x 2 rows = 4 square cells, evenly spaced, on a plain pure white background (#FFFFFF), "
    "with only empty white space between the cells. Each patch sits centered in its own cell with a wide white "
    "margin, about 80 percent of the cell wide and about 30 percent of the cell tall.\n\n"
    "The attached reference image shows the drawing style of the same game: flat cel-shaded layered shapes, "
    "one solid color per layer, crisp edges, a thick dark outline. Use it only for that style.\n\n"
    "Subject: a flat icy puddle ellipse, the surface like a frozen pond (pale ice-pop blue) with a lighter inner "
    "shine band like fresh snow, a swimming-pool blue rim, a thick deep navy outline like dark denim, "
    "and four or five small chunky ice crystals standing up along the back edge of the ellipse, "
    "with two small white four-point sparkles.\n"
    "The four cells are a gentle looping shimmer of the same patch: identical ellipse and position in all four, "
    "only the sparkles move to new spots and the shine band slides slightly left to right.\n"
)


def main():
    for key, (subject, colors, debris, rim) in HITS.items():
        text = (HEAD + "Subject: " + subject + "\n"
                + "Color layers from center outward: " + colors + ".\n"
                + "Secondary pieces: " + debris + ".\n"
                + "Every shape has a thick " + rim + ".\n\n"
                + FRAMES)
        (HERE / f"v4_hit_{key}.txt").write_text(text, encoding="utf-8")
    (HERE / "v4_slow_mark.txt").write_text(SLOW, encoding="utf-8")
    print("written", len(HITS) + 1)


if __name__ == "__main__":
    main()
