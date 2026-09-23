# Dialogue portrait prompts (Gemini)

Prompts for making the dialogue portraits with Gemini on the web (free).
`Tools/import_portraits.py` then turns the downloads into the in-game portraits.

## How to do it

1. Open gemini.google.com and use image generation.
2. **One chat per character.** Paste the character's **base** prompt (for characters 2-6, the style paragraph below, then their character paragraph). Regenerate until you like it.
   - Download it and save it as `Art/PortraitsRaw/<name>_neutral.png`.
3. In the **same chat**, paste each **expression** prompt one at a time: "Take the first portrait of <name> in this chat and keep everything identical...", then the expression. Save each as `<name>_<expression>.png`.
   - No upload is needed; Gemini edits the portrait already in the chat. Attaching `rook_neutral.png` as a style reference also works if uploads do.

**File names:** `rook`, `pip`, `tala`, `general`, `dobbs`, `sultan`, then `_neutral`, `_angry`, `_smug`, `_surprised`, `_panic` or `_soft`. PNG, JPG and WEBP all work.

**Required** expressions are the ones the game actually uses (21 images). The optional ones are never shown right now, so skip them if Gemini runs out of free images. Missing ones borrow a neighbouring expression.

**Don't worry about:**
- the Gemini sparkle watermark in the corner;
- a character facing the wrong way (the script flips them);
- the exact image size.

**Do redo** an image if the background isn't one flat magenta colour, or if a hand or hat runs off the top of the picture. To fix the background, reply: *"Make the background one flat solid magenta (#FF00FF), nothing else."*

**Test first:** do `rook_neutral` and `tala_neutral`, then tell Claude. Claude imports those two so you can check the look in-game before making the rest.

---

## Style (already included in every base prompt)

> Character portrait for a retro action game's dialogue box. Style: high-quality 16-bit pixel art blended with clean anime/comic illustration: crisp visible pixels, bold dark outlines, cel shading with 2-3 tones per colour, smooth anime facial features and expressive eyes, vibrant but slightly earthy jungle-military palette (Metal Slug meets modern anime pixel art). Framing: square 1:1 image, head-and-shoulders bust, three-quarter view with the character turned to face the RIGHT side of the image, whole head visible with a little empty space above it, shoulders cut off by the bottom edge. Background: one flat solid magenta colour (#FF00FF), no gradient, no shadow, no scenery. No text, no frame, no border.

---

## Rook (the hero): new chat, no attachment

**Base → `rook_neutral`** (required)
```
Character portrait for a retro action game's dialogue box. Style: high-quality 16-bit pixel art blended with clean anime/comic illustration: crisp visible pixels, bold dark outlines, cel shading with 2-3 tones per colour, smooth anime facial features and expressive eyes, vibrant but slightly earthy jungle-military palette (Metal Slug meets modern anime pixel art). Framing: square 1:1 image, head-and-shoulders bust, three-quarter view with the character turned to face the RIGHT side of the image, whole head visible with a little empty space above it, shoulders cut off by the bottom edge. Background: one flat solid magenta colour (#FF00FF), no gradient, no shadow, no scenery. No text, no frame, no border.

The character: Rook, a rugged commando in his early 30s and the game's hero. Short messy dark-brown hair, a long red bandana headband tied at the back with its tails hanging behind his head, light stubble, tan skin, strong jaw, olive-green sleeveless tank top, muscular shoulders, silver dog tags on a ball chain. Expression: deadpan and unimpressed, half-lidded eyes, flat straight mouth.
```

**Expressions** (same chat). Paste this line, then add the expression from the list:
```
Edit this image: keep the exact same character, outfit, art style, pixel size, framing, pose and flat magenta background. Only change the facial expression to:
```
- `rook_angry` (required): furious, eyes narrowed, brows pulled down hard, teeth gritted, jaw clenched.
- `rook_soft` (required): quietly touched, eyes softened and looking slightly down, a small reluctant half-smile he is trying to hide.
- `rook_smug` (optional): a dry smirk, one corner of the mouth up, one eyebrow raised.
- `rook_surprised` (optional): eyebrows up, eyes wide, mouth slightly open.
- `rook_panic` (optional): alarmed, eyes wide, a sweat drop on his temple, mouth open in a shout.

---

## Characters 2-6: new chat each

Start every base prompt below with the style paragraph above. For Sultan, say "head-and-mane bust of a lion" instead of "head-and-shoulders bust".
For the expressions, begin with: "Take the first portrait of <name> in this chat and keep everything identical: same character, outfit, art style, pixel size, framing, pose and flat magenta background. Only change the expression to:"

### Pip (wildlife vet on the radio)
**Base → `pip_neutral`** (required)
```
The character: Dr. Priya "Pip" Nair, a brilliant, over-caffeinated wildlife vet in her late 20s. South Asian, brown skin, black hair in a messy bun with a pencil stuck through it, round glasses, a grey radio headset with a small mic arm and a tiny glowing cyan light, teal medical scrubs under an open white lab coat. Expression: bright and alert, wide-awake eyes behind the glasses, an eager smile, one hand touching the headset mic.
```
- `pip_surprised` (required): gasping, eyebrows shot up, eyes huge behind the glasses, mouth open in an "oh!".
- `pip_soft` (required): gentle and sad-warm, eyebrows tilted up, a small kind smile, eyes a little glossy.
- `pip_panic` (required): frantic, both hands clutching the headset, eyes wide, mouth wide open yelling, sweat drops flying, glasses slightly crooked.
- `pip_angry` (optional): scolding, brows furrowed, mouth open mid-lecture.
- `pip_smug` (optional): self-satisfied, eyes half-closed, knowing grin, pushing her glasses up with one finger.

### Tala (the ranger)
**Base → `tala_neutral`** (required)
```
The character: Tala Reyes, a calm, tough reserve park ranger in her early 30s. Filipina, warm brown skin, dark-brown hair in one long thick braid over her shoulder, a tan campaign ranger hat with a dark band, an olive-green ranger shirt with a V collar and a gold badge on the chest. Expression: calm and steady, a level unblinking stare, mouth closed, serious.
```
- `tala_angry` (required): fierce, a hard glare, brows down, teeth slightly bared, jaw tight.
- `tala_soft` (required): tender, eyes glistening with held-back tears, a small sad smile.
- `tala_surprised` (required): eyes widened, lips parted, eyebrows raised.
- `tala_smug` (required): dry confidence, one eyebrow raised, the corner of her mouth turned up.
- `tala_panic` (optional): alarmed, eyes wide, mouth open, a bead of sweat.

### General Maneford (the villain)
**Base → `general_neutral`** (required)
```
The character: General Leopold "The King" Maneford, a pompous, vain villain in his 60s. Pale pinkish skin, a huge white handlebar mustache, bushy white eyebrows, a gold military helmet sculpted like a lion's head with a big flowing orange-gold lion mane around it and a small red plume on top, a dark olive general's uniform with gold epaulettes, gold collar trim and a row of colourful medal ribbons. Expression: haughty, chin raised, looking down his nose.
```
- `general_smug` (required): gloating, eyes half-closed, a big self-satisfied grin under the mustache.
- `general_angry` (required): raging, face flushed red, eyes bulging, mouth wide open shouting, mustache bristling, a throbbing vein on his forehead.
- `general_panic` (required): terrified, sweating heavily, eyes bulging, mustache drooping, the lion helmet knocked crooked.
- `general_surprised` (optional): jaw dropped, eyebrows shot up.
- `general_soft` (optional): humbled and sad, eyes downcast and wet, mustache drooping.

### Corporal Dobbs (the nervous soldier)
**Base → `dobbs_neutral`** (required)
```
The character: Corporal Dobbs, a nervous, gangly young soldier around 20. Pale freckled skin, big ears, sandy-brown hair peeking out, a green army helmet clearly too big for him that sits low over his eyebrows with the chin strap dangling, an olive uniform with a bright orange hi-vis safety vest with a yellow reflective stripe. Expression: a nervous, polite smile, eager eyes, slightly worried eyebrows.
```
- `dobbs_panic` (required): terrified, both hands raised in surrender, eyes huge, sweating buckets, helmet slipping over one eye. Keep his hands inside the picture.
- `dobbs_surprised` (required): eyes wide, mouth in a small "o", eyebrows up under the helmet.
- `dobbs_smug` (optional): proud, chest puffed out, beaming grin.
- `dobbs_angry` (optional): trying to look tough and failing, a pouty frown.
- `dobbs_soft` (optional): sheepish, an embarrassed smile, blushing.

### Sultan (the alpha lion)
**Base → `sultan_neutral`** (required)
```
The character: Sultan, a majestic adult male African lion, the alpha of the reserve. Head and mane bust. A huge dark-auburn and orange mane, golden fur, amber-gold eyes, a couple of old scars on his nose, no collar. Expression: calm and regal, a steady gaze, mouth closed.
```
- `sultan_angry` (required): a full mighty roar, mouth wide open showing his fangs, eyes narrowed, mane flared.
